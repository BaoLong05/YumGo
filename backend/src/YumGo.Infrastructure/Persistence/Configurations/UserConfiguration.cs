using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using YumGo.Domain.Common;
using YumGo.Domain.Identity;

namespace YumGo.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", table =>
        {
            table.HasCheckConstraint(
                "ck_users_id_nanoid_21",
                "\"id\" ~ '^[A-Za-z0-9_-]{21}$'");
            table.HasCheckConstraint(
                "ck_users_status",
                "\"status\" IN ('active', 'suspended', 'disabled')");
            table.HasCheckConstraint(
                "ck_users_email_normalized",
                "\"email\" = lower(\"email\")");
        });
        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .HasColumnName("id")
            .HasColumnType("varchar(21)")
            .HasConversion(new ValueConverter<NanoId, string>(id => id.Value, value => NanoId.Create(value)))
            .ValueGeneratedNever();

        builder.Property(user => user.Email)
            .HasColumnName("email")
            .HasColumnType("varchar(254)")
            .HasConversion(new ValueConverter<Email, string>(email => email.Value, value => Email.Create(value)))
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasColumnName("password_hash")
            .HasColumnType("varchar(255)")
            .HasConversion(new ValueConverter<PasswordHash, string>(hash => hash.Value, value => PasswordHash.FromEncodedHash(value)))
            .IsRequired();

        builder.Property(user => user.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(20)")
            .HasConversion(
                status => status.ToString().ToLowerInvariant(),
                value => Enum.Parse<UserStatus>(value, ignoreCase: true))
            .IsRequired();

        builder.Property<DateTimeOffset?>("deleted_at")
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone");

        builder.Property<string?>("phone")
            .HasColumnName("phone")
            .HasColumnType("varchar(20)");

        builder.Property<DateTimeOffset?>("email_verified_at")
            .HasColumnName("email_verified_at")
            .HasColumnType("timestamp with time zone");

        builder.Property<DateTimeOffset>("created_at")
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property<DateTimeOffset>("updated_at")
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("ux_users_email");

        builder.HasIndex("phone")
            .IsUnique()
            .HasDatabaseName("ux_users_phone")
            .HasFilter("\"phone\" IS NOT NULL");
    }
}
