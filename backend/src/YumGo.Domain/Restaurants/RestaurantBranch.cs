using YumGo.Domain.Common;
using YumGo.Domain.Identity;

namespace YumGo.Domain.Restaurants;

public sealed class RestaurantBranch
{
    private RestaurantBranch(NanoId id, NanoId restaurantId, string name, PhoneNumber phone, string addressLine,
        string ward, string district, string city, decimal latitude, decimal longitude, TimeOnly? opensAt,
        TimeOnly? closesAt, string status, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        Id = id;
        RestaurantId = restaurantId;
        Name = name;
        Phone = phone;
        AddressLine = addressLine;
        Ward = ward;
        District = district;
        City = city;
        Latitude = latitude;
        Longitude = longitude;
        OpensAt = opensAt;
        ClosesAt = closesAt;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        Status = status;
    }

    public NanoId Id { get; }
    public NanoId RestaurantId { get; }
    public string Name { get; }
    public PhoneNumber Phone { get; }
    public string AddressLine { get; }
    public string Ward { get; }
    public string District { get; }
    public string City { get; }
    public decimal Latitude { get; }
    public decimal Longitude { get; }
    public string Status { get; }
    public TimeOnly? OpensAt { get; }
    public TimeOnly? ClosesAt { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; }

    public static RestaurantBranch Create(NanoId id, NanoId restaurantId, string name, string phone, string addressLine,
        string ward, string district, string city, decimal latitude, decimal longitude, TimeOnly? opensAt,
        TimeOnly? closesAt, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(restaurantId);
        var normalizedName = ValidateText(name, nameof(name), 2, 160);
        var normalizedPhone = PhoneNumber.Create(phone);
        var normalizedAddress = ValidateText(addressLine, nameof(addressLine), 5, 255);
        var normalizedWard = ValidateText(ward, nameof(ward), 1, 100);
        var normalizedDistrict = ValidateText(district, nameof(district), 1, 100);
        var normalizedCity = ValidateText(city, nameof(city), 1, 100);
        if (latitude is < -90 or > 90) throw new ArgumentOutOfRangeException(nameof(latitude));
        if (longitude is < -180 or > 180) throw new ArgumentOutOfRangeException(nameof(longitude));
        if (opensAt.HasValue != closesAt.HasValue)
            throw new ArgumentException("Opening and closing times must be provided together.", nameof(opensAt));
        if (opensAt.HasValue && opensAt.Value >= closesAt.GetValueOrDefault())
            throw new ArgumentException("Overnight schedules are not supported; opening time must be before closing time.", nameof(opensAt));

        return new RestaurantBranch(id, restaurantId, normalizedName, normalizedPhone, normalizedAddress,
            normalizedWard, normalizedDistrict, normalizedCity,
            decimal.Round(latitude, 6, MidpointRounding.AwayFromZero),
            decimal.Round(longitude, 6, MidpointRounding.AwayFromZero), opensAt, closesAt, "draft", now, now);
    }

    public RestaurantBranch SetVisibility(bool isOpen, DateTimeOffset now)
    {
        if (Status == "suspended")
            throw new InvalidOperationException("A suspended branch cannot be published or closed by its owner.");

        var targetStatus = isOpen ? "open" : "closed";
        if (Status == targetStatus) return this;

        return new RestaurantBranch(Id, RestaurantId, Name, Phone, AddressLine, Ward, District, City,
            Latitude, Longitude, OpensAt, ClosesAt, targetStatus, CreatedAt, now);
    }

    public static RestaurantBranch Restore(NanoId id, NanoId restaurantId, string name, string phone,
        string addressLine, string ward, string district, string city, decimal latitude, decimal longitude,
        string status, TimeOnly? opensAt, TimeOnly? closesAt, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        if (status is not ("draft" or "open" or "closed" or "suspended"))
            throw new ArgumentException("Unknown branch status.", nameof(status));
        var created = Create(id, restaurantId, name, phone, addressLine, ward, district, city,
            latitude, longitude, opensAt, closesAt, createdAt);
        return new RestaurantBranch(created.Id, created.RestaurantId, created.Name, created.Phone,
            created.AddressLine, created.Ward, created.District, created.City, created.Latitude,
            created.Longitude, created.OpensAt, created.ClosesAt, status, createdAt, updatedAt);
    }

    private static string ValidateText(string? value, string parameterName, int min, int max)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length < min || normalized.Length > max)
            throw new ArgumentException($"The value must contain between {min} and {max} characters.", parameterName);
        return normalized;
    }
}
