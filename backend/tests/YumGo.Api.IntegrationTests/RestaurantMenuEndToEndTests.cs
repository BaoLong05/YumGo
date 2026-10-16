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

public sealed class RestaurantMenuEndToEndTests
{
    [Fact]
    public async Task Owner_can_create_a_menu_category_on_an_owned_branch()
    {
        await using var postgres = await StartDatabaseAsync();
        var signingKey = TestJwtConfiguration.CreateSigningKey();
        using var factory = CreateFactory(postgres, signingKey);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();

        var ownerId = NanoId.New().Value;
        var otherOwnerId = NanoId.New().Value;
        var restaurantId = NanoId.New().Value;
        var branchId = NanoId.New().Value;
        await SeedOwnerBranchAsync(dbContext, ownerId, otherOwnerId, restaurantId, branchId);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = Bearer(ownerId, signingKey);

        using var response = await client.PostAsJsonAsync("/api/v1/restaurant/menu/categories", new
        {
            branchId,
            name = "Soups",
            displayOrder = 0
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var category = body.RootElement.GetProperty("data");
        Assert.Equal(branchId, category.GetProperty("branchId").GetString());
        Assert.Equal("Soups", category.GetProperty("name").GetString());
        Assert.Equal(0, category.GetProperty("displayOrder").GetInt32());
        Assert.Equal("active", category.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Owner_can_update_a_category_and_read_the_branch_menu()
    {
        await using var postgres = await StartDatabaseAsync();
        var signingKey = TestJwtConfiguration.CreateSigningKey();
        using var factory = CreateFactory(postgres, signingKey);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();

        var ownerId = NanoId.New().Value;
        var otherOwnerId = NanoId.New().Value;
        var restaurantId = NanoId.New().Value;
        var branchId = NanoId.New().Value;
        await SeedOwnerBranchAsync(dbContext, ownerId, otherOwnerId, restaurantId, branchId);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = Bearer(ownerId, signingKey);
        using var created = await client.PostAsJsonAsync("/api/v1/restaurant/menu/categories",
            new { branchId, name = "Noodles", displayOrder = 1 });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdBody = await JsonDocument.ParseAsync(await created.Content.ReadAsStreamAsync());
        var categoryId = createdBody.RootElement.GetProperty("data").GetProperty("id").GetString();

        using var updated = await client.PatchAsJsonAsync($"/api/v1/restaurant/menu/categories/{categoryId}",
            new { name = "Rice", displayOrder = 0, status = "inactive" });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        using var updatedBody = await JsonDocument.ParseAsync(await updated.Content.ReadAsStreamAsync());
        Assert.Equal("inactive", updatedBody.RootElement.GetProperty("data").GetProperty("status").GetString());

        using var menuResponse = await client.GetAsync($"/api/v1/restaurant/branches/{branchId}/menu");
        Assert.Equal(HttpStatusCode.OK, menuResponse.StatusCode);
        using var menuBody = await JsonDocument.ParseAsync(await menuResponse.Content.ReadAsStreamAsync());
        var categories = menuBody.RootElement.GetProperty("data").GetProperty("categories");
        Assert.Equal(1, categories.GetArrayLength());
        Assert.Equal("Rice", categories[0].GetProperty("name").GetString());
        Assert.Empty(categories[0].GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Owner_can_create_a_menu_item_in_an_active_category()
    {
        await using var postgres = await StartDatabaseAsync();
        var signingKey = TestJwtConfiguration.CreateSigningKey();
        using var factory = CreateFactory(postgres, signingKey);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();

        var ownerId = NanoId.New().Value;
        var otherOwnerId = NanoId.New().Value;
        var restaurantId = NanoId.New().Value;
        var branchId = NanoId.New().Value;
        await SeedOwnerBranchAsync(dbContext, ownerId, otherOwnerId, restaurantId, branchId);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = Bearer(ownerId, signingKey);
        using var categoryResponse = await client.PostAsJsonAsync("/api/v1/restaurant/menu/categories",
            new { branchId, name = "Noodles", displayOrder = 0 });
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);
        using var categoryBody = await JsonDocument.ParseAsync(await categoryResponse.Content.ReadAsStreamAsync());
        var categoryId = categoryBody.RootElement.GetProperty("data").GetProperty("id").GetString();

        using var itemResponse = await client.PostAsJsonAsync("/api/v1/restaurant/menu/items", new
        {
            branchId, categoryId, name = "Beef noodle soup", description = "Spicy broth", price = 12.50m
        });

        Assert.Equal(HttpStatusCode.Created, itemResponse.StatusCode);
        using var itemBody = await JsonDocument.ParseAsync(await itemResponse.Content.ReadAsStreamAsync());
        var item = itemBody.RootElement.GetProperty("data");
        Assert.Equal(branchId, item.GetProperty("branchId").GetString());
        Assert.Equal(categoryId, item.GetProperty("categoryId").GetString());
        Assert.Equal(12.50m, item.GetProperty("price").GetDecimal());
        Assert.Equal("available", item.GetProperty("stockStatus").GetString());
        Assert.Equal("active", item.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Owner_can_update_a_menu_item_without_changing_its_stock_state()
    {
        await using var postgres = await StartDatabaseAsync();
        var signingKey = TestJwtConfiguration.CreateSigningKey();
        using var factory = CreateFactory(postgres, signingKey);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
        var ownerId = NanoId.New().Value;
        var restaurantId = NanoId.New().Value;
        var branchId = NanoId.New().Value;
        await SeedOwnerBranchAsync(dbContext, ownerId, NanoId.New().Value, restaurantId, branchId);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = Bearer(ownerId, signingKey);
        using var categoryResponse = await client.PostAsJsonAsync("/api/v1/restaurant/menu/categories",
            new { branchId, name = "Noodles", displayOrder = 0 });
        using var categoryBody = await JsonDocument.ParseAsync(await categoryResponse.Content.ReadAsStreamAsync());
        var categoryId = categoryBody.RootElement.GetProperty("data").GetProperty("id").GetString();
        using var itemResponse = await client.PostAsJsonAsync("/api/v1/restaurant/menu/items",
            new { branchId, categoryId, name = "Beef soup", description = "Original", price = 12m });
        using var itemBody = await JsonDocument.ParseAsync(await itemResponse.Content.ReadAsStreamAsync());
        var itemId = itemBody.RootElement.GetProperty("data").GetProperty("id").GetString();

        using var deactivatedCategory = await client.PatchAsJsonAsync($"/api/v1/restaurant/menu/categories/{categoryId}",
            new { name = "Noodles", displayOrder = 0, status = "inactive" });
        Assert.Equal(HttpStatusCode.OK, deactivatedCategory.StatusCode);

        using var updated = await client.PatchAsJsonAsync($"/api/v1/restaurant/menu/items/{itemId}",
            new { categoryId, name = "Special beef soup", description = "Updated", price = 15.25m, status = "active" });

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        using var updatedBody = await JsonDocument.ParseAsync(await updated.Content.ReadAsStreamAsync());
        var item = updatedBody.RootElement.GetProperty("data");
        Assert.Equal("Special beef soup", item.GetProperty("name").GetString());
        Assert.Equal(15.25m, item.GetProperty("price").GetDecimal());
        Assert.Equal("active", item.GetProperty("status").GetString());
        Assert.Equal("available", item.GetProperty("stockStatus").GetString());
    }

    [Fact]
    public async Task Owner_can_mark_an_item_sold_out_and_available_without_changing_activation()
    {
        await using var postgres = await StartDatabaseAsync();
        var signingKey = TestJwtConfiguration.CreateSigningKey();
        using var factory = CreateFactory(postgres, signingKey);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
        var ownerId = NanoId.New().Value;
        var restaurantId = NanoId.New().Value;
        var branchId = NanoId.New().Value;
        await SeedOwnerBranchAsync(dbContext, ownerId, NanoId.New().Value, restaurantId, branchId);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = Bearer(ownerId, signingKey);
        using var categoryResponse = await client.PostAsJsonAsync("/api/v1/restaurant/menu/categories",
            new { branchId, name = "Noodles", displayOrder = 0 });
        using var categoryBody = await JsonDocument.ParseAsync(await categoryResponse.Content.ReadAsStreamAsync());
        var categoryId = categoryBody.RootElement.GetProperty("data").GetProperty("id").GetString();
        using var itemResponse = await client.PostAsJsonAsync("/api/v1/restaurant/menu/items",
            new { branchId, categoryId, name = "Beef soup", description = (string?)null, price = 12m });
        using var itemBody = await JsonDocument.ParseAsync(await itemResponse.Content.ReadAsStreamAsync());
        var itemId = itemBody.RootElement.GetProperty("data").GetProperty("id").GetString();

        using var soldOut = await client.PostAsync($"/api/v1/restaurant/menu/items/{itemId}/sold-out", null);
        Assert.Equal(HttpStatusCode.OK, soldOut.StatusCode);
        using var soldOutBody = await JsonDocument.ParseAsync(await soldOut.Content.ReadAsStreamAsync());
        Assert.Equal("out_of_stock", soldOutBody.RootElement.GetProperty("data").GetProperty("stockStatus").GetString());
        Assert.Equal("active", soldOutBody.RootElement.GetProperty("data").GetProperty("status").GetString());

        using var available = await client.PostAsync($"/api/v1/restaurant/menu/items/{itemId}/available", null);
        Assert.Equal(HttpStatusCode.OK, available.StatusCode);
        using var availableBody = await JsonDocument.ParseAsync(await available.Content.ReadAsStreamAsync());
        Assert.Equal("available", availableBody.RootElement.GetProperty("data").GetProperty("stockStatus").GetString());
        Assert.Equal("active", availableBody.RootElement.GetProperty("data").GetProperty("status").GetString());

        using var duplicateAvailable = await client.PostAsync($"/api/v1/restaurant/menu/items/{itemId}/available", null);
        Assert.Equal(HttpStatusCode.OK, duplicateAvailable.StatusCode);

        var auditCount = await dbContext.Database.SqlQuery<int>($"""
            SELECT count(*)::int AS "Value" FROM audit_logs
            WHERE resource_type = 'menu_item' AND resource_id = {itemId}
            """).SingleAsync();
        Assert.Equal(3, auditCount);
    }

    [Fact]
    public async Task Active_items_cannot_be_created_in_inactive_categories()
    {
        await using var postgres = await StartDatabaseAsync();
        var signingKey = TestJwtConfiguration.CreateSigningKey();
        using var factory = CreateFactory(postgres, signingKey);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
        var ownerId = NanoId.New().Value;
        var restaurantId = NanoId.New().Value;
        var branchId = NanoId.New().Value;
        await SeedOwnerBranchAsync(dbContext, ownerId, NanoId.New().Value, restaurantId, branchId);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = Bearer(ownerId, signingKey);
        using var categoryResponse = await client.PostAsJsonAsync("/api/v1/restaurant/menu/categories",
            new { branchId, name = "Noodles", displayOrder = 0 });
        using var categoryBody = await JsonDocument.ParseAsync(await categoryResponse.Content.ReadAsStreamAsync());
        var categoryId = categoryBody.RootElement.GetProperty("data").GetProperty("id").GetString();
        using var deactivated = await client.PatchAsJsonAsync($"/api/v1/restaurant/menu/categories/{categoryId}",
            new { name = "Noodles", displayOrder = 0, status = "inactive" });
        Assert.Equal(HttpStatusCode.OK, deactivated.StatusCode);
        using var itemResponse = await client.PostAsJsonAsync("/api/v1/restaurant/menu/items",
            new { branchId, categoryId, name = "Beef soup", description = (string?)null, price = 12m });
        Assert.Equal(HttpStatusCode.Conflict, itemResponse.StatusCode);
    }


    [Fact]
    public async Task Another_restaurant_owner_cannot_create_categories_in_the_branch()
    {
        await using var postgres = await StartDatabaseAsync();
        var signingKey = TestJwtConfiguration.CreateSigningKey();
        using var factory = CreateFactory(postgres, signingKey);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
        var ownerId = NanoId.New().Value;
        var otherOwnerId = NanoId.New().Value;
        var restaurantId = NanoId.New().Value;
        var branchId = NanoId.New().Value;
        await SeedOwnerBranchAsync(dbContext, ownerId, otherOwnerId, restaurantId, branchId);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = Bearer(otherOwnerId, signingKey);
        using var response = await client.PostAsJsonAsync("/api/v1/restaurant/menu/categories",
            new { branchId, name = "Unauthorized", displayOrder = 0 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var categoryCount = await dbContext.Database.SqlQuery<int>($"""
            SELECT count(*)::int AS "Value" FROM menu_categories WHERE branch_id = {branchId}
            """).SingleAsync();
        Assert.Equal(0, categoryCount);
    }

    [Fact]
    public async Task Active_restaurant_staff_can_manage_its_menu()
    {
        await using var postgres = await StartDatabaseAsync();
        var signingKey = TestJwtConfiguration.CreateSigningKey();
        using var factory = CreateFactory(postgres, signingKey);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
        var ownerId = NanoId.New().Value;
        var otherOwnerId = NanoId.New().Value;
        var staffId = NanoId.New().Value;
        var restaurantId = NanoId.New().Value;
        var branchId = NanoId.New().Value;
        await SeedOwnerBranchAsync(dbContext, ownerId, otherOwnerId, restaurantId, branchId);
        var now = DateTimeOffset.UtcNow;
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO users (id, email, password_hash, status, email_verified_at, created_at, updated_at)
            VALUES ({staffId}, 'menu-staff@example.test', 'hash', 'active', {now}, {now}, {now})
            """);
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO restaurant_staff (id, restaurant_id, user_id, role, status, created_at)
            VALUES ({NanoId.New().Value}, {restaurantId}, {staffId}, 'manager', 'active', {now})
            """);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = Bearer(staffId, signingKey);
        using var response = await client.PostAsJsonAsync("/api/v1/restaurant/menu/categories",
            new { branchId, name = "Staff-managed", displayOrder = 0 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Public_branch_menu_returns_only_published_categories_and_orderable_items()
    {
        await using var postgres = await StartDatabaseAsync();
        var signingKey = TestJwtConfiguration.CreateSigningKey();
        using var factory = CreateFactory(postgres, signingKey);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
        var ownerId = NanoId.New().Value;
        var restaurantId = NanoId.New().Value;
        var branchId = NanoId.New().Value;
        await SeedOwnerBranchAsync(dbContext, ownerId, NanoId.New().Value, restaurantId, branchId);
        var categoryId = NanoId.New().Value;
        var itemId = NanoId.New().Value;
        var now = DateTimeOffset.UtcNow;
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO menu_categories (id, branch_id, name, display_order, status, created_at, updated_at)
            VALUES ({categoryId}, {branchId}, 'Meals', 0, 'active', {now}, {now})
            """);
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO menu_items (id, branch_id, category_id, name, price, stock_status, status, created_at, updated_at)
            VALUES ({itemId}, {branchId}, {categoryId}, 'Rice bowl', 10.00, 'available', 'active', {now}, {now}),
                   ({NanoId.New().Value}, {branchId}, {categoryId}, 'Sold out', 11.00, 'out_of_stock', 'active', {now}, {now}),
                   ({NanoId.New().Value}, {branchId}, {categoryId}, 'Hidden', 12.00, 'available', 'inactive', {now}, {now})
            """);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var response = await client.GetAsync($"/api/v1/branches/{branchId}/menu");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var categories = body.RootElement.GetProperty("data").GetProperty("categories");
        Assert.Single(categories.EnumerateArray());
        var items = categories[0].GetProperty("items");
        Assert.Single(items.EnumerateArray());
        Assert.Equal(itemId, items[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task Public_discovery_lists_only_active_restaurants_and_open_branches()
    {
        await using var postgres = await StartDatabaseAsync();
        var signingKey = TestJwtConfiguration.CreateSigningKey();
        using var factory = CreateFactory(postgres, signingKey);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
        var ownerId = NanoId.New().Value;
        var restaurantId = NanoId.New().Value;
        var branchId = NanoId.New().Value;
        await SeedOwnerBranchAsync(dbContext, ownerId, NanoId.New().Value, restaurantId, branchId);
        var pendingRestaurantId = NanoId.New().Value;
        var pendingBranchId = NanoId.New().Value;
        var now = DateTimeOffset.UtcNow;
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO restaurants (id, owner_user_id, name, status, created_at, updated_at)
            VALUES ({pendingRestaurantId}, {ownerId}, 'Pending Place', 'pending', {now}, {now})
            """);
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO restaurant_branches (
                id, restaurant_id, name, phone, address_line, ward, district, city, latitude, longitude,
                status, created_at, updated_at)
            VALUES ({pendingBranchId}, {pendingRestaurantId}, 'Preview branch', '+84900000000', '42 Example Road',
                    'Central', 'District 1', 'HCMC', 10.0, 106.0, 'open', {now}, {now})
            """);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var response = await client.GetAsync("/api/v1/restaurants?city=HCMC&limit=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var items = body.RootElement.GetProperty("data");
        Assert.Single(items.EnumerateArray());
        Assert.Equal(restaurantId, items[0].GetProperty("id").GetString());
        Assert.False(body.RootElement.GetProperty("meta").GetProperty("hasMore").GetBoolean());

        using var wildcardCity = await client.GetAsync("/api/v1/restaurants?city=H%25");
        Assert.Equal(HttpStatusCode.OK, wildcardCity.StatusCode);
        using var wildcardBody = await JsonDocument.ParseAsync(await wildcardCity.Content.ReadAsStreamAsync());
        Assert.Empty(wildcardBody.RootElement.GetProperty("data").EnumerateArray());

        using var invalidPage = await client.GetAsync("/api/v1/restaurants?limit=0");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidPage.StatusCode);
        using var invalidCursor = await client.GetAsync("/api/v1/restaurants?cursor=not-a-cursor");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidCursor.StatusCode);
        using var restaurant = await client.GetAsync($"/api/v1/restaurants/{restaurantId}");
        Assert.Equal(HttpStatusCode.OK, restaurant.StatusCode);
        using var branches = await client.GetAsync($"/api/v1/restaurants/{restaurantId}/branches");
        Assert.Equal(HttpStatusCode.OK, branches.StatusCode);
        using var branch = await client.GetAsync($"/api/v1/branches/{branchId}");
        Assert.Equal(HttpStatusCode.OK, branch.StatusCode);
    }

    [Fact]
    public async Task Public_discovery_cursor_pages_do_not_skip_or_repeat_branches_with_equal_timestamps()
    {
        await using var postgres = await StartDatabaseAsync();
        var signingKey = TestJwtConfiguration.CreateSigningKey();
        using var factory = CreateFactory(postgres, signingKey);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
        var ownerId = NanoId.New().Value;
        var restaurantId = NanoId.New().Value;
        var firstBranchId = NanoId.New().Value;
        await SeedOwnerBranchAsync(dbContext, ownerId, NanoId.New().Value, restaurantId, firstBranchId);
        var secondBranchId = NanoId.New().Value;
        var createdAt = await dbContext.Database.SqlQuery<DateTimeOffset>($"""
            SELECT created_at AS "Value" FROM restaurant_branches WHERE id = {firstBranchId}
            """).SingleAsync();
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO restaurant_branches (
                id, restaurant_id, name, phone, address_line, ward, district, city, latitude, longitude,
                status, created_at, updated_at)
            VALUES ({secondBranchId}, {restaurantId}, 'Second Branch', '+84900000000', '42 Example Road',
                    'Central', 'District 1', 'HCMC', 10.0, 106.0, 'open', {createdAt}, {createdAt})
            """);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var firstPage = await client.GetAsync("/api/v1/restaurants?city=HCMC&limit=1");
        Assert.Equal(HttpStatusCode.OK, firstPage.StatusCode);
        using var firstBody = await JsonDocument.ParseAsync(await firstPage.Content.ReadAsStreamAsync());
        var firstData = firstBody.RootElement.GetProperty("data");
        Assert.Single(firstData.EnumerateArray());
        Assert.True(firstBody.RootElement.GetProperty("meta").GetProperty("hasMore").GetBoolean());
        var cursor = firstBody.RootElement.GetProperty("meta").GetProperty("nextCursor").GetString();

        using var secondPage = await client.GetAsync($"/api/v1/restaurants?city=HCMC&limit=1&cursor={Uri.EscapeDataString(cursor!)}");
        Assert.Equal(HttpStatusCode.OK, secondPage.StatusCode);
        using var secondBody = await JsonDocument.ParseAsync(await secondPage.Content.ReadAsStreamAsync());
        var secondData = secondBody.RootElement.GetProperty("data");
        Assert.Single(secondData.EnumerateArray());
        Assert.NotEqual(firstData[0].GetProperty("branchId").GetString(), secondData[0].GetProperty("branchId").GetString());
        Assert.False(secondBody.RootElement.GetProperty("meta").GetProperty("hasMore").GetBoolean());
    }

    private static async Task<PostgreSqlContainer> StartDatabaseAsync()
    {
        var postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("yumgo_menu_workflow_test")
            .WithUsername("postgres")
            .WithPassword(Guid.NewGuid().ToString("N"))
            .Build();
        await postgres.StartAsync();
        return postgres;
    }

    private static WebApplicationFactory<Program> CreateFactory(PostgreSqlContainer postgres, string signingKey) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
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

    private static async Task SeedOwnerBranchAsync(
        ApplicationDbContext context, string ownerId, string otherOwnerId, string restaurantId, string branchId)
    {
        var now = DateTimeOffset.UtcNow;
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO users (id, email, password_hash, status, email_verified_at, created_at, updated_at)
            VALUES ({ownerId}, 'menu-owner@example.test', 'hash', 'active', {now}, {now}, {now}),
                   ({otherOwnerId}, 'menu-other@example.test', 'hash', 'active', {now}, {now}, {now})
            """);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO restaurants (id, owner_user_id, name, description, status, created_at, updated_at)
            VALUES ({restaurantId}, {ownerId}, 'Pho House', NULL, 'active', {now}, {now})
            """);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO restaurant_branches (
                id, restaurant_id, name, phone, address_line, ward, district, city, latitude, longitude,
                status, opens_at, closes_at, created_at, updated_at)
            VALUES ({branchId}, {restaurantId}, 'Main Branch', '+84912345678', '12 Nguyen Hue', 'Ben Nghe',
                    'District 1', 'HCMC', 10.776900, 106.700900, 'open', NULL, NULL, {now}, {now})
            """);
    }

    private static AuthenticationHeaderValue Bearer(string userId, string signingKey)
    {
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: "YumGo.Tests",
            audience: "YumGo.Tests.Client",
            claims: [new Claim(JwtRegisteredClaimNames.Sub, userId), new Claim("sid", NanoId.New().Value)],
            notBefore: now.AddMinutes(-1),
            expires: now.AddMinutes(10),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Convert.FromBase64String(signingKey)), SecurityAlgorithms.HmacSha256));
        return new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
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
