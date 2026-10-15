using YumGo.Domain.Common;
using YumGo.Domain.Identity;

namespace YumGo.Application.Identity;

public sealed class LoginHandler(
    IAuthenticationStore authenticationStore,
    IPasswordHashingService passwordHashingService,
    IRefreshTokenGenerator refreshTokenGenerator,
    IAccessTokenIssuer accessTokenIssuer,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(48);

    public async Task<LoginResult> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.Password))
        {
            await RecordFailedLoginAsync(command, now, cancellationToken);
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        string normalizedEmail;
        try
        {
            normalizedEmail = Email.Create(command.Email).Value;
        }
        catch (ArgumentException)
        {
            await RecordFailedLoginAsync(command, now, cancellationToken);
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        var account = await authenticationStore.FindByEmailAsync(normalizedEmail, cancellationToken);
        if (account is null
            || account.Status != UserStatus.Active
            || !passwordHashingService.Verify(command.Password, account.PasswordHash))
        {
            await RecordFailedLoginAsync(command, now, cancellationToken);
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        var sessionExpiresAt = now.Add(SessionLifetime);
        var sessionId = NanoId.New().Value;
        var refreshToken = refreshTokenGenerator.Generate();
        var accessToken = accessTokenIssuer.Issue(account.UserId, sessionId, account.Roles, now, sessionExpiresAt);

        await authenticationStore.CreateRefreshTokenAsync(
            sessionId,
            account.UserId,
            refreshToken.Hash,
            sessionExpiresAt,
            now,
            command.IpAddress,
            command.UserAgent,
            cancellationToken);

        return new LoginResult(
            LoginStatus.Success,
            new LoginSession(
                accessToken.Value,
                accessToken.ExpiresAt,
                refreshToken.Value,
                sessionExpiresAt));
    }

    private Task RecordFailedLoginAsync(
        LoginCommand command,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        return authenticationStore.RecordAuthenticationAuditAsync(
            null,
            "auth.login.failed",
            command.IpAddress,
            command.UserAgent,
            occurredAt,
            cancellationToken);
    }
}
