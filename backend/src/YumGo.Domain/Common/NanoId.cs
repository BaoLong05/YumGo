using System.Security.Cryptography;

namespace YumGo.Domain.Common;

public sealed record NanoId
{
    private const string Alphabet = "_-0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int Length = 21;

    private NanoId(string value) => Value = value;

    public string Value { get; }

    public static NanoId New()
    {
        var bytes = RandomNumberGenerator.GetBytes(Length);
        var characters = new char[Length];

        for (var index = 0; index < bytes.Length; index++)
        {
            characters[index] = Alphabet[bytes[index] & 63];
        }

        return new NanoId(new string(characters));
    }

    public static NanoId Create(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length != Length || value.Any(character => !Alphabet.Contains(character)))
        {
            throw new ArgumentException("A NanoID must contain exactly 21 URL-safe characters.", nameof(value));
        }

        return new NanoId(value);
    }

    public override string ToString() => Value;
}
