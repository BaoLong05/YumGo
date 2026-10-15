namespace YumGo.Application.Identity;

public sealed class LogoutSessionHandler(
    IAuthenticationStore authenticationStore,
    IRefreshTokenGenerator refreshTokenGenerator,
    TimeProvider timeProvider)
{
    public async Task HandleAsync(
        string userId,
        string sessionId,
        string? refreshTokenValue,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenValue))
        {
            return;
        }

        var tokenHash = refreshTokenGenerator.Hash(refreshTokenValue);
        var token = await authenticationStore.FindRefreshTokenByHashAsync(tokenHash, cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (token is null
            || token.Id != sessionId
            || token.UserId != userId
            || token.RevokedAt.HasValue
            || token.ExpiresAt <= now
            || !token.IsAccountActive)
        {
            return;
        }

        await authenticationStore.RevokeRefreshTokenDescendantsAsync(
            token.Id,
            now,
            cancellationToken);
        await authenticationStore.RecordAuthenticationAuditAsync(
            userId,
            "auth.logout.succeeded",
            ipAddress,
            userAgent,
            now,
            cancellationToken);
    }
}
