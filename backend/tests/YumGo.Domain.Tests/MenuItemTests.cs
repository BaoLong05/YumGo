using YumGo.Domain.Common;
using YumGo.Domain.Menu;

namespace YumGo.Domain.Tests;

public sealed class MenuItemTests
{
    [Fact]
    public void Create_starts_active_and_available_without_rounding_price()
    {
        var now = DateTimeOffset.UtcNow;

        var item = MenuItem.Create(NanoId.New(), NanoId.New(), NanoId.New(), "  Beef noodle  ", "  Spicy  ", 12.50m, now);

        Assert.Equal("Beef noodle", item.Name);
        Assert.Equal("Spicy", item.Description);
        Assert.Equal(12.50m, item.Price);
        Assert.Equal("available", item.StockStatus);
        Assert.Equal("active", item.Status);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(0.001)]
    [InlineData(10000000000.00)]
    public void Create_rejects_price_outside_numeric_12_2(decimal price)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MenuItem.Create(NanoId.New(), NanoId.New(), NanoId.New(), "Beef noodle", null, price, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Stock_availability_is_independent_from_item_activation()
    {
        var item = MenuItem.Create(NanoId.New(), NanoId.New(), NanoId.New(), "Beef noodle", null, 12m, DateTimeOffset.UtcNow);

        var soldOut = item.SetStockStatus("out_of_stock", DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.Equal("active", soldOut.Status);
        Assert.Equal("out_of_stock", soldOut.StockStatus);
    }
}
