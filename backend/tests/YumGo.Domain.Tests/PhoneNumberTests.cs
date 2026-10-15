using YumGo.Domain.Identity;

namespace YumGo.Domain.Tests;

public sealed class PhoneNumberTests
{
    [Fact]
    public void Create_normalizes_an_e164_like_phone_number()
    {
        var phone = PhoneNumber.Create(" +84 (912) 345-678 ");

        Assert.Equal("+84912345678", phone.Value);
    }

    [Theory]
    [InlineData("0912345678")]
    [InlineData("+0123456789")]
    [InlineData("+1234567")]
    [InlineData("+84abc12345678")]
    [InlineData("+1234567890123456")]
    public void Create_rejects_invalid_phone_number(string value)
    {
        Assert.Throws<ArgumentException>(() => PhoneNumber.Create(value));
    }
}
