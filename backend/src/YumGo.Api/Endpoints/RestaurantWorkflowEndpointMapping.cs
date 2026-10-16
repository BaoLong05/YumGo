using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YumGo.Api.Contracts.Restaurants;
using YumGo.Application.Restaurants;

namespace YumGo.Api.Endpoints;

public static class RestaurantWorkflowEndpointMapping
{
    public static IEndpointRouteBuilder MapRestaurantWorkflowEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/admin/restaurants/{restaurantId}/review",
            async (string restaurantId, ReviewRestaurantRequest request, HttpContext context,
                RestaurantWorkflowHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.ReviewRestaurantAsync(
                    GetUserId(context), restaurantId, request.Decision, request.Reason, cancellationToken);
                return ToResult(result, static value => Results.Ok(new { data = value.Restaurant }));
            })
            .WithName("ReviewRestaurant")
            .WithTags("Restaurant administration")
            .RequireAuthorization(policy => policy.RequireRole("admin"));

        var ownerGroup = endpoints.MapGroup("/api/v1/restaurant/restaurants")
            .WithTags("Restaurant management")
            .RequireAuthorization();

        ownerGroup.MapPatch("/{restaurantId}/branches/{branchId}/status",
            async (string restaurantId, string branchId, SetRestaurantBranchStatusRequest request,
                HttpContext context, RestaurantWorkflowHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.SetBranchVisibilityAsync(
                    GetUserId(context), restaurantId, branchId, request.Status, cancellationToken);
                return ToResult(result, static value => Results.Ok(new { data = value.Branch }));
            })
            .WithName("SetRestaurantBranchVisibility");

        ownerGroup.MapPut("/{restaurantId}/staff/{employeeUserId}/roles",
            async (string restaurantId, string employeeUserId, ReplaceRestaurantStaffRolesRequest request,
                HttpContext context, RestaurantWorkflowHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.ReplaceStaffRolesAsync(
                    GetUserId(context), restaurantId, employeeUserId, request.Roles, cancellationToken);
                return ToResult(result, static value => Results.Ok(new { data = value.Staff }));
            })
            .WithName("ReplaceRestaurantStaffRoles");

        return endpoints;
    }

    private static string? GetUserId(HttpContext context) =>
        context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

    private static IResult ToResult(RestaurantWorkflowResult result, Func<RestaurantWorkflowResult, IResult> success) =>
        result.Status switch
        {
            RestaurantWorkflowStatus.Success => success(result),
            RestaurantWorkflowStatus.ValidationFailed => Results.ValidationProblem(
                result.Errors?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? new Dictionary<string, string[]>(),
                statusCode: StatusCodes.Status422UnprocessableEntity),
            RestaurantWorkflowStatus.NotFound => Results.NotFound(),
            RestaurantWorkflowStatus.Conflict => Results.Problem(statusCode: StatusCodes.Status409Conflict),
            RestaurantWorkflowStatus.Forbidden => Results.Forbid(),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
}
