using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using YumGo.Application.Addresses;

namespace YumGo.Infrastructure.Addresses;

public sealed class GoogleAddressReverseGeocoder(
    HttpClient httpClient,
    IConfiguration configuration,
    IGoogleGeocodingQuota quota)
    : IAddressReverseGeocoder
{
    private const string Endpoint = "https://maps.googleapis.com/maps/api/geocode/json";

    public async Task<ReverseGeocodedAddress?> ReverseGeocodeAsync(
        decimal latitude,
        decimal longitude,
        CancellationToken cancellationToken)
    {
        var apiKey = configuration["GoogleMaps:GeocodingApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new GeocodingUnavailableException("Google Geocoding is not configured.");
        }

        const int FreeTierSafetyCap = 9000;
        var configuredLimit = configuration["GoogleMaps:MonthlyRequestLimit"];
        var monthlyLimit = FreeTierSafetyCap;
        if (configuredLimit is not null
            && (!int.TryParse(configuredLimit, out monthlyLimit) || monthlyLimit is < 1 or > FreeTierSafetyCap))
        {
            throw new GeocodingUnavailableException("Google Geocoding monthly request limit must be between 1 and 9000.");
        }

        if (!await quota.TryReserveAsync(monthlyLimit, cancellationToken))
        {
            throw new GeocodingQuotaExceededException();
        }

        var url = $"{Endpoint}?latlng={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}%2C{longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&language=vi&region=vn&key={Uri.EscapeDataString(apiKey)}";
        GoogleGeocodingResponse? response;
        try
        {
            using var httpResponse = await httpClient.GetAsync(url, cancellationToken);
            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new GeocodingUnavailableException("Google Geocoding returned an unsuccessful HTTP response.");
            }

            response = await httpResponse.Content.ReadFromJsonAsync<GoogleGeocodingResponse>(cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new GeocodingUnavailableException("Google Geocoding could not be reached.", exception);
        }
        catch (JsonException exception)
        {
            throw new GeocodingUnavailableException("Google Geocoding returned an invalid response.", exception);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new GeocodingUnavailableException("Google Geocoding timed out.", exception);
        }

        if (response is null)
        {
            throw new GeocodingUnavailableException("Google Geocoding returned an empty response.");
        }

        if (string.Equals(response.Status, "ZERO_RESULTS", StringComparison.Ordinal)) return null;
        if (!string.Equals(response.Status, "OK", StringComparison.Ordinal)
            || response.Results is null
            || response.Results.Count == 0)
        {
            throw new GeocodingUnavailableException("Google Geocoding could not resolve the submitted location.");
        }

        var result = response.Results[0];
        var addressLine = BuildAddressLine(result);
        var ward = FindComponent(result, "administrative_area_level_3", "sublocality_level_1", "sublocality_level_2");
        var district = FindComponent(result, "administrative_area_level_2", "locality", "sublocality_level_1");
        var city = FindComponent(result, "administrative_area_level_1", "locality");
        if (string.IsNullOrWhiteSpace(addressLine)
            || string.IsNullOrWhiteSpace(ward)
            || string.IsNullOrWhiteSpace(district)
            || string.IsNullOrWhiteSpace(city))
        {
            return null;
        }

        return new ReverseGeocodedAddress(addressLine, ward, district, city);
    }

    private static string BuildAddressLine(GoogleGeocodingResult result)
    {
        var number = FindComponent(result, "street_number");
        var route = FindComponent(result, "route");
        if (!string.IsNullOrWhiteSpace(route))
        {
            return string.Join(' ', new[] { number, route }.Where(part => !string.IsNullOrWhiteSpace(part)));
        }

        return FindComponent(result, "premise", "establishment") ?? result.FormattedAddress ?? string.Empty;
    }

    private static string? FindComponent(GoogleGeocodingResult result, params string[] preferredTypes)
    {
        foreach (var type in preferredTypes)
        {
            var component = result.AddressComponents.FirstOrDefault(candidate => candidate.Types.Contains(type, StringComparer.Ordinal));
            if (component is not null) return component.LongName;
        }

        return null;
    }

    private sealed record GoogleGeocodingResponse(
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("results")] List<GoogleGeocodingResult> Results);

    private sealed record GoogleGeocodingResult(
        [property: JsonPropertyName("formatted_address")] string? FormattedAddress,
        [property: JsonPropertyName("address_components")] List<GoogleAddressComponent> AddressComponents);

    private sealed record GoogleAddressComponent(
        [property: JsonPropertyName("long_name")] string LongName,
        [property: JsonPropertyName("types")] List<string> Types);
}
