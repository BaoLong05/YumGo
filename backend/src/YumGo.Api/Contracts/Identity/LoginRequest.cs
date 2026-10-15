namespace YumGo.Api.Contracts.Identity;

public sealed record LoginRequest(string? Email, string? Password);

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    string TokenType);
