using YumGo.Domain.Common;

namespace YumGo.Domain.Menu;

public sealed class MenuItem
{
    private MenuItem(NanoId id, NanoId branchId, NanoId categoryId, string name, string? description,
        decimal price, string stockStatus, string status, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        Id = id;
        BranchId = branchId;
        CategoryId = categoryId;
        Name = name;
        Description = description;
        Price = price;
        StockStatus = stockStatus;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public NanoId Id { get; }
    public NanoId BranchId { get; }
    public NanoId CategoryId { get; }
    public string Name { get; }
    public string? Description { get; }
    public decimal Price { get; }
    public string StockStatus { get; }
    public string Status { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; }

    public static MenuItem Create(NanoId id, NanoId branchId, NanoId categoryId, string name,
        string? description, decimal price, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(branchId);
        ArgumentNullException.ThrowIfNull(categoryId);
        ValidatePrice(price);
        return new MenuItem(id, branchId, categoryId, ValidateName(name), NormalizeDescription(description),
            price, "available", "active", now, now);
    }

    public MenuItem Update(NanoId categoryId, string name, string? description, decimal price,
        string status, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(categoryId);
        ValidatePrice(price);
        if (status is not ("active" or "inactive")) throw new ArgumentException("Unknown menu item status.", nameof(status));
        return new MenuItem(Id, BranchId, categoryId, ValidateName(name), NormalizeDescription(description),
            price, StockStatus, status, CreatedAt, now);
    }

    public MenuItem SetStockStatus(string stockStatus, DateTimeOffset now)
    {
        if (stockStatus is not ("available" or "unavailable" or "out_of_stock"))
            throw new ArgumentException("Unknown menu item stock status.", nameof(stockStatus));
        return new MenuItem(Id, BranchId, CategoryId, Name, Description, Price, stockStatus, Status, CreatedAt, now);
    }

    public static MenuItem Restore(NanoId id, NanoId branchId, NanoId categoryId, string name,
        string? description, decimal price, string stockStatus, string status,
        DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(branchId);
        ArgumentNullException.ThrowIfNull(categoryId);
        ValidatePrice(price);
        if (stockStatus is not ("available" or "unavailable" or "out_of_stock"))
            throw new ArgumentException("Unknown menu item stock status.", nameof(stockStatus));
        if (status is not ("active" or "inactive"))
            throw new ArgumentException("Unknown menu item status.", nameof(status));
        return new MenuItem(id, branchId, categoryId, ValidateName(name), NormalizeDescription(description),
            price, stockStatus, status, createdAt, updatedAt);
    }

    private static string ValidateName(string? name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        var normalized = name.Trim();
        if (normalized.Length is < 2 or > 160)
            throw new ArgumentException("Menu item name must contain between 2 and 160 characters.", nameof(name));
        return normalized;
    }

    private static string? NormalizeDescription(string? description)
    {
        var normalized = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (normalized?.Length > 2000)
            throw new ArgumentException("Menu item description cannot exceed 2000 characters.", nameof(description));
        return normalized;
    }

    private static void ValidatePrice(decimal price)
    {
        if (price is < 0 or > 9_999_999_999.99m || decimal.Round(price, 2) != price)
            throw new ArgumentOutOfRangeException(nameof(price), "Price must fit numeric(12,2) and cannot be negative.");
    }
}
