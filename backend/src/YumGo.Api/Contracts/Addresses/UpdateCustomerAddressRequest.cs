using System.Text.Json.Serialization;

namespace YumGo.Api.Contracts.Addresses;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateCustomerAddressRequest(
    string? Label,
    string? RecipientName,
    string? RecipientPhone,
    decimal? Latitude,
    decimal? Longitude,
    bool? IsDefault);
