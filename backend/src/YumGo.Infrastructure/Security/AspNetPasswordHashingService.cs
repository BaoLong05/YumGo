using Microsoft.AspNetCore.Identity;
using YumGo.Application.Identity;
using YumGo.Domain.Identity;

namespace YumGo.Infrastructure.Security;

public sealed class AspNetPasswordHashingService : IPasswordHashingService
{
    private static readonly object PasswordHasherUser = new();
    private readonly PasswordHasher<object> _passwordHasher = new();

    public PasswordHash Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return PasswordHash.FromEncodedHash(_passwordHasher.HashPassword(PasswordHasherUser, password));
    }

    public bool Verify(string password, PasswordHash passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentNullException.ThrowIfNull(passwordHash);

        var result = _passwordHasher.VerifyHashedPassword(PasswordHasherUser, passwordHash.Value, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
