using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YumGo.Api.Contracts.Addresses;
using YumGo.Application.Addresses;

namespace YumGo.Api.Endpoints;

public static class CustomerAddressEndpointMapping
{
    public static IEndpointRouteBuilder MapCustomerAddressEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/me/addresses")
            .WithTags("Customer addresses")
            .RequireAuthorization();

        group.MapGet("", async (HttpContext context, CustomerAddressHandler handler, CancellationToken cancellationToken) =>
        {
            var result = await handler.ListAsync(GetUserId(context), cancellationToken);
            return result.Status == CustomerAddressStatus.Success
                ? Results.Ok(new { data = result.Addresses })
                : ToProblem(result);
        }).WithName("ListCustomerAddresses");

        group.MapPost("", async (
            CreateCustomerAddressRequest request,
            HttpContext context,
            CustomerAddressHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.CreateAsync(
                new CreateCustomerAddressCommand(
                    GetUserId(context), request.Label, request.RecipientName, request.RecipientPhone,
                    request.Latitude, request.Longitude, request.IsDefault),
                cancellationToken);
            return result.Status == CustomerAddressStatus.Created
                ? Results.Created($"/api/v1/me/addresses/{result.Address!.Id}", new { data = result.Address })
                : ToProblem(result);
        }).WithName("CreateCustomerAddress");

        group.MapPatch("/{id}", async (
            string id,
            UpdateCustomerAddressRequest request,
            HttpContext context,
            CustomerAddressHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.UpdateAsync(
                GetUserId(context), id,
                new UpdateCustomerAddressCommand(
                    request.Label, request.RecipientName, request.RecipientPhone,
                    request.Latitude, request.Longitude, request.IsDefault),
                cancellationToken);
            return result.Status == CustomerAddressStatus.Success
                ? Results.Ok(new { data = result.Address })
                : ToProblem(result);
        }).WithName("UpdateCustomerAddress");

        group.MapDelete("/{id}", async (
            string id,
            HttpContext context,
            CustomerAddressHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.DeleteAsync(GetUserId(context), id, cancellationToken);
            return result.Status == CustomerAddressStatus.Success ? Results.NoContent() : ToProblem(result);
        }).WithName("DeleteCustomerAddress");

        group.MapPost("/{id}/default", async (
            string id,
            HttpContext context,
            CustomerAddressHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.SetDefaultAsync(GetUserId(context), id, cancellationToken);
            return result.Status == CustomerAddressStatus.Success ? Results.NoContent() : ToProblem(result);
        }).WithName("SetDefaultCustomerAddress");

        return endpoints;
    }

    private static string? GetUserId(HttpContext context) =>
        context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

    private static IResult ToProblem(CustomerAddressResult result) => result.Status switch
    {
        CustomerAddressStatus.ValidationFailed => Results.ValidationProblem(
            result.Errors ?? new Dictionary<string, string[]>(),
            statusCode: StatusCodes.Status422UnprocessableEntity),
        CustomerAddressStatus.NotFound => Results.NotFound(),
        CustomerAddressStatus.LocationNotFound => Results.Problem(
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: "Location could not be resolved",
            detail: "Choose a location with a precise delivery address."),
        CustomerAddressStatus.GeocodingUnavailable => Results.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Address lookup unavailable",
            detail: "The address lookup service is temporarily unavailable. Try again later."),
        CustomerAddressStatus.GeocodingQuotaExceeded => Results.Problem(
            statusCode: StatusCodes.Status429TooManyRequests,
            title: "Monthly address lookup limit reached",
            detail: "Address lookup is temporarily unavailable for this month. Try again next month."),
        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };
}
