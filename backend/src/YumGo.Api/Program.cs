using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using YumGo.Api.Contracts.Identity;
using YumGo.Application.Identity;
using YumGo.Infrastructure.Registration;
using YumGo.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalWeb", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://127.0.0.1:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddHealthChecks();
builder.Services.AddDbContext<YumGo.Infrastructure.Persistence.ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<RegistrationPasswordPolicy>();
builder.Services.AddScoped<RegisterCustomerHandler>();
builder.Services.AddScoped<ICustomerRegistrationStore, EfCustomerRegistrationStore>();
builder.Services.AddSingleton<IPasswordHashingService, AspNetPasswordHashingService>();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("registration", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();
app.UseCors("LocalWeb");

app.MapHealthChecks("/health");
app.MapPost("/api/v1/auth/register", async (
    RegisterCustomerRequest request,
    RegisterCustomerHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(
        new RegisterCustomerCommand(request.Email, request.Phone, request.Password, request.FullName),
        cancellationToken);

    IResult response = result.Status switch
    {
        RegisterCustomerStatus.Created => Results.Json(
            new { data = new RegisterCustomerResponse(result.Customer!.Id, result.Customer.Email, result.Customer.FullName) },
            statusCode: StatusCodes.Status201Created),
        RegisterCustomerStatus.ValidationFailed => Results.ValidationProblem(
            result.Errors?.ToDictionary(pair => pair.Key, pair => pair.Value)
                ?? new Dictionary<string, string[]>(),
            statusCode: StatusCodes.Status422UnprocessableEntity),
        RegisterCustomerStatus.DuplicateEmail or RegisterCustomerStatus.DuplicatePhone => Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Account already exists",
            detail: "An account with these details already exists."),
        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };

    return response;
})
    .WithName("RegisterCustomer")
    .WithTags("Authentication")
    .RequireRateLimiting("registration");

app.Run();

public partial class Program;
