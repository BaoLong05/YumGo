using YumGo.Domain.Addresses;
using YumGo.Domain.Common;
using YumGo.Domain.Identity;

namespace YumGo.Domain.Tests;

public sealed class CustomerAddressTests
{
    [Fact]
    public void Create_trims_required_fields_and_preserves_geocoded_location()
    {
        var address = CustomerAddress.Create(
            NanoId.New(),
            NanoId.New(),
            "  Home  ",
            "  Long Tran  ",
            PhoneNumber.Create("+84912345678"),
            "12 Nguyen Hue",
            "Ben Nghe",
            "District 1",
            "Ho Chi Minh City",
            10.7769m,
            106.7009m,
            true,
            DateTimeOffset.UtcNow);

        Assert.Equal("Home", address.Label);
        Assert.Equal("Long Tran", address.RecipientName);
        Assert.Equal("12 Nguyen Hue", address.AddressLine);
        Assert.Equal(10.7769m, address.Latitude);
        Assert.Equal(106.7009m, address.Longitude);
        Assert.True(address.IsDefault);
    }

    [Theory]
    [InlineData(-90.0001, 0)]
    [InlineData(90.0001, 0)]
    [InlineData(0, -180.0001)]
    [InlineData(0, 180.0001)]
    public void Create_rejects_coordinates_outside_valid_ranges(double latitude, double longitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CustomerAddress.Create(
            NanoId.New(), NanoId.New(), "Home", "Long Tran", PhoneNumber.Create("+84912345678"),
            "12 Nguyen Hue", "Ben Nghe", "District 1", "Ho Chi Minh City",
            (decimal)latitude, (decimal)longitude, false, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_rejects_address_components_that_were_not_reverse_geocoded()
    {
        Assert.Throws<ArgumentException>(() => CustomerAddress.Create(
            NanoId.New(), NanoId.New(), "Home", "Long Tran", PhoneNumber.Create("+84912345678"),
            "", "Ben Nghe", "District 1", "Ho Chi Minh City",
            10.7769m, 106.7009m, false, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_rounds_coordinates_to_database_precision()
    {
        var address = CustomerAddress.Create(
            NanoId.New(), NanoId.New(), "Home", "Long Tran", PhoneNumber.Create("+84912345678"),
            "12 Nguyen Hue", "Ben Nghe", "District 1", "Ho Chi Minh City",
            10.1234567m, 106.7654321m, false, DateTimeOffset.UtcNow);

        Assert.Equal(10.123457m, address.Latitude);
        Assert.Equal(106.765432m, address.Longitude);
    }
}
