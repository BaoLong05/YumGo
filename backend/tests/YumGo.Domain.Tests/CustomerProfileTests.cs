using YumGo.Domain.Common;
using YumGo.Domain.Identity;

namespace YumGo.Domain.Tests;

public sealed class CustomerProfileTests
{
    [Fact]
    public void Register_trims_full_name_and_creates_profile_ids()
    {
        var userId = NanoId.New();

        var profile = CustomerProfile.Register(userId, "  Long Tran  ");

        Assert.Matches("^[A-Za-z0-9_-]{21}$", profile.Id.Value);
        Assert.Equal(userId, profile.UserId);
        Assert.Equal("Long Tran", profile.FullName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")]
    public void Register_rejects_full_names_shorter_than_two_characters(string fullName)
    {
        Assert.Throws<ArgumentException>(() => CustomerProfile.Register(NanoId.New(), fullName));
    }

    [Fact]
    public void Register_rejects_full_names_longer_than_120_characters()
    {
        var fullName = new string('A', 121);

        Assert.Throws<ArgumentException>(() => CustomerProfile.Register(NanoId.New(), fullName));
    }
}
