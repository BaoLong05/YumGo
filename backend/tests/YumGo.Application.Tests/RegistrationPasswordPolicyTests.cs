using YumGo.Application.Identity;

namespace YumGo.Application.Tests;

public sealed class RegistrationPasswordPolicyTests
{
    [Theory]
    [InlineData("Abc123", true)]
    [InlineData("Abc!xy", true)]
    [InlineData("abc123", false)]
    [InlineData("ABC123", false)]
    [InlineData("abcde!", false)]
    [InlineData("Ab1!x", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Validate_applies_six_characters_and_three_of_four_character_classes(string? password, bool expected)
    {
        var policy = new RegistrationPasswordPolicy();

        Assert.Equal(expected, policy.IsValid(password));
    }
}
