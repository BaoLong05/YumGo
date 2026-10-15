using YumGo.Domain.Common;

namespace YumGo.Application.Identity;

public sealed class RefreshSessionHandler(
    IAuthenticationStore authenticationStore,
    IRefreshTokenGenerator refreshTokenGenerator,
    IAccessTokenIssuer accessTokenIssuer,
    TimeProvider timeProvider)
{
    public async Task<LoginResult> HandleAsync(
        string? refreshTokenValue,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenValue))
        {
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        var tokenHash = refreshTokenGenerator.Hash(refreshTokenValue);
        var storedToken = await authenticationStore.FindRefreshTokenByHashAsync(tokenHash, cancellationToken);
        if (storedToken is null)
        {
            await RecordAuditAsync(null, "auth.refresh.failed", ipAddress, userAgent, timeProvider.GetUtcNow(), cancellationToken);
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        var now = timeProvider.GetUtcNow();
        if (storedToken.RevokedAt.HasValue)
        {
            await authenticationStore.RevokeRefreshTokenDescendantsAsync(storedToken.Id, now, cancellationToken);
            await RecordAuditAsync(storedToken.UserId, "auth.refresh.reuse_detected", ipAddress, userAgent, now, cancellationToken);
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        if (storedToken.ExpiresAt <= now)
        {
            await RecordAuditAsync(storedToken.UserId, "auth.refresh.failed", ipAddress, userAgent, now, cancellationToken);
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        if (!storedToken.IsAccountActive)
        {
            await authenticationStore.RevokeRefreshTokenDescendantsAsync(storedToken.Id, now, cancellationToken);
            await RecordAuditAsync(storedToken.UserId, "auth.refresh.rejected", ipAddress, userAgent, now, cancellationToken);
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        var replacement = refreshTokenGenerator.Generate();
        var replacementId = NanoId.New().Value;
        var accessToken = accessTokenIssuer.Issue(
            storedToken.UserId,
            replacementId,
            storedToken.Roles,
            now,
            storedToken.ExpiresAt);
        var rotated = await authenticationStore.RotateRefreshTokenAsync(
            storedToken.Id,
            tokenHash,
            replacementId,
            storedToken.UserId,
            replacement.Hash,
            storedToken.ExpiresAt,
            now,
            ipAddress,
            userAgent,
            cancellationToken);

        if (!rotated)
        {
            await authenticationStore.RevokeRefreshTokenDescendantsAsync(storedToken.Id, now, cancellationToken);
            await RecordAuditAsync(storedToken.UserId, "auth.refresh.reuse_detected", ipAddress, userAgent, now, cancellationToken);
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        return new LoginResult(
            LoginStatus.Success,
            new LoginSession(
                accessToken.Value,
                accessToken.ExpiresAt,
                replacement.Value,
                storedToken.ExpiresAt));
    }

    private Task RecordAuditAsync(
        string? actorUserId,
        string action,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        return authenticationStore.RecordAuthenticationAuditAsync(
            actorUserId,
            action,
            ipAddress,
            userAgent,
            occurredAt,
            cancellationToken);
    }
}
