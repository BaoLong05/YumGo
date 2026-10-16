using YumGo.Domain.Addresses;
using YumGo.Domain.Common;

namespace YumGo.Application.Addresses;

public interface ICustomerAddressStore
{
    Task<IReadOnlyList<CustomerAddress>> ListAsync(NanoId userId, CancellationToken cancellationToken);
    Task<CustomerAddress?> FindAsync(NanoId userId, NanoId addressId, CancellationToken cancellationToken);
    Task CreateAsync(CustomerAddress address, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(CustomerAddress address, bool? isDefaultOverride, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(NanoId userId, NanoId addressId, CancellationToken cancellationToken);
    Task<bool> SetDefaultAsync(NanoId userId, NanoId addressId, DateTimeOffset now, CancellationToken cancellationToken);
}

public interface IAddressReverseGeocoder
{
    Task<ReverseGeocodedAddress?> ReverseGeocodeAsync(
        decimal latitude,
        decimal longitude,
        CancellationToken cancellationToken);
}

public interface IGoogleGeocodingQuota
{
    Task<bool> TryReserveAsync(int monthlyLimit, CancellationToken cancellationToken);
}

public sealed record ReverseGeocodedAddress(string AddressLine, string Ward, string District, string City);

public sealed record CreateCustomerAddressCommand(
    string? UserId,
    string? Label,
    string? RecipientName,
    string? RecipientPhone,
    decimal? Latitude,
    decimal? Longitude,
    bool IsDefault);

public sealed record UpdateCustomerAddressCommand(
    string? Label,
    string? RecipientName,
    string? RecipientPhone,
    decimal? Latitude,
    decimal? Longitude,
    bool? IsDefault);

public sealed record CustomerAddressDto(
    string Id,
    string Label,
    string RecipientName,
    string RecipientPhone,
    string AddressLine,
    string Ward,
    string District,
    string City,
    decimal Latitude,
    decimal Longitude,
    bool IsDefault,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public enum CustomerAddressStatus
{
    Success,
    Created,
    ValidationFailed,
    NotFound,
    LocationNotFound,
    GeocodingUnavailable,
    GeocodingQuotaExceeded
}

public sealed record CustomerAddressResult(
    CustomerAddressStatus Status,
    CustomerAddressDto? Address = null,
    IReadOnlyList<CustomerAddressDto>? Addresses = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);

public sealed class GeocodingUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class GeocodingQuotaExceededException()
    : Exception("The monthly Google Geocoding request limit has been reached.");
