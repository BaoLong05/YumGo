using YumGo.Application.Restaurants;

namespace YumGo.Api.Endpoints;

public static class RestaurantDiscoveryEndpointMapping
{
    public static IEndpointRouteBuilder MapRestaurantDiscoveryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/restaurants/{restaurantId}", async (
            string restaurantId, RestaurantDiscoveryHandler handler, CancellationToken cancellationToken) =>
            ToResourceResult(await handler.GetRestaurantAsync(restaurantId, cancellationToken), result => Results.Ok(new { data = result.Restaurant })))
            .WithName("GetPublicRestaurant").WithTags("Restaurant discovery").AllowAnonymous();

        endpoints.MapGet("/api/v1/restaurants/{restaurantId}/branches", async (
            string restaurantId, RestaurantDiscoveryHandler handler, CancellationToken cancellationToken) =>
            ToResourceResult(await handler.GetBranchesAsync(restaurantId, cancellationToken), result => Results.Ok(new { data = result.Branches })))
            .WithName("GetPublicRestaurantBranches").WithTags("Restaurant discovery").AllowAnonymous();

        endpoints.MapGet("/api/v1/branches/{branchId}", async (
            string branchId, RestaurantDiscoveryHandler handler, CancellationToken cancellationToken) =>
            ToResourceResult(await handler.GetBranchAsync(branchId, cancellationToken), result => Results.Ok(new { data = result.Branch })))
            .WithName("GetPublicBranch").WithTags("Restaurant discovery").AllowAnonymous();

        endpoints.MapGet("/api/v1/restaurants", async (
            string? q, string? city, string? district, bool? openNow, string? categoryId,
            string? cursor, int? limit, RestaurantDiscoveryHandler handler, CancellationToken cancellationToken) =>
        {
            var result = await handler.SearchAsync(q, city, district, openNow, categoryId, cursor, limit, cancellationToken);
            return result.Status switch
            {
                RestaurantDiscoveryStatus.Success => Results.Ok(new
                {
                    data = result.Page!.Items,
                    meta = new { result.Page.NextCursor, result.Page.HasMore }
                }),
                RestaurantDiscoveryStatus.ValidationFailed => Results.ValidationProblem(
                    result.Errors!.ToDictionary(pair => pair.Key, pair => pair.Value),
                    statusCode: StatusCodes.Status422UnprocessableEntity),
                _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
            };
        })
            .WithName("SearchRestaurants")
            .WithTags("Restaurant discovery")
            .AllowAnonymous();

        endpoints.MapGet("/api/v1/branches/{branchId}/menu", async (
            string branchId, RestaurantDiscoveryHandler handler, CancellationToken cancellationToken) =>
        {
            var result = await handler.GetPublicMenuAsync(branchId, cancellationToken);
            return result.Status switch
            {
                RestaurantDiscoveryStatus.Success => Results.Ok(new { data = result.Menu }),
                RestaurantDiscoveryStatus.ValidationFailed => Results.ValidationProblem(
                    result.Errors!.ToDictionary(pair => pair.Key, pair => pair.Value),
                    statusCode: StatusCodes.Status422UnprocessableEntity),
                RestaurantDiscoveryStatus.NotFound => Results.NotFound(),
                _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
            };
        })
            .WithName("GetPublicBranchMenu")
            .WithTags("Restaurant discovery")
            .AllowAnonymous();
        return endpoints;
    }

    private static IResult ToResourceResult(RestaurantDiscoveryResult result, Func<RestaurantDiscoveryResult, IResult> success) =>
        result.Status switch
        {
            RestaurantDiscoveryStatus.Success => success(result),
            RestaurantDiscoveryStatus.ValidationFailed => Results.ValidationProblem(
                result.Errors!.ToDictionary(pair => pair.Key, pair => pair.Value),
                statusCode: StatusCodes.Status422UnprocessableEntity),
            RestaurantDiscoveryStatus.NotFound => Results.NotFound(),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
}
