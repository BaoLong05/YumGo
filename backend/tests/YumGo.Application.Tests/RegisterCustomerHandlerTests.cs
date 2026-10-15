using YumGo.Application.Identity;
using YumGo.Domain.Identity;

namespace YumGo.Application.Tests;

public sealed class RegisterCustomerHandlerTests
{
    [Fact]
    public async Task Handle_normalizes_and_hashes_inputs_before_persisting_user_and_profile()
    {
        var store = new FakeStore();
        var hasher = new FakePasswordHashingService();
        var handler = CreateHandler(store, hasher);
        var command = new RegisterCustomerCommand(
            " Long.Tran@Example.COM ",
            "+84 912-345-678",
            "Long!Pass1",
            "  Long Tran  ");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(RegisterCustomerStatus.Created, result.Status);
        Assert.Equal("long.tran@example.com", result.Customer?.Email);
        Assert.Equal("Long Tran", result.Customer?.FullName);
        Assert.NotNull(store.User);
        Assert.NotNull(store.Profile);
        Assert.Equal(store.User.Id, store.Profile.UserId);
        Assert.Equal("+84912345678", store.User.PhoneNumber?.Value);
        Assert.Equal("hashed-password", store.User.PasswordHash.Value);
        Assert.Equal("Long!Pass1", hasher.PasswordReceived);
        Assert.Equal(1, store.Calls);
        Assert.Equal(1, hasher.Calls);
    }

    [Fact]
    public async Task Handle_returns_field_errors_without_hashing_or_persisting_invalid_input()
    {
        var store = new FakeStore();
        var hasher = new FakePasswordHashingService();
        var handler = CreateHandler(store, hasher);
        var command = new RegisterCustomerCommand("bad-email", "not-a-phone", "abc", "A");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(RegisterCustomerStatus.ValidationFailed, result.Status);
        Assert.Contains("email", result.Errors!.Keys);
        Assert.Contains("phone", result.Errors.Keys);
        Assert.Contains("password", result.Errors.Keys);
        Assert.Contains("fullName", result.Errors.Keys);
        Assert.Equal(0, hasher.Calls);
        Assert.Equal(0, store.Calls);
    }

    [Theory]
    [InlineData(CustomerRegistrationStoreStatus.DuplicateEmail, RegisterCustomerStatus.DuplicateEmail)]
    [InlineData(CustomerRegistrationStoreStatus.DuplicatePhone, RegisterCustomerStatus.DuplicatePhone)]
    public async Task Handle_maps_database_uniqueness_conflicts(
        CustomerRegistrationStoreStatus storeStatus,
        RegisterCustomerStatus expectedStatus)
    {
        var store = new FakeStore { Result = storeStatus };
        var handler = CreateHandler(store, new FakePasswordHashingService());

        var result = await handler.HandleAsync(
            new RegisterCustomerCommand("long@example.com", null, "Long!Pass1", "Long Tran"),
            CancellationToken.None);

        Assert.Equal(expectedStatus, result.Status);
        Assert.Null(result.Customer);
    }

    private static RegisterCustomerHandler CreateHandler(FakeStore store, FakePasswordHashingService hasher) =>
        new(store, hasher, new RegistrationPasswordPolicy());

    private sealed class FakePasswordHashingService : IPasswordHashingService
    {
        public int Calls { get; private set; }

        public string? PasswordReceived { get; private set; }

        public PasswordHash Hash(string password)
        {
            Calls++;
            PasswordReceived = password;
            return PasswordHash.FromEncodedHash("hashed-password");
        }

        public bool Verify(string password, PasswordHash passwordHash) => true;
    }

    private sealed class FakeStore : ICustomerRegistrationStore
    {
        public int Calls { get; private set; }

        public User? User { get; private set; }

        public CustomerProfile? Profile { get; private set; }

        public CustomerRegistrationStoreStatus Result { get; set; } = CustomerRegistrationStoreStatus.Created;

        public Task<CustomerRegistrationStoreStatus> CreateAsync(
            User user,
            CustomerProfile profile,
            CancellationToken cancellationToken)
        {
            Calls++;
            User = user;
            Profile = profile;
            return Task.FromResult(Result);
        }
    }
}
