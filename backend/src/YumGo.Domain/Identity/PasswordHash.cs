namespace YumGo.Domain.Identity;

public sealed record PasswordHash
{
    private PasswordHash(string value) => Value = value;

    public string Value { get; }

    public static PasswordHash FromEncodedHash(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return new PasswordHash(value);
    }

    public override string ToString() => "PasswordHash [REDACTED]";
}
