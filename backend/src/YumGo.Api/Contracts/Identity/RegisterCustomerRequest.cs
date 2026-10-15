namespace YumGo.Api.Contracts.Identity;

public sealed record RegisterCustomerRequest(
    string? Email,
    string? Phone,
    string? Password,
    string? FullName);
