using YumGo.Domain.Common;

namespace YumGo.Domain.Tests;

public sealed class NanoIdTests
{
    [Fact]
    public void New_generates_a_url_safe_identifier_of_21_characters()
    {
        var id = NanoId.New();

        Assert.Equal(21, id.Value.Length);
        Assert.Matches("^[A-Za-z0-9_-]{21}$", id.Value);
    }

    [Fact]
    public void Create_preserves_a_valid_identifier()
    {
        const string value = "V1StGXR8_Z5jdHi6B-myT";

        var id = NanoId.Create(value);

        Assert.Equal(value, id.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("V1StGXR8_Z5jdHi6B-my!")]
    public void Create_rejects_values_that_are_not_nanoid_21(string value)
    {
        Assert.Throws<ArgumentException>(() => NanoId.Create(value));
    }
}
