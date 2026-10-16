using YumGo.Domain.Common;

namespace YumGo.Domain.Restaurants;

public sealed class Restaurant
{
    private Restaurant(NanoId id, NanoId ownerUserId, string name, string? description, string status,
        DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        Id = id;
        OwnerUserId = ownerUserId;
        Name = name;
        Description = description;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public NanoId Id { get; }
    public NanoId OwnerUserId { get; }
    public string Name { get; }
    public string? Description { get; }
    public string Status { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; }

    public static Restaurant Create(NanoId id, NanoId ownerUserId, string name, string? description, DateTimeOffset now)
    {
        return Restore(id, ownerUserId, name, description, "pending", now, now);
    }

    public Restaurant Review(bool approve, DateTimeOffset now)
    {
        if (Status != "pending")
            throw new InvalidOperationException("Only pending restaurants can be reviewed.");

        return new Restaurant(Id, OwnerUserId, Name, Description, approve ? "active" : "rejected", CreatedAt, now);
    }

    public static Restaurant Restore(NanoId id, NanoId ownerUserId, string name, string? description,
        string status, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(ownerUserId);
        var normalizedName = ValidateText(name, nameof(name), 2, 160);
        var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (normalizedDescription?.Length > 2000)
            throw new ArgumentException("Description cannot exceed 2000 characters.", nameof(description));
        if (status is not ("pending" or "active" or "rejected" or "suspended" or "closed"))
            throw new ArgumentException("Unknown restaurant status.", nameof(status));

        return new Restaurant(id, ownerUserId, normalizedName, normalizedDescription, status, createdAt, updatedAt);
    }

    private static string ValidateText(string? value, string parameterName, int min, int max)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length < min || normalized.Length > max)
            throw new ArgumentException($"The value must contain between {min} and {max} characters.", parameterName);
        return normalized;
    }
}
