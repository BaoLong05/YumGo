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
}
