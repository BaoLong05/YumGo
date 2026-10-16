using YumGo.Domain.Common;
using YumGo.Domain.Identity;

namespace YumGo.Domain.Addresses;

public sealed class CustomerAddress
{
    private CustomerAddress(
        NanoId id,
        NanoId userId,
        string label,
        string recipientName,
        PhoneNumber recipientPhone,
        string addressLine,
        string ward,
        string district,
        string city,
        decimal latitude,
        decimal longitude,
        bool isDefault,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt)
    {
        Id = id;
        UserId = userId;
        Label = label;
        RecipientName = recipientName;
        RecipientPhone = recipientPhone;
        AddressLine = addressLine;
        Ward = ward;
        District = district;
        City = city;
        Latitude = latitude;
        Longitude = longitude;
        IsDefault = isDefault;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt ?? createdAt;
    }

    public NanoId Id { get; }
    public NanoId UserId { get; }
    public string Label { get; private set; }
    public string RecipientName { get; private set; }
    public PhoneNumber RecipientPhone { get; private set; }
    public string AddressLine { get; private set; }
    public string Ward { get; private set; }
    public string District { get; private set; }
    public string City { get; private set; }
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public bool IsDefault { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static CustomerAddress Create(
        NanoId id,
        NanoId userId,
        string label,
        string recipientName,
        PhoneNumber recipientPhone,
        string addressLine,
        string ward,
        string district,
        string city,
        decimal latitude,
        decimal longitude,
        bool isDefault,
        DateTimeOffset now,
        DateTimeOffset? updatedAt = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(recipientPhone);
        ValidateText(label, nameof(label), 1, 40);
        ValidateText(recipientName, nameof(recipientName), 2, 120);
        ValidateText(addressLine, nameof(addressLine), 5, 255);
        ValidateText(ward, nameof(ward), 1, 100);
        ValidateText(district, nameof(district), 1, 100);
        ValidateText(city, nameof(city), 1, 100);
        ValidateCoordinates(latitude, longitude);
        latitude = decimal.Round(latitude, 6, MidpointRounding.AwayFromZero);
        longitude = decimal.Round(longitude, 6, MidpointRounding.AwayFromZero);

        return new CustomerAddress(
            id,
            userId,
            label.Trim(),
            recipientName.Trim(),
            recipientPhone,
            addressLine.Trim(),
            ward.Trim(),
            district.Trim(),
            city.Trim(),
            latitude,
            longitude,
            isDefault,
            now,
            updatedAt);
    }

    public void Update(
        string label,
        string recipientName,
        PhoneNumber recipientPhone,
        string addressLine,
        string ward,
        string district,
        string city,
        decimal latitude,
        decimal longitude,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(recipientPhone);
        ValidateText(label, nameof(label), 1, 40);
        ValidateText(recipientName, nameof(recipientName), 2, 120);
        ValidateText(addressLine, nameof(addressLine), 5, 255);
        ValidateText(ward, nameof(ward), 1, 100);
        ValidateText(district, nameof(district), 1, 100);
        ValidateText(city, nameof(city), 1, 100);
        ValidateCoordinates(latitude, longitude);
        latitude = decimal.Round(latitude, 6, MidpointRounding.AwayFromZero);
        longitude = decimal.Round(longitude, 6, MidpointRounding.AwayFromZero);

        Label = label.Trim();
        RecipientName = recipientName.Trim();
        RecipientPhone = recipientPhone;
        AddressLine = addressLine.Trim();
        Ward = ward.Trim();
        District = district.Trim();
        City = city.Trim();
        Latitude = latitude;
        Longitude = longitude;
        UpdatedAt = now;
    }

    public void SetDefault(bool isDefault, DateTimeOffset now)
    {
        IsDefault = isDefault;
        UpdatedAt = now;
    }

    private static string ValidateText(string? value, string parameterName, int minimumLength, int maximumLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var trimmed = value.Trim();
        if (trimmed.Length < minimumLength || trimmed.Length > maximumLength)
        {
            throw new ArgumentException(
                $"The value must contain between {minimumLength} and {maximumLength} characters.",
                parameterName);
        }

        return trimmed;
    }

    private static void ValidateCoordinates(decimal latitude, decimal longitude)
    {
        if (latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude));
        }

        if (longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude));
        }
    }
}
