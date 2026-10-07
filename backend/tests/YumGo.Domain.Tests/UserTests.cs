using YumGo.Domain.Identity;

namespace YumGo.Domain.Tests;

public sealed class UserTests
{
    [Fact]
    public void Register_creates_an_active_user_with_normalized_email_and_password_hash()
    {
        var email = Email.Create("Long.Tran@Example.COM");
        var passwordHash = PasswordHash.FromEncodedHash("encoded-password-hash");

        var user = User.Register(email, passwordHash);

        Assert.Matches("^[A-Za-z0-9_-]{21}$", user.Id.Value);
        Assert.Equal("long.tran@example.com", user.Email.Value);
        Assert.Equal(passwordHash, user.PasswordHash);
        Assert.Equal(UserStatus.Active, user.Status);
    }

    [Fact]
    public void Register_creates_a_distinct_id_for_each_user()
    {
        var email = Email.Create("long@example.com");
        var passwordHash = PasswordHash.FromEncodedHash("encoded-password-hash");

        var first = User.Register(email, passwordHash);
        var second = User.Register(email, passwordHash);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Suspend_marks_the_user_as_suspended()
    {
        var user = CreateUser();

        user.Suspend();

        Assert.Equal(UserStatus.Suspended, user.Status);
    }

    [Fact]
    public void Reactivate_restores_a_suspended_user()
    {
        var user = CreateUser();
        user.Suspend();

        user.Reactivate();

        Assert.Equal(UserStatus.Active, user.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FromEncodedHash_rejects_blank_values(string value)
    {
        Assert.Throws<ArgumentException>(() => PasswordHash.FromEncodedHash(value));
    }

    [Fact]
    public void ToString_does_not_expose_the_encoded_hash()
    {
        var passwordHash = PasswordHash.FromEncodedHash("secret-encoded-hash");

        Assert.DoesNotContain(passwordHash.Value, passwordHash.ToString());
    }

    private static User CreateUser() => User.Register(
        Email.Create("long@example.com"),
        PasswordHash.FromEncodedHash("encoded-password-hash"));
}
