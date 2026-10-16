using YumGo.Domain.Common;

namespace YumGo.Domain.Menu;

public sealed class MenuCategory
{
    private MenuCategory(NanoId id, NanoId branchId, string name, int displayOrder,
        string status, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        Id = id;
        BranchId = branchId;
        Name = name;
        DisplayOrder = displayOrder;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public NanoId Id { get; }
    public NanoId BranchId { get; }
    public string Name { get; }
    public int DisplayOrder { get; }
    public string Status { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; }

    public static MenuCategory Create(NanoId id, NanoId branchId, string name, int displayOrder, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(branchId);
        if (displayOrder < 0) throw new ArgumentOutOfRangeException(nameof(displayOrder));
        var normalizedName = ValidateName(name);
        return new MenuCategory(id, branchId, normalizedName, displayOrder, "active", now, now);
    }

    public MenuCategory Update(string name, int displayOrder, string status, DateTimeOffset now)
    {
        if (displayOrder < 0) throw new ArgumentOutOfRangeException(nameof(displayOrder));
        if (status is not ("active" or "inactive")) throw new ArgumentException("Unknown menu category status.", nameof(status));
        return new MenuCategory(Id, BranchId, ValidateName(name), displayOrder, status, CreatedAt, now);
    }


    public static MenuCategory Restore(NanoId id, NanoId branchId, string name, int displayOrder,
        string status, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(branchId);
        if (displayOrder < 0) throw new ArgumentOutOfRangeException(nameof(displayOrder));
        if (status is not ("active" or "inactive")) throw new ArgumentException("Unknown menu category status.", nameof(status));
        return new MenuCategory(id, branchId, ValidateName(name), displayOrder, status, createdAt, updatedAt);
    }

    private static string ValidateName(string? name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        var normalized = name.Trim();
        if (normalized.Length > 120) throw new ArgumentException("Category name cannot exceed 120 characters.", nameof(name));
        return normalized;
    }
}
