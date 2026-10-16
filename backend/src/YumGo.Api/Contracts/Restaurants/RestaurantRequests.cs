using System.Text.Json.Serialization;

namespace YumGo.Api.Contracts.Restaurants;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateRestaurantRequest(string? Name, string? Description);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateRestaurantBranchRequest(
    string? Name,
    string? Phone,
    string? AddressLine,
    string? Ward,
    string? District,
    string? City,
    decimal? Latitude,
    decimal? Longitude,
    TimeOnly? OpensAt,
    TimeOnly? ClosesAt);
