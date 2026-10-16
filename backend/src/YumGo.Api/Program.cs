using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using YumGo.Api.Authentication;
using YumGo.Api.Endpoints;
using YumGo.Api.Contracts.Identity;
using YumGo.Application.Addresses;
using YumGo.Application.Identity;
using YumGo.Application.Restaurants;
using YumGo.Application.Menu;
using YumGo.Infrastructure.Identity;
using YumGo.Infrastructure.Addresses;
using YumGo.Infrastructure.Restaurants;
using YumGo.Infrastructure.Menu;
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
builder.Services.AddScoped<LoginHandler>();
builder.Services.AddScoped<RefreshSessionHandler>();
builder.Services.AddScoped<LogoutSessionHandler>();
builder.Services.AddScoped<CustomerAddressHandler>();
builder.Services.AddScoped<RestaurantBranchHandler>();
builder.Services.AddScoped<RestaurantWorkflowHandler>();
builder.Services.AddScoped<RestaurantMenuHandler>();
builder.Services.AddScoped<IAuthenticationStore, EfAuthenticationStore>();
builder.Services.AddScoped<ICustomerAddressStore, EfCustomerAddressStore>();
builder.Services.AddScoped<IRestaurantBranchStore, EfRestaurantBranchStore>();
builder.Services.AddScoped<IRestaurantWorkflowStore, EfRestaurantWorkflowStore>();
builder.Services.AddScoped<IRestaurantMenuStore, EfRestaurantMenuStore>();
builder.Services.AddScoped<IGoogleGeocodingQuota, PostgresGoogleGeocodingQuotaStore>();
builder.Services.AddHttpClient<IAddressReverseGeocoder, GoogleAddressReverseGeocoder>(client =>
    client.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddScoped<ICustomerRegistrationStore, EfCustomerRegistrationStore>();
builder.Services.AddSingleton<IPasswordHashingService, AspNetPasswordHashingService>();
builder.Services.AddSingleton<IRefreshTokenGenerator, CryptographicRefreshTokenGenerator>();
builder.Services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<JwtTokenSettings>(builder.Configuration.GetSection(JwtTokenSettings.SectionName));
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtTokenSettings>>((options, settingsAccessor) =>
    {
        var jwtSettings = settingsAccessor.Value;
        jwtSettings.Validate();
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwtSettings.CreateSigningKey(),
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = "role",
            NameClaimType = "sub",
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userId = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                var sessionId = context.Principal?.FindFirst("sid")?.Value;
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sessionId))
                {
                    context.Fail("The access token is not bound to an active session.");
                    return;
                }

                var store = context.HttpContext.RequestServices.GetRequiredService<IAuthenticationStore>();
                var timeProvider = context.HttpContext.RequestServices.GetRequiredService<TimeProvider>();
                var isActive = await store.IsSessionActiveAsync(
                    userId,
                    sessionId,
                    timeProvider.GetUtcNow(),
                    context.HttpContext.RequestAborted);
                if (!isActive)
                {
                    context.Fail("The access token session is no longer active.");
                }
            }
        };
    });
builder.Services.AddAuthorization();
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
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.AddPolicy("refresh", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
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
app.UseCors("LocalWeb");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

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

app.MapPost("/api/v1/auth/login", async (
    LoginRequest request,
    HttpContext context,
    LoginHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(
        new LoginCommand(
            request.Email,
            request.Password,
            context.Connection.RemoteIpAddress?.ToString(),
            context.Request.Headers.UserAgent.ToString()),
        cancellationToken);

    if (result.Status == LoginStatus.InvalidCredentials)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Invalid credentials",
            detail: "The email or password is invalid.");
    }

    var session = result.Session!;
    return Results.Ok(new
    {
        data = new LoginResponse(
            session.AccessToken,
            session.AccessTokenExpiresAt,
            session.RefreshToken,
            session.RefreshTokenExpiresAt,
            "Bearer")
    });
})
    .WithName("Login")
    .WithTags("Authentication")
    .AllowAnonymous()
    .RequireRateLimiting("login");

app.MapPost("/api/v1/auth/refresh", async (
    RefreshSessionRequest request,
    HttpContext context,
    RefreshSessionHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(
        request.RefreshToken,
        context.Connection.RemoteIpAddress?.ToString(),
        context.Request.Headers.UserAgent.ToString(),
        cancellationToken);
    if (result.Status == LoginStatus.InvalidCredentials)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Invalid refresh token",
            detail: "The refresh token is invalid or expired.");
    }

    var session = result.Session!;
    return Results.Ok(new
    {
        data = new LoginResponse(
            session.AccessToken,
            session.AccessTokenExpiresAt,
            session.RefreshToken,
            session.RefreshTokenExpiresAt,
            "Bearer")
    });
})
    .WithName("RefreshSession")
    .WithTags("Authentication")
    .AllowAnonymous()
    .RequireRateLimiting("refresh");

app.MapPost("/api/v1/auth/logout", async (
    HttpContext context,
    LogoutSessionRequest request,
    LogoutSessionHandler handler,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.RefreshToken))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["refreshToken"] = ["A refresh token is required."]
        });
    }

    var userId = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    var sessionId = context.User.FindFirst("sid")?.Value;
    if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sessionId))
    {
        return Results.Unauthorized();
    }

    await handler.HandleAsync(
        userId,
        sessionId,
        request.RefreshToken,
        context.Connection.RemoteIpAddress?.ToString(),
        context.Request.Headers.UserAgent.ToString(),
        cancellationToken);
    return Results.NoContent();
})
    .WithName("LogoutSession")
    .WithTags("Authentication")
    .RequireAuthorization();

app.MapGet("/api/v1/auth/me", (HttpContext context) =>
{
    var userId = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    if (string.IsNullOrWhiteSpace(userId))
    {
        return Results.Unauthorized();
    }

    var roles = context.User.FindAll("role").Select(claim => claim.Value).ToArray();
    return Results.Ok(new { data = new { id = userId, roles } });
})
    .WithName("GetCurrentIdentity")
    .WithTags("Authentication")
    .RequireAuthorization();

app.MapCustomerAddressEndpoints();
app.MapRestaurantManagementEndpoints();
app.MapRestaurantWorkflowEndpoints();
app.MapRestaurantMenuEndpoints();

app.Run();

public partial class Program;
