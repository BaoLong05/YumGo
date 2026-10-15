namespace YumGo.Application.Identity;

public sealed class RegistrationPasswordPolicy
{
    private const int MinimumLength = 6;
    private const int RequiredCharacterClassCount = 3;

    public bool IsValid(string? password)
    {
        if (password is null || password.Length < MinimumLength)
        {
            return false;
        }

        var characterClassCount = new[]
        {
            password.Any(char.IsUpper),
            password.Any(char.IsLower),
            password.Any(char.IsDigit),
            password.Any(character => !char.IsLetterOrDigit(character))
        }.Count(isPresent => isPresent);

        return characterClassCount >= RequiredCharacterClassCount;
    }
}
