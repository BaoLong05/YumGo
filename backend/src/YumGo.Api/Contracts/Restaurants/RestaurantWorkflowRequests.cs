using System.Text.Json.Serialization;

namespace YumGo.Api.Contracts.Restaurants;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ReviewRestaurantRequest(string? Decision, string? Reason);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SetRestaurantBranchStatusRequest(string? Status);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ReplaceRestaurantStaffRolesRequest(string?[]? Roles);
