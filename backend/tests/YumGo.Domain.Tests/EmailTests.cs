using YumGo.Domain.Identity;

namespace YumGo.Domain.Tests;

public sealed class EmailTests
{
    [Fact]
    public void Create_normalizes_email_for_case_insensitive_lookup()
    {
        var email = Email.Create("  Long.Tran@Example.COM  ");

        Assert.Equal("long.tran@example.com", email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    public void Create_rejects_invalid_email(string value)
    {
        Assert.Throws<ArgumentException>(() => Email.Create(value));
    }

    [Fact]
    public void Create_rejects_email_longer_than_254_characters()
    {
        var value = $"{new string('a', 243)}@example.com";

        Assert.Equal(255, value.Length);
        Assert.Throws<ArgumentException>(() => Email.Create(value));
    }
}
