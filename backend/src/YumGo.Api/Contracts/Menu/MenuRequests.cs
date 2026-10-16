using System.Text.Json.Serialization;

namespace YumGo.Api.Contracts.Menu;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateMenuCategoryRequest(string? BranchId, string? Name, int? DisplayOrder);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateMenuCategoryRequest(string? Name, int? DisplayOrder, string? Status);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateMenuItemRequest(
    string? BranchId, string? CategoryId, string? Name, string? Description, decimal? Price);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateMenuItemRequest(
    string? CategoryId, string? Name, string? Description, decimal? Price, string? Status);
