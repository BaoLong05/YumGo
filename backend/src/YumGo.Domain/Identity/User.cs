using YumGo.Domain.Common;

namespace YumGo.Domain.Identity;

public sealed class User
{
    private User(NanoId id, Email email, PasswordHash passwordHash, UserStatus status)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        Status = status;
    }

    public NanoId Id { get; private set; }

    public Email Email { get; private set; }

    public PasswordHash PasswordHash { get; private set; }

    public UserStatus Status { get; private set; }

    public static User Register(Email email, PasswordHash passwordHash)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(passwordHash);

        return new User(NanoId.New(), email, passwordHash, UserStatus.Active);
    }

    public void Suspend() => Status = UserStatus.Suspended;

    public void Reactivate() => Status = UserStatus.Active;
}
