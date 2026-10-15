using Microsoft.EntityFrameworkCore;
using YumGo.Application.Identity;
using YumGo.Domain.Common;
using YumGo.Domain.Identity;
using YumGo.Infrastructure.Persistence;

namespace YumGo.Infrastructure.Identity;

public sealed class EfAuthenticationStore(ApplicationDbContext dbContext) : IAuthenticationStore
{
    public async Task<AuthenticationAccount?> FindByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Set<User>()
            .FromSqlInterpolated($"SELECT * FROM users WHERE email = {normalizedEmail} AND deleted_at IS NULL")
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        var roles = await dbContext.Database.SqlQuery<string>($"""
            SELECT roles.code AS "Value"
            FROM user_roles
            INNER JOIN roles ON roles.id = user_roles.role_id
            WHERE user_roles.user_id = {user.Id.Value}
            ORDER BY roles.code
            """)
            .ToArrayAsync(cancellationToken);

        return new AuthenticationAccount(user.Id.Value, user.PasswordHash, user.Status, roles);
    }

    public async Task CreateRefreshTokenAsync(
        string id,
        string userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO refresh_tokens (id, user_id, token_hash, expires_at, created_at)
            VALUES ({id}, {userId}, {tokenHash}, {expiresAt}, {createdAt})
            """, cancellationToken);
        await InsertAuthenticationAuditAsync(
            userId,
            "auth.login.succeeded",
            ipAddress,
            userAgent,
            createdAt,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<RefreshTokenSession?> FindRefreshTokenByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        var token = await dbContext.Database.SqlQuery<RefreshTokenRow>($"""
            SELECT id AS "Id",
                   user_id AS "UserId",
                   expires_at AS "ExpiresAt",
                   revoked_at AS "RevokedAt",
                   replaced_by_id AS "ReplacedById"
            FROM refresh_tokens
            WHERE token_hash = {tokenHash}
            """)
            .SingleOrDefaultAsync(cancellationToken);

        if (token is null)
        {
            return null;
        }

        var user = await dbContext.Set<User>()
            .FromSqlInterpolated($"SELECT * FROM users WHERE id = {token.UserId} AND deleted_at IS NULL")
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return new RefreshTokenSession(
                token.Id,
                token.UserId,
                token.ExpiresAt,
                token.RevokedAt,
                token.ReplacedById,
                false,
                Array.Empty<string>());
        }

        var roles = await GetRolesAsync(user.Id.Value, cancellationToken);
        return new RefreshTokenSession(
            token.Id,
            token.UserId,
            token.ExpiresAt,
            token.RevokedAt,
            token.ReplacedById,
            user.Status == UserStatus.Active,
            roles);
    }

    public async Task<bool> RotateRefreshTokenAsync(
        string oldTokenId,
        string oldTokenHash,
        string newTokenId,
        string userId,
        string newTokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset now,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO refresh_tokens (id, user_id, token_hash, expires_at, created_at)
            VALUES ({newTokenId}, {userId}, {newTokenHash}, {expiresAt}, {now})
            """, cancellationToken);

        var revokedCount = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE refresh_tokens
            SET revoked_at = {now}, replaced_by_id = {newTokenId}
            WHERE id = {oldTokenId}
              AND token_hash = {oldTokenHash}
              AND user_id = {userId}
              AND revoked_at IS NULL
              AND expires_at > {now}
              AND EXISTS (
                  SELECT 1 FROM users
                  WHERE users.id = {userId}
                    AND users.status = 'active'
                    AND users.deleted_at IS NULL)
            """, cancellationToken);

        if (revokedCount != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await InsertAuthenticationAuditAsync(
            userId,
            "auth.refresh.rotated",
            ipAddress,
            userAgent,
            now,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task RevokeRefreshTokenDescendantsAsync(
        string tokenId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            WITH RECURSIVE descendants(id, replaced_by_id) AS (
                SELECT id, replaced_by_id
                FROM refresh_tokens
                WHERE id = {tokenId}
                UNION
                SELECT child.id, child.replaced_by_id
                FROM refresh_tokens child
                INNER JOIN descendants parent ON child.id = parent.replaced_by_id
            )
            UPDATE refresh_tokens
            SET revoked_at = {now}
            WHERE id IN (SELECT id FROM descendants)
              AND revoked_at IS NULL
            """, cancellationToken);
    }

    public async Task<bool> IsSessionActiveAsync(
        string userId,
        string sessionId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return await dbContext.Database.SqlQuery<bool>($"""
            SELECT EXISTS (
                SELECT 1
                FROM refresh_tokens
                INNER JOIN users ON users.id = refresh_tokens.user_id
                WHERE refresh_tokens.id = {sessionId}
                  AND refresh_tokens.user_id = {userId}
                  AND refresh_tokens.revoked_at IS NULL
                  AND refresh_tokens.expires_at > {now}
                  AND users.status = 'active'
                  AND users.deleted_at IS NULL
            ) AS "Value"
            """)
            .SingleAsync(cancellationToken);
    }

    public Task RecordAuthenticationAuditAsync(
        string? actorUserId,
        string action,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        return InsertAuthenticationAuditAsync(
            actorUserId,
            action,
            ipAddress,
            userAgent,
            createdAt,
            cancellationToken);
    }

    private async Task InsertAuthenticationAuditAsync(
        string? actorUserId,
        string action,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        var resourceType = actorUserId is null ? "authentication" : "user";
        var resourceId = actorUserId;
        var safeIpAddress = ipAddress is { Length: > 64 } ? ipAddress[..64] : ipAddress;
        var safeUserAgent = userAgent is { Length: > 1000 } ? userAgent[..1000] : userAgent;

        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO audit_logs (
                id, actor_user_id, action, resource_type, resource_id,
                before_json, after_json, ip_address, user_agent, created_at)
            VALUES (
                {NanoId.New().Value}, {actorUserId}, {action}, {resourceType}, {resourceId},
                NULL, NULL, {safeIpAddress}, {safeUserAgent}, {createdAt})
            """, cancellationToken);
    }

    private async Task<string[]> GetRolesAsync(string userId, CancellationToken cancellationToken)
    {
        return await dbContext.Database.SqlQuery<string>($"""
            SELECT roles.code AS "Value"
            FROM user_roles
            INNER JOIN roles ON roles.id = user_roles.role_id
            WHERE user_roles.user_id = {userId}
            ORDER BY roles.code
            """)
            .ToArrayAsync(cancellationToken);
    }

    private sealed class RefreshTokenRow
    {
        public string Id { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public DateTimeOffset ExpiresAt { get; set; }

        public DateTimeOffset? RevokedAt { get; set; }

        public string? ReplacedById { get; set; }
    }
}
