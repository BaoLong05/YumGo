using YumGo.Domain.Common;
using YumGo.Domain.Menu;

namespace YumGo.Domain.Tests;

public sealed class MenuCategoryTests
{
    [Fact]
    public void Create_trims_name_and_starts_active()
    {
        var now = DateTimeOffset.UtcNow;

        var category = MenuCategory.Create(NanoId.New(), NanoId.New(), "  Noodles  ", 0, now);

        Assert.Equal("Noodles", category.Name);
        Assert.Equal(0, category.DisplayOrder);
        Assert.Equal("active", category.Status);
        Assert.Equal(now, category.CreatedAt);
    }

    [Fact]
    public void Create_rejects_negative_display_order()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MenuCategory.Create(NanoId.New(), NanoId.New(), "Noodles", -1, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Update_changes_name_order_and_status_without_changing_identity_or_creation_time()
    {
        var now = DateTimeOffset.UtcNow;
        var category = MenuCategory.Create(NanoId.New(), NanoId.New(), "Noodles", 0, now);

        var updated = category.Update("  Rice  ", 2, "inactive", now.AddMinutes(1));

        Assert.Equal(category.Id, updated.Id);
        Assert.Equal(category.BranchId, updated.BranchId);
        Assert.Equal("Rice", updated.Name);
        Assert.Equal(2, updated.DisplayOrder);
        Assert.Equal("inactive", updated.Status);
        Assert.Equal(now, updated.CreatedAt);
        Assert.Equal(now.AddMinutes(1), updated.UpdatedAt);
    }
}
