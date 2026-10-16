using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YumGo.Api.Contracts.Menu;
using YumGo.Application.Menu;

namespace YumGo.Api.Endpoints;

public static class RestaurantMenuEndpointMapping
{
    public static IEndpointRouteBuilder MapRestaurantMenuEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/restaurant/menu")
            .WithTags("Restaurant menu management")
            .RequireAuthorization();

        endpoints.MapGet("/api/v1/restaurant/branches/{branchId}/menu", async (
            string branchId, HttpContext context, RestaurantMenuHandler handler, CancellationToken cancellationToken) =>
        {
            var result = await handler.GetBranchMenuAsync(
                context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, branchId, cancellationToken);
            return ToResult(result, value => Results.Ok(new { data = value.Menu }));
        })
            .WithName("GetRestaurantBranchMenu")
            .WithTags("Restaurant menu management")
            .RequireAuthorization();

        group.MapPost("/categories", async (
            CreateMenuCategoryRequest request, HttpContext context,
            RestaurantMenuHandler handler, CancellationToken cancellationToken) =>
        {
            var result = await handler.CreateCategoryAsync(
                context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
                request.BranchId, request.Name, request.DisplayOrder, cancellationToken);
            return ToResult(result, value => Results.Created(
                $"/api/v1/restaurant/menu/categories/{value.Category!.Id}", new { data = value.Category }));
        }).WithName("CreateMenuCategory");

        group.MapPatch("/categories/{categoryId}", async (
            string categoryId, UpdateMenuCategoryRequest request, HttpContext context,
            RestaurantMenuHandler handler, CancellationToken cancellationToken) =>
        {
            var result = await handler.UpdateCategoryAsync(
                context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, categoryId,
                request.Name, request.DisplayOrder, request.Status, cancellationToken);
            return ToResult(result, value => Results.Ok(new { data = value.Category }));
        }).WithName("UpdateMenuCategory");

        group.MapPost("/items", async (
            CreateMenuItemRequest request, HttpContext context,
            RestaurantMenuHandler handler, CancellationToken cancellationToken) =>
        {
            var result = await handler.CreateItemAsync(
                context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
                request.BranchId, request.CategoryId, request.Name, request.Description, request.Price, cancellationToken);
            return ToResult(result, value => Results.Created(
                $"/api/v1/restaurant/menu/items/{value.Item!.Id}", new { data = value.Item }));
        }).WithName("CreateMenuItem");

        group.MapPatch("/items/{itemId}", async (
            string itemId, UpdateMenuItemRequest request, HttpContext context,
            RestaurantMenuHandler handler, CancellationToken cancellationToken) =>
        {
            var result = await handler.UpdateItemAsync(
                context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, itemId,
                request.CategoryId, request.Name, request.Description, request.Price, request.Status, cancellationToken);
            return ToResult(result, value => Results.Ok(new { data = value.Item }));
        }).WithName("UpdateMenuItem");

        group.MapPost("/items/{itemId}/sold-out", async (
            string itemId, HttpContext context, RestaurantMenuHandler handler, CancellationToken cancellationToken) =>
        {
            var result = await handler.SetItemAvailabilityAsync(
                context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, itemId, false, cancellationToken);
            return ToResult(result, value => Results.Ok(new { data = value.Item }));
        }).WithName("MarkMenuItemSoldOut");

        group.MapPost("/items/{itemId}/available", async (
            string itemId, HttpContext context, RestaurantMenuHandler handler, CancellationToken cancellationToken) =>
        {
            var result = await handler.SetItemAvailabilityAsync(
                context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, itemId, true, cancellationToken);
            return ToResult(result, value => Results.Ok(new { data = value.Item }));
        }).WithName("MarkMenuItemAvailable");

        return endpoints;
    }

    private static IResult ToResult(RestaurantMenuResult result, Func<RestaurantMenuResult, IResult> success) =>
        result.Status switch
        {
            RestaurantMenuStatus.Created => success(result),
            RestaurantMenuStatus.Updated or RestaurantMenuStatus.Success => success(result),
            RestaurantMenuStatus.ValidationFailed => Results.ValidationProblem(
                result.Errors?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? new Dictionary<string, string[]>(),
                statusCode: StatusCodes.Status422UnprocessableEntity),
            RestaurantMenuStatus.NotFound => Results.NotFound(),
            RestaurantMenuStatus.Conflict => Results.Problem(statusCode: StatusCodes.Status409Conflict),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
}
