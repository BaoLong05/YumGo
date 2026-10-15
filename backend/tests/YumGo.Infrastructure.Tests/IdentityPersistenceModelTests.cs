using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using YumGo.Domain.Identity;
using YumGo.Infrastructure.Persistence;

namespace YumGo.Infrastructure.Tests;

public sealed class IdentityPersistenceModelTests
{
    [Fact]
    public void User_is_mapped_to_the_identity_table_with_a_nanoid_primary_key()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(User));

        Assert.NotNull(entity);
        Assert.Equal("users", entity.GetTableName());

        var id = entity.FindProperty(nameof(User.Id));
        Assert.NotNull(id);
        Assert.Equal("varchar(21)", id.GetColumnType());
        Assert.Equal("id", id.GetColumnName());
    }

    [Fact]
    public void Normalized_email_has_a_unique_index_across_all_users()
    {
        using var context = CreateContext();
        var entity = Assert.IsAssignableFrom<Microsoft.EntityFrameworkCore.Metadata.IEntityType>(
            context.Model.FindEntityType(typeof(User)));
        var index = Assert.Single(entity.GetIndexes(), candidate =>
            candidate.Properties.Any(property => property.Name == nameof(User.Email)));

        Assert.True(index.IsUnique);
        Assert.Null(index.GetFilter());
    }

    [Fact]
    public void User_has_UTC_creation_and_update_columns()
    {
        using var context = CreateContext();
        var entity = Assert.IsAssignableFrom<Microsoft.EntityFrameworkCore.Metadata.IEntityType>(
            context.Model.FindEntityType(typeof(User)));

        Assert.Equal("timestamp with time zone", entity.FindProperty("created_at")?.GetColumnType());
        Assert.Equal("timestamp with time zone", entity.FindProperty("updated_at")?.GetColumnType());
    }

    [Fact]
    public void User_status_is_constrained_to_documented_values()
    {
        using var context = CreateContext();
        var entity = Assert.IsAssignableFrom<Microsoft.EntityFrameworkCore.Metadata.IEntityType>(
            context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(User)));
        var constraint = Assert.Single(entity.GetCheckConstraints(), candidate =>
            candidate.Name == "ck_users_status");

        Assert.Equal("\"status\" IN ('active', 'suspended', 'disabled')", constraint.Sql);
    }

    [Fact]
    public void User_primary_key_is_constrained_to_nanoid_21_format()
    {
        using var context = CreateContext();
        var entity = Assert.IsAssignableFrom<Microsoft.EntityFrameworkCore.Metadata.IEntityType>(
            context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(User)));
        var constraint = Assert.Single(entity.GetCheckConstraints(), candidate =>
            candidate.Name == "ck_users_id_nanoid_21");

        Assert.Equal("\"id\" ~ '^[A-Za-z0-9_-]{21}$'", constraint.Sql);
    }

    [Fact]
    public void User_email_is_constrained_to_lowercase()
    {
        using var context = CreateContext();
        var entity = Assert.IsAssignableFrom<Microsoft.EntityFrameworkCore.Metadata.IEntityType>(
            context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(User)));
        var constraint = Assert.Single(entity.GetCheckConstraints(), candidate =>
            candidate.Name == "ck_users_email_normalized");

        Assert.Equal("\"email\" = lower(\"email\")", constraint.Sql);
    }

    [Fact]
    public void User_phone_has_a_unique_partial_index_for_non_null_values()
    {
        using var context = CreateContext();
        var entity = Assert.IsAssignableFrom<Microsoft.EntityFrameworkCore.Metadata.IEntityType>(
            context.Model.FindEntityType(typeof(User)));
        var phone = entity.FindProperty("phone");
        Assert.NotNull(phone);
        Assert.True(phone.IsNullable);
        Assert.Equal("varchar(20)", phone.GetColumnType());

        var index = Assert.Single(entity.GetIndexes(), candidate =>
            candidate.Properties.Any(property => property.Name == "phone"));
        Assert.True(index.IsUnique);
        Assert.Equal(
            "\"phone\" IS NOT NULL",
            index.GetFilter());
    }

    [Fact]
    public void User_has_optional_email_verification_timestamp()
    {
        using var context = CreateContext();
        var entity = Assert.IsAssignableFrom<Microsoft.EntityFrameworkCore.Metadata.IEntityType>(
            context.Model.FindEntityType(typeof(User)));
        var property = entity.FindProperty("email_verified_at");

        Assert.NotNull(property);
        Assert.True(property.IsNullable);
        Assert.Equal("timestamp with time zone", property.GetColumnType());
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=yumgo_test;Username=yumgo;Password=unused")
            .Options;

        return new ApplicationDbContext(options);
    }
}
