using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using YumGo.Application.Identity;
using YumGo.Domain.Common;
using YumGo.Infrastructure.Persistence;

namespace YumGo.Api.IntegrationTests;

public sealed class RestaurantWorkflowEndToEndTests
{
    [Fact]
    public async Task Restaurant_review_branch_visibility_and_staff_roles_enforce_scope_and_audit_changes()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("yumgo_restaurant_workflow_test")
            .WithUsername("postgres")
            .WithPassword(Guid.NewGuid().ToString("N"))
            .Build();
        await postgres.StartAsync();

        var signingKey = TestJwtConfiguration.CreateSigningKey();
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            TestJwtConfiguration.Apply(builder, signingKey);
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = postgres.GetConnectionString()
                }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAuthenticationStore>();
                services.AddSingleton<IAuthenticationStore, ActiveSessionStore>();
            });
        });

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();

        var adminId = NanoId.New().Value;
        var ownerId = NanoId.New().Value;
        var otherOwnerId = NanoId.New().Value;
        var employeeId = NanoId.New().Value;
        var unverifiedEmployeeId = NanoId.New().Value;
        var rejectedRestaurantId = NanoId.New().Value;
        var activeRestaurantId = NanoId.New().Value;
        var pendingRestaurantId = NanoId.New().Value;
        var activeBranchId = NanoId.New().Value;
        var pendingBranchId = NanoId.New().Value;
        await SeedAsync(dbContext, adminId, ownerId, otherOwnerId, employeeId, unverifiedEmployeeId,
            rejectedRestaurantId, activeRestaurantId, pendingRestaurantId, activeBranchId, pendingBranchId);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        client.DefaultRequestHeaders.Authorization = Bearer(ownerId, null, signingKey);
        using var nonAdminReview = await client.PostAsJsonAsync(
            $"/api/v1/admin/restaurants/{pendingRestaurantId}/review", new { decision = "approve" });
        Assert.Equal(HttpStatusCode.Forbidden, nonAdminReview.StatusCode);

        client.DefaultRequestHeaders.Authorization = Bearer(adminId, "admin", signingKey);
        using var missingReason = await client.PostAsJsonAsync(
            $"/api/v1/admin/restaurants/{rejectedRestaurantId}/review", new { decision = "reject" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, missingReason.StatusCode);

        using var rejectResponse = await client.PostAsJsonAsync(
            $"/api/v1/admin/restaurants/{rejectedRestaurantId}/review",
            new { decision = "reject", reason = "Required documents are missing." });
        Assert.Equal(HttpStatusCode.OK, rejectResponse.StatusCode);
        using var rejectedBody = await JsonDocument.ParseAsync(await rejectResponse.Content.ReadAsStreamAsync());
        Assert.Equal("rejected", rejectedBody.RootElement.GetProperty("data").GetProperty("status").GetString());

        using var duplicateReview = await client.PostAsJsonAsync(
            $"/api/v1/admin/restaurants/{rejectedRestaurantId}/review", new { decision = "approve" });
        Assert.Equal(HttpStatusCode.Conflict, duplicateReview.StatusCode);

        using var approveResponse = await client.PostAsJsonAsync(
            $"/api/v1/admin/restaurants/{pendingRestaurantId}/review", new { decision = "approve" });
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        using var approvedBody = await JsonDocument.ParseAsync(await approveResponse.Content.ReadAsStreamAsync());
        Assert.Equal("active", approvedBody.RootElement.GetProperty("data").GetProperty("status").GetString());

        client.DefaultRequestHeaders.Authorization = Bearer(ownerId, null, signingKey);
        using var openBranch = await client.PatchAsJsonAsync(
            $"/api/v1/restaurant/restaurants/{activeRestaurantId}/branches/{activeBranchId}/status",
            new { status = "open" });
        Assert.Equal(HttpStatusCode.OK, openBranch.StatusCode);
        using var openBody = await JsonDocument.ParseAsync(await openBranch.Content.ReadAsStreamAsync());
        Assert.Equal("open", openBody.RootElement.GetProperty("data").GetProperty("status").GetString());

        using var repeatedOpenBranch = await client.PatchAsJsonAsync(
            $"/api/v1/restaurant/restaurants/{activeRestaurantId}/branches/{activeBranchId}/status",
            new { status = "open" });
        Assert.Equal(HttpStatusCode.OK, repeatedOpenBranch.StatusCode);

        using var closeBranch = await client.PatchAsJsonAsync(
            $"/api/v1/restaurant/restaurants/{activeRestaurantId}/branches/{activeBranchId}/status",
            new { status = "closed" });
        Assert.Equal(HttpStatusCode.OK, closeBranch.StatusCode);
        using var closeBody = await JsonDocument.ParseAsync(await closeBranch.Content.ReadAsStreamAsync());
        Assert.Equal("closed", closeBody.RootElement.GetProperty("data").GetProperty("status").GetString());

        using var rejectedBranchOpen = await client.PatchAsJsonAsync(
            $"/api/v1/restaurant/restaurants/{rejectedRestaurantId}/branches/{pendingBranchId}/status",
            new { status = "open" });
        Assert.Equal(HttpStatusCode.Conflict, rejectedBranchOpen.StatusCode);

        client.DefaultRequestHeaders.Authorization = Bearer(otherOwnerId, null, signingKey);
        using var foreignBranchOpen = await client.PatchAsJsonAsync(
            $"/api/v1/restaurant/restaurants/{activeRestaurantId}/branches/{activeBranchId}/status",
            new { status = "open" });
        Assert.Equal(HttpStatusCode.NotFound, foreignBranchOpen.StatusCode);

        client.DefaultRequestHeaders.Authorization = Bearer(ownerId, null, signingKey);
        using var assignRoles = await client.PutAsJsonAsync(
            $"/api/v1/restaurant/restaurants/{activeRestaurantId}/staff/{employeeId}/roles",
            new { roles = new[] { "manager", "staff" } });
        Assert.Equal(HttpStatusCode.OK, assignRoles.StatusCode);
        using var rolesBody = await JsonDocument.ParseAsync(await assignRoles.Content.ReadAsStreamAsync());
        Assert.Equal(new[] { "manager", "staff" }, rolesBody.RootElement.GetProperty("data").GetProperty("roles")
            .EnumerateArray().Select(role => role.GetString()).ToArray());

        using var repeatedAssignRoles = await client.PutAsJsonAsync(
            $"/api/v1/restaurant/restaurants/{activeRestaurantId}/staff/{employeeId}/roles",
            new { roles = new[] { "staff", "manager" } });
        Assert.Equal(HttpStatusCode.OK, repeatedAssignRoles.StatusCode);

        using var removeRoles = await client.PutAsJsonAsync(
            $"/api/v1/restaurant/restaurants/{activeRestaurantId}/staff/{employeeId}/roles",
            new { roles = Array.Empty<string>() });
        Assert.Equal(HttpStatusCode.OK, removeRoles.StatusCode);
        using var removedBody = await JsonDocument.ParseAsync(await removeRoles.Content.ReadAsStreamAsync());
        Assert.Empty(removedBody.RootElement.GetProperty("data").GetProperty("roles").EnumerateArray());

        using var unverifiedUserAssignment = await client.PutAsJsonAsync(
            $"/api/v1/restaurant/restaurants/{activeRestaurantId}/staff/{unverifiedEmployeeId}/roles",
            new { roles = new[] { "staff" } });
        Assert.Equal(HttpStatusCode.NotFound, unverifiedUserAssignment.StatusCode);

        using var invalidRole = await client.PutAsJsonAsync(
            $"/api/v1/restaurant/restaurants/{activeRestaurantId}/staff/{employeeId}/roles",
            new { roles = new[] { "admin" } });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidRole.StatusCode);

        Assert.Equal(1, await CountAsync(dbContext, "restaurant.review.approved"));
        Assert.Equal(1, await CountAsync(dbContext, "restaurant.review.rejected"));
        Assert.Equal(2, await CountAsync(dbContext, "restaurant.branch.status.changed"));
        Assert.Equal(2, await CountAsync(dbContext, "restaurant.staff.roles.updated"));
        Assert.Equal(2, await dbContext.Database
            .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM restaurant_staff WHERE user_id = {employeeId} AND status = 'inactive'")
            .SingleAsync());
    }

    private static async Task SeedAsync(
        ApplicationDbContext context,
        string adminId,
        string ownerId,
        string otherOwnerId,
        string employeeId,
        string unverifiedEmployeeId,
        string rejectedRestaurantId,
        string activeRestaurantId,
        string pendingRestaurantId,
        string activeBranchId,
        string pendingBranchId)
    {
        var now = DateTimeOffset.UtcNow;
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO users (id, email, password_hash, status, email_verified_at, created_at, updated_at)
            VALUES ({adminId}, 'workflow-admin@example.test', 'hash', 'active', {now}, {now}, {now}),
                   ({ownerId}, 'workflow-owner@example.test', 'hash', 'active', {now}, {now}, {now}),
                   ({otherOwnerId}, 'workflow-other@example.test', 'hash', 'active', {now}, {now}, {now}),
                   ({employeeId}, 'workflow-employee@example.test', 'hash', 'active', {now}, {now}, {now}),
                   ({unverifiedEmployeeId}, 'workflow-unverified@example.test', 'hash', 'active', NULL, {now}, {now})
            """);

        var adminRoleId = await context.Database
            .SqlQueryRaw<string>("SELECT id AS \"Value\" FROM roles WHERE code = 'admin'")
            .SingleAsync();
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO user_roles (id, user_id, role_id, created_at)
            VALUES ({NanoId.New().Value}, {adminId}, {adminRoleId}, {now})
            """);

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO restaurants (id, owner_user_id, name, description, status, created_at, updated_at)
            VALUES ({rejectedRestaurantId}, {ownerId}, 'Rejected Restaurant', NULL, 'pending', {now}, {now}),
                   ({activeRestaurantId}, {ownerId}, 'Active Restaurant', NULL, 'active', {now}, {now}),
                   ({pendingRestaurantId}, {ownerId}, 'Pending Restaurant', NULL, 'pending', {now}, {now})
            """);
        await SeedBranchAsync(context, activeBranchId, activeRestaurantId, "Active Branch", now);
        await SeedBranchAsync(context, pendingBranchId, rejectedRestaurantId, "Pending Branch", now);
    }

    private static Task<int> CountAsync(ApplicationDbContext context, string action) =>
        context.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM audit_logs WHERE action = {action}").SingleAsync();

    private static Task SeedBranchAsync(ApplicationDbContext context, string branchId, string restaurantId, string name, DateTimeOffset now) =>
        context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO restaurant_branches (
                id, restaurant_id, name, phone, address_line, ward, district, city, latitude, longitude,
                status, opens_at, closes_at, created_at, updated_at)
            VALUES ({branchId}, {restaurantId}, {name}, '+84912345678', '12 Nguyen Hue', 'Ben Nghe', 'District 1', 'HCMC',
                    10.776900, 106.700900, 'draft', NULL, NULL, {now}, {now})
            """);

    private static AuthenticationHeaderValue Bearer(string userId, string? role, string signingKey) =>
        new("Bearer", CreateToken(userId, role, signingKey));

    private static string CreateToken(string userId, string? role, string signingKey)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new("sid", NanoId.New().Value)
        };
        if (role is not null) claims.Add(new Claim("role", role));

        var token = new JwtSecurityToken(
            issuer: "YumGo.Tests",
            audience: "YumGo.Tests.Client",
            claims: claims,
            notBefore: now.AddMinutes(-1),
            expires: now.AddMinutes(10),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Convert.FromBase64String(signingKey)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class ActiveSessionStore : IAuthenticationStore
    {
        public Task<AuthenticationAccount?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task CreateRefreshTokenAsync(string id, string userId, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset createdAt, string? ipAddress, string? userAgent, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<RefreshTokenSession?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> RotateRefreshTokenAsync(string oldTokenId, string oldTokenHash, string newTokenId, string userId, string newTokenHash, DateTimeOffset expiresAt, DateTimeOffset now, string? ipAddress, string? userAgent, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task RevokeRefreshTokenDescendantsAsync(string tokenId, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> IsSessionActiveAsync(string userId, string sessionId, DateTimeOffset now, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task RecordAuthenticationAuditAsync(string? actorUserId, string action, string? ipAddress, string? userAgent, DateTimeOffset createdAt, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
