using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using YumGo.Application.Addresses;
using YumGo.Infrastructure.Addresses;

namespace YumGo.Infrastructure.Tests;

public sealed class GoogleAddressReverseGeocoderTests
{
    [Fact]
    public async Task Reverse_geocodes_coordinates_and_returns_provider_components()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """
            {"status":"OK","results":[{"formatted_address":"12 Nguyen Hue, Ben Nghe, District 1, Ho Chi Minh City, Vietnam","address_components":[
              {"long_name":"12","types":["street_number"]},
              {"long_name":"Nguyen Hue","types":["route"]},
              {"long_name":"Ben Nghe","types":["administrative_area_level_3"]},
              {"long_name":"District 1","types":["administrative_area_level_2"]},
              {"long_name":"Ho Chi Minh City","types":["administrative_area_level_1"]}]}]}
            """);
        var quota = new FakeGeocodingQuota(true);
        var geocoder = CreateGeocoder(handler, quota);

        var address = await geocoder.ReverseGeocodeAsync(10.7769m, 106.7009m, CancellationToken.None);

        Assert.Equal(new ReverseGeocodedAddress("12 Nguyen Hue", "Ben Nghe", "District 1", "Ho Chi Minh City"), address);
        Assert.Equal(1, quota.Reservations);
        Assert.Contains("latlng=10.7769%2C106.7009", handler.RequestUri);
        Assert.Contains("language=vi", handler.RequestUri);
    }

    [Fact]
    public async Task Reverse_geocoder_refuses_to_call_google_after_monthly_quota_is_reached()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
        var quota = new FakeGeocodingQuota(false);
        var geocoder = CreateGeocoder(handler, quota);

        await Assert.ThrowsAsync<GeocodingQuotaExceededException>(
            () => geocoder.ReverseGeocodeAsync(10m, 106m, CancellationToken.None));

        Assert.Equal(1, quota.Reservations);
        Assert.Equal(0, handler.Requests);
    }

    private static GoogleAddressReverseGeocoder CreateGeocoder(FakeHttpMessageHandler handler, FakeGeocodingQuota quota)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GoogleMaps:GeocodingApiKey"] = "test-key",
                ["GoogleMaps:MonthlyRequestLimit"] = "9000"
            })
            .Build();
        return new GoogleAddressReverseGeocoder(new HttpClient(handler), configuration, quota);
    }

    private sealed class FakeGeocodingQuota(bool available) : IGoogleGeocodingQuota
    {
        public int Reservations { get; private set; }
        public Task<bool> TryReserveAsync(int monthlyLimit, CancellationToken cancellationToken)
        {
            Reservations++;
            Assert.Equal(9000, monthlyLimit);
            return Task.FromResult(available);
        }
    }

    private sealed class FakeHttpMessageHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
    {
        public int Requests { get; private set; }
        public string? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            RequestUri = request.RequestUri?.ToString();
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
