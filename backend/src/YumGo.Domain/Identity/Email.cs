using System.Net.Mail;

namespace YumGo.Domain.Identity;

public sealed record Email
{
    private Email(string value) => Value = value;

    public string Value { get; }

    public static Email Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = value.Trim().ToLowerInvariant();
        if (!MailAddress.TryCreate(normalized, out var parsed)
            || !string.Equals(parsed.Address, normalized, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The email address is invalid.", nameof(value));
        }

        return new Email(normalized);
    }
}
