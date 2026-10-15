using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using YumGo.Application.Identity;
using YumGo.Domain.Common;

namespace YumGo.Api.Authentication;

public sealed class JwtAccessTokenIssuer(IOptions<JwtTokenSettings> options) : IAccessTokenIssuer
{
    private readonly JwtTokenSettings _settings = options.Value;

    public IssuedAccessToken Issue(
        string userId,
        string sessionId,
        IReadOnlyCollection<string> roles,
        DateTimeOffset issuedAt,
        DateTimeOffset sessionExpiresAt)
    {
        var accessExpiresAt = issuedAt.AddMinutes(_settings.AccessTokenLifetimeMinutes);
        if (accessExpiresAt > sessionExpiresAt)
        {
            accessExpiresAt = sessionExpiresAt;
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new("sid", sessionId),
            new(JwtRegisteredClaimNames.Jti, NanoId.New().Value),
            new(JwtRegisteredClaimNames.Iat, issuedAt.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };
        claims.AddRange(roles.Select(role => new Claim("role", role)));

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: accessExpiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(_settings.CreateSigningKey(), SecurityAlgorithms.HmacSha256));

        return new IssuedAccessToken(new JwtSecurityTokenHandler().WriteToken(token), accessExpiresAt);
    }
}
