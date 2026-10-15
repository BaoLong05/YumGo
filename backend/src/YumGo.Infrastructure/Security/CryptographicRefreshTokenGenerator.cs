using System.Security.Cryptography;
using System.Text;
using YumGo.Application.Identity;

namespace YumGo.Infrastructure.Security;

public sealed class CryptographicRefreshTokenGenerator : IRefreshTokenGenerator
{
    public GeneratedRefreshToken Generate()
    {
        var value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return new GeneratedRefreshToken(value, Hash(value));
    }

    public string Hash(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
