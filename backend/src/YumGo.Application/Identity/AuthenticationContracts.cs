using YumGo.Domain.Identity;

namespace YumGo.Application.Identity;

public interface IAuthenticationStore
{
    Task<AuthenticationAccount?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task CreateRefreshTokenAsync(
        string id,
        string userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<RefreshTokenSession?> FindRefreshTokenByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken);

    Task<bool> RotateRefreshTokenAsync(
        string oldTokenId,
        string oldTokenHash,
        string newTokenId,
        string userId,
        string newTokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset now,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task RevokeRefreshTokenDescendantsAsync(
        string tokenId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<bool> IsSessionActiveAsync(
        string userId,
        string sessionId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task RecordAuthenticationAuditAsync(
        string? actorUserId,
        string action,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken);
}

public interface IRefreshTokenGenerator
{
    GeneratedRefreshToken Generate();

    string Hash(string value);
}

public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(
        string userId,
        string sessionId,
        IReadOnlyCollection<string> roles,
        DateTimeOffset issuedAt,
        DateTimeOffset sessionExpiresAt);
}

public sealed record AuthenticationAccount(
    string UserId,
    PasswordHash PasswordHash,
    UserStatus Status,
    IReadOnlyCollection<string> Roles);

public sealed record GeneratedRefreshToken(string Value, string Hash);

public sealed record RefreshTokenSession(
    string Id,
    string UserId,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt,
    string? ReplacedById,
    bool IsAccountActive,
    IReadOnlyCollection<string> Roles);

public sealed record IssuedAccessToken(string Value, DateTimeOffset ExpiresAt);

public sealed record LoginCommand(string? Email, string? Password, string? IpAddress, string? UserAgent);

public enum LoginStatus
{
    Success,
    InvalidCredentials
}

public sealed record LoginSession(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

public sealed record LoginResult(LoginStatus Status, LoginSession? Session = null);
