using Microsoft.IdentityModel.Tokens;

namespace YumGo.Api.Authentication;

public sealed class JwtTokenSettings
{
    public const string SectionName = "Authentication:Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    public SymmetricSecurityKey CreateSigningKey()
    {
        if (string.IsNullOrWhiteSpace(SigningKey))
        {
            throw new InvalidOperationException($"Configuration '{SectionName}:SigningKey' is required.");
        }

        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(SigningKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException($"Configuration '{SectionName}:SigningKey' must be base64.", exception);
        }

        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException($"Configuration '{SectionName}:SigningKey' must contain at least 32 bytes.");
        }

        return new SymmetricSecurityKey(keyBytes);
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException($"Configuration '{SectionName}:Issuer' and '{SectionName}:Audience' are required.");
        }

        if (AccessTokenLifetimeMinutes is < 1 or > 60)
        {
            throw new InvalidOperationException($"Configuration '{SectionName}:AccessTokenLifetimeMinutes' must be between 1 and 60.");
        }

        _ = CreateSigningKey();
    }
}
