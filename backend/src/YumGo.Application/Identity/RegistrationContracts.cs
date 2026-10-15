using YumGo.Domain.Identity;

namespace YumGo.Application.Identity;

public interface IPasswordHashingService
{
    PasswordHash Hash(string password);

    bool Verify(string password, PasswordHash passwordHash);
}

public interface ICustomerRegistrationStore
{
    Task<CustomerRegistrationStoreStatus> CreateAsync(
        User user,
        CustomerProfile profile,
        CancellationToken cancellationToken);
}

public enum CustomerRegistrationStoreStatus
{
    Created,
    DuplicateEmail,
    DuplicatePhone
}

public enum RegisterCustomerStatus
{
    Created,
    ValidationFailed,
    DuplicateEmail,
    DuplicatePhone
}

public sealed record RegisterCustomerCommand(
    string? Email,
    string? Phone,
    string? Password,
    string? FullName);

public sealed record RegisteredCustomer(string Id, string Email, string FullName);

public sealed record RegisterCustomerResult(
    RegisterCustomerStatus Status,
    RegisteredCustomer? Customer = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);
