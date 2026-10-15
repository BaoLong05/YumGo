using YumGo.Domain.Common;

namespace YumGo.Domain.Identity;

public sealed class User
{
    private User(NanoId id, Email email, PasswordHash passwordHash, PhoneNumber? phoneNumber, UserStatus status)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        PhoneNumber = phoneNumber;
        Status = status;
    }

    public NanoId Id { get; private set; }

    public Email Email { get; private set; }

    public PasswordHash PasswordHash { get; private set; }

    public PhoneNumber? PhoneNumber { get; private set; }

    public UserStatus Status { get; private set; }

    public static User Register(Email email, PasswordHash passwordHash, PhoneNumber? phoneNumber = null)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(passwordHash);

        return new User(NanoId.New(), email, passwordHash, phoneNumber, UserStatus.Active);
    }

    public void Suspend() => Status = UserStatus.Suspended;

    public void Reactivate() => Status = UserStatus.Active;
}
