using YumGo.Domain.Addresses;
using YumGo.Domain.Common;
using YumGo.Domain.Identity;

namespace YumGo.Application.Addresses;

public sealed class CustomerAddressHandler(
    ICustomerAddressStore store,
    IAddressReverseGeocoder reverseGeocoder,
    TimeProvider timeProvider)
{
    public async Task<CustomerAddressResult> ListAsync(string? userId, CancellationToken cancellationToken)
    {
        if (!TryParseId(userId, out var parsedUserId))
        {
            return ValidationError("user", "The authenticated user ID is invalid.");
        }

        var addresses = await store.ListAsync(parsedUserId, cancellationToken);
        return new CustomerAddressResult(CustomerAddressStatus.Success, Addresses: addresses.Select(Map).ToArray());
    }

    public async Task<CustomerAddressResult> CreateAsync(CreateCustomerAddressCommand command, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        if (!TryParseId(command.UserId, out var userId)) AddError(errors, "user", "The authenticated user ID is invalid.");
        var label = ValidateText(command.Label, "label", 1, 40, errors);
        var recipientName = ValidateText(command.RecipientName, "recipientName", 2, 120, errors);
        var phone = ParsePhone(command.RecipientPhone, errors);
        ValidateCoordinates(command.Latitude, command.Longitude, errors);
        if (errors.Count > 0) return Invalid(errors);

        var latitude = decimal.Round(command.Latitude!.Value, 6, MidpointRounding.AwayFromZero);
        var longitude = decimal.Round(command.Longitude!.Value, 6, MidpointRounding.AwayFromZero);
        ReverseGeocodedAddress? geocoded;
        try
        {
            geocoded = await reverseGeocoder.ReverseGeocodeAsync(latitude, longitude, cancellationToken);
        }
        catch (GeocodingQuotaExceededException)
        {
            return new CustomerAddressResult(CustomerAddressStatus.GeocodingQuotaExceeded);
        }
        catch (GeocodingUnavailableException)
        {
            return new CustomerAddressResult(CustomerAddressStatus.GeocodingUnavailable);
        }

        if (geocoded is null) return new CustomerAddressResult(CustomerAddressStatus.LocationNotFound);

        CustomerAddress address;
        try
        {
            address = CustomerAddress.Create(
                NanoId.New(), userId, label!, recipientName!, phone!, geocoded.AddressLine, geocoded.Ward,
                geocoded.District, geocoded.City, latitude, longitude,
                command.IsDefault, timeProvider.GetUtcNow());
        }
        catch (ArgumentException)
        {
            return ValidationError("location", "The location did not resolve to a complete delivery address.");
        }

        await store.CreateAsync(address, cancellationToken);
        return new CustomerAddressResult(CustomerAddressStatus.Created, Address: Map(address));
    }

    public async Task<CustomerAddressResult> UpdateAsync(
        string? userIdValue,
        string? addressIdValue,
        UpdateCustomerAddressCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryParseId(userIdValue, out var userId) || !TryParseId(addressIdValue, out var addressId))
            return new CustomerAddressResult(CustomerAddressStatus.NotFound);

        var address = await store.FindAsync(userId, addressId, cancellationToken);
        if (address is null) return new CustomerAddressResult(CustomerAddressStatus.NotFound);

        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var label = command.Label is null ? address.Label : ValidateText(command.Label, "label", 1, 40, errors);
        var recipientName = command.RecipientName is null
            ? address.RecipientName
            : ValidateText(command.RecipientName, "recipientName", 2, 120, errors);
        var phone = command.RecipientPhone is null ? address.RecipientPhone : ParsePhone(command.RecipientPhone, errors);
        if (command.Latitude.HasValue != command.Longitude.HasValue)
            AddError(errors, "latitude", "Latitude and longitude must be provided together.");
        if (command.Latitude.HasValue && command.Longitude.HasValue)
            ValidateCoordinates(command.Latitude, command.Longitude, errors);
        if (errors.Count > 0) return Invalid(errors);

        var latitude = decimal.Round(command.Latitude ?? address.Latitude, 6, MidpointRounding.AwayFromZero);
        var longitude = decimal.Round(command.Longitude ?? address.Longitude, 6, MidpointRounding.AwayFromZero);
        var geocoded = new ReverseGeocodedAddress(address.AddressLine, address.Ward, address.District, address.City);
        if (command.Latitude.HasValue
            && (latitude != address.Latitude || longitude != address.Longitude))
        {
            try
            {
                var updatedLocation = await reverseGeocoder.ReverseGeocodeAsync(latitude, longitude, cancellationToken);
                if (updatedLocation is null)
                    return new CustomerAddressResult(CustomerAddressStatus.LocationNotFound);
                geocoded = updatedLocation;
            }
            catch (GeocodingQuotaExceededException)
            {
                return new CustomerAddressResult(CustomerAddressStatus.GeocodingQuotaExceeded);
            }
            catch (GeocodingUnavailableException)
            {
                return new CustomerAddressResult(CustomerAddressStatus.GeocodingUnavailable);
            }
        }

        try
        {
            address.Update(label!, recipientName!, phone!, geocoded.AddressLine, geocoded.Ward, geocoded.District,
                geocoded.City, latitude, longitude, timeProvider.GetUtcNow());
        }
        catch (ArgumentException)
        {
            return ValidationError("location", "The location did not resolve to a complete delivery address.");
        }

        if (command.IsDefault.HasValue) address.SetDefault(command.IsDefault.Value, timeProvider.GetUtcNow());
        if (!await store.UpdateAsync(address, command.IsDefault, cancellationToken))
            return new CustomerAddressResult(CustomerAddressStatus.NotFound);
        var savedAddress = await store.FindAsync(userId, addressId, cancellationToken);
        return savedAddress is null
            ? new CustomerAddressResult(CustomerAddressStatus.NotFound)
            : new CustomerAddressResult(CustomerAddressStatus.Success, Address: Map(savedAddress));
    }

    public async Task<CustomerAddressResult> DeleteAsync(string? userIdValue, string? addressIdValue, CancellationToken cancellationToken)
    {
        if (!TryParseId(userIdValue, out var userId) || !TryParseId(addressIdValue, out var addressId))
            return new CustomerAddressResult(CustomerAddressStatus.NotFound);
        return await store.DeleteAsync(userId, addressId, cancellationToken)
            ? new CustomerAddressResult(CustomerAddressStatus.Success)
            : new CustomerAddressResult(CustomerAddressStatus.NotFound);
    }

    public async Task<CustomerAddressResult> SetDefaultAsync(string? userIdValue, string? addressIdValue, CancellationToken cancellationToken)
    {
        if (!TryParseId(userIdValue, out var userId) || !TryParseId(addressIdValue, out var addressId))
            return new CustomerAddressResult(CustomerAddressStatus.NotFound);
        return await store.SetDefaultAsync(userId, addressId, timeProvider.GetUtcNow(), cancellationToken)
            ? new CustomerAddressResult(CustomerAddressStatus.Success)
            : new CustomerAddressResult(CustomerAddressStatus.NotFound);
    }

    private static CustomerAddressDto Map(CustomerAddress address) => new(
        address.Id.Value, address.Label, address.RecipientName, address.RecipientPhone.Value,
        address.AddressLine, address.Ward, address.District, address.City, address.Latitude,
        address.Longitude, address.IsDefault, address.CreatedAt, address.UpdatedAt);

    private static string? ValidateText(string? value, string field, int minimumLength, int maximumLength, IDictionary<string, List<string>> errors)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length < minimumLength || value.Trim().Length > maximumLength)
        {
            AddError(errors, field, $"{field} must contain between {minimumLength} and {maximumLength} characters.");
            return null;
        }
        return value.Trim();
    }

    private static PhoneNumber? ParsePhone(string? value, IDictionary<string, List<string>> errors)
    {
        try { return PhoneNumber.Create(value ?? string.Empty); }
        catch (ArgumentException)
        {
            AddError(errors, "recipientPhone", "Enter a valid international phone number.");
            return null;
        }
    }

    private static void ValidateCoordinates(decimal? latitude, decimal? longitude, IDictionary<string, List<string>> errors)
    {
        if (!latitude.HasValue || !longitude.HasValue)
        {
            AddError(errors, "latitude", "A current location with latitude and longitude is required.");
            return;
        }
        if (latitude is < -90 or > 90) AddError(errors, "latitude", "Latitude must be between -90 and 90.");
        if (longitude is < -180 or > 180) AddError(errors, "longitude", "Longitude must be between -180 and 180.");
    }

    private static bool TryParseId(string? value, out NanoId id)
    {
        try { id = NanoId.Create(value ?? string.Empty); return true; }
        catch (ArgumentException) { id = null!; return false; }
    }

    private static CustomerAddressResult Invalid(IDictionary<string, List<string>> errors) =>
        new(CustomerAddressStatus.ValidationFailed,
            Errors: errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal));

    private static CustomerAddressResult ValidationError(string field, string message) =>
        Invalid(new Dictionary<string, List<string>>(StringComparer.Ordinal) { [field] = [message] });

    private static void AddError(IDictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.TryGetValue(field, out var messages)) errors.Add(field, messages = []);
        messages.Add(message);
    }
}
