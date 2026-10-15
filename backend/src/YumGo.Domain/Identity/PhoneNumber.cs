using System.Text.RegularExpressions;

namespace YumGo.Domain.Identity;

public sealed record PhoneNumber
{
    private static readonly Regex E164LikePattern = new(
        @"^\+[1-9][0-9]{7,14}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private PhoneNumber(string value) => Value = value;

    public string Value { get; }

    public static PhoneNumber Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = string.Concat(value.Trim().Where(character =>
            !char.IsWhiteSpace(character) && character is not '(' and not ')' and not '-'));

        if (!E164LikePattern.IsMatch(normalized))
        {
            throw new ArgumentException("The phone number must be a valid E.164-like number.", nameof(value));
        }

        return new PhoneNumber(normalized);
    }

    public override string ToString() => Value;
}
