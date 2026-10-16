using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YumGo.Api.Contracts.Restaurants;
using YumGo.Application.Restaurants;
using YumGo.Infrastructure.Restaurants;

namespace YumGo.Api.Endpoints;

public static class RestaurantManagementEndpointMapping
{
    public static IEndpointRouteBuilder MapRestaurantManagementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/restaurant/restaurants")
            .WithTags("Restaurant management")
            .RequireAuthorization();

        group.MapGet("", async (HttpContext context, RestaurantBranchHandler handler, CancellationToken cancellationToken) =>
        {
            var result = await handler.ListRestaurantsAsync(GetUserId(context), cancellationToken);
            return ToResult(result, static value => Results.Ok(new { data = value.Restaurants }));
        }).WithName("ListOwnedRestaurants");

        group.MapPost("", async (CreateRestaurantRequest request, HttpContext context,
            RestaurantBranchHandler handler, CancellationToken cancellationToken) =>
        {
            var result = await handler.CreateRestaurantAsync(
                new CreateRestaurantCommand(GetUserId(context), request.Name, request.Description), cancellationToken);
            return ToResult(result, static value => Results.Created(
                $"/api/v1/restaurant/restaurants/{value.Restaurant!.Id}", new { data = value.Restaurant }));
        }).WithName("CreateRestaurant");

        group.MapGet("/{restaurantId}/branches", async (string restaurantId, HttpContext context,
            RestaurantBranchHandler handler, CancellationToken cancellationToken) =>
        {
            var result = await handler.ListBranchesAsync(GetUserId(context), restaurantId, cancellationToken);
            return ToResult(result, static value => Results.Ok(new { data = value.Branches }));
        }).WithName("ListOwnedRestaurantBranches");

        group.MapPost("/{restaurantId}/branches", async (string restaurantId,
            CreateRestaurantBranchRequest request, HttpContext context, RestaurantBranchHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.CreateBranchAsync(GetUserId(context), restaurantId,
                new CreateRestaurantBranchCommand(request.Name, request.Phone, request.AddressLine, request.Ward,
                    request.District, request.City, request.Latitude, request.Longitude, request.OpensAt, request.ClosesAt),
                cancellationToken);
            return ToResult(result, static value => Results.Created(
                $"/api/v1/restaurant/restaurants/{value.Branch!.RestaurantId}/branches/{value.Branch.Id}",
                new { data = value.Branch }));
        }).WithName("CreateRestaurantBranch");

        return endpoints;
    }

    private static string? GetUserId(HttpContext context) =>
        context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

    private static IResult ToResult(RestaurantBranchResult result, Func<RestaurantBranchResult, IResult> success) => result.Status switch
    {
        RestaurantBranchStatus.Success or RestaurantBranchStatus.Created => success(result),
        RestaurantBranchStatus.ValidationFailed => Results.ValidationProblem(
            result.Errors?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? new Dictionary<string, string[]>(),
            statusCode: StatusCodes.Status422UnprocessableEntity),
        RestaurantBranchStatus.NotFound => Results.NotFound(),
        RestaurantBranchStatus.DuplicateName => Results.Problem(
            statusCode: StatusCodes.Status409Conflict, title: "Branch name already exists"),
        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };
}
