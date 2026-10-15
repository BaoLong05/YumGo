using YumGo.Domain.Identity;

namespace YumGo.Application.Identity;

public sealed class RegisterCustomerHandler(
    ICustomerRegistrationStore store,
    IPasswordHashingService passwordHashingService,
    RegistrationPasswordPolicy passwordPolicy)
{
    public async Task<RegisterCustomerResult> HandleAsync(
        RegisterCustomerCommand command,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        Email? email = null;
        PhoneNumber? phone = null;
        string? fullName = null;

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            AddError(errors, "email", "Email is required.");
        }
        else
        {
            try
            {
                email = Email.Create(command.Email);
            }
            catch (ArgumentException)
            {
                AddError(errors, "email", "Enter a valid email address.");
            }
        }

        if (!string.IsNullOrWhiteSpace(command.Phone))
        {
            try
            {
                phone = PhoneNumber.Create(command.Phone);
            }
            catch (ArgumentException)
            {
                AddError(errors, "phone", "Enter a valid international phone number.");
            }
        }

        if (command.Password is null)
        {
            AddError(errors, "password", "Password is required.");
        }
        else if (!passwordPolicy.IsValid(command.Password))
        {
            AddError(errors, "password", "Password must be at least 6 characters and contain at least 3 of these groups: uppercase, lowercase, digit, and symbol.");
        }

        if (string.IsNullOrWhiteSpace(command.FullName))
        {
            AddError(errors, "fullName", "Full name is required.");
        }
        else
        {
            fullName = command.FullName.Trim();
            if (fullName.Length is < 2 or > 120)
            {
                AddError(errors, "fullName", "Full name must contain between 2 and 120 characters.");
            }
        }

        if (errors.Count > 0)
        {
            return new RegisterCustomerResult(
                RegisterCustomerStatus.ValidationFailed,
                Errors: errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal));
        }

        var user = User.Register(email!, passwordHashingService.Hash(command.Password!), phone);
        var profile = CustomerProfile.Register(user.Id, fullName!);
        var storeStatus = await store.CreateAsync(user, profile, cancellationToken);

        return storeStatus switch
        {
            CustomerRegistrationStoreStatus.Created => new RegisterCustomerResult(
                RegisterCustomerStatus.Created,
                new RegisteredCustomer(user.Id.Value, user.Email.Value, profile.FullName)),
            CustomerRegistrationStoreStatus.DuplicateEmail => new RegisterCustomerResult(RegisterCustomerStatus.DuplicateEmail),
            CustomerRegistrationStoreStatus.DuplicatePhone => new RegisterCustomerResult(RegisterCustomerStatus.DuplicatePhone),
            _ => throw new ArgumentOutOfRangeException(nameof(storeStatus), storeStatus, "Unknown customer registration store status.")
        };
    }

    private static void AddError(IDictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.TryGetValue(field, out var messages))
        {
            messages = [];
            errors.Add(field, messages);
        }

        messages.Add(message);
    }
}
