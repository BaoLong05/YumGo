using YumGo.Domain.Common;

namespace YumGo.Domain.Identity;

public sealed class CustomerProfile
{
    private CustomerProfile(NanoId id, NanoId userId, string fullName)
    {
        Id = id;
        UserId = userId;
        FullName = fullName;
    }

    public NanoId Id { get; private set; }

    public NanoId UserId { get; private set; }

    public string FullName { get; private set; }

    public static CustomerProfile Register(NanoId userId, string fullName)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);

        var normalizedFullName = fullName.Trim();
        if (normalizedFullName.Length is < 2 or > 120)
        {
            throw new ArgumentException("A customer's full name must contain between 2 and 120 characters.", nameof(fullName));
        }

        return new CustomerProfile(NanoId.New(), userId, normalizedFullName);
    }
}
