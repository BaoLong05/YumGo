using YumGo.Application.Addresses;
using YumGo.Domain.Addresses;
using YumGo.Domain.Common;
using YumGo.Domain.Identity;

namespace YumGo.Application.Tests;

public sealed class CustomerAddressHandlerTests
{
    [Fact]
    public async Task Create_reverse_geocodes_coordinates_and_persists_only_provider_address_data()
    {
        var store = new FakeStore();
        var geocoder = new FakeGeocoder(new ReverseGeocodedAddress(
            "12 Nguyen Hue", "Ben Nghe", "District 1", "Ho Chi Minh City"));
        var handler = new CustomerAddressHandler(store, geocoder, TimeProvider.System);

        var result = await handler.CreateAsync(
            new CreateCustomerAddressCommand(
                NanoId.New().Value, "Home", "Long Tran", "+84912345678", 10.7769m, 106.7009m, true),
            CancellationToken.None);

        Assert.Equal(CustomerAddressStatus.Created, result.Status);
        Assert.Equal("12 Nguyen Hue", result.Address?.AddressLine);
        Assert.Equal("Ben Nghe", result.Address?.Ward);
        Assert.Equal(10.7769m, geocoder.LatitudeReceived);
        Assert.Equal(106.7009m, geocoder.LongitudeReceived);
        Assert.Equal(result.Address?.Id, store.CreatedAddress?.Id.Value);
    }

    [Fact]
    public async Task Create_rejects_invalid_coordinates_before_calling_geocoder_or_store()
    {
        var store = new FakeStore();
        var geocoder = new FakeGeocoder(new ReverseGeocodedAddress(
            "12 Nguyen Hue", "Ben Nghe", "District 1", "Ho Chi Minh City"));
        var handler = new CustomerAddressHandler(store, geocoder, TimeProvider.System);

        var result = await handler.CreateAsync(
            new CreateCustomerAddressCommand(
                NanoId.New().Value, "Home", "Long Tran", "+84912345678", 91m, 106m, false),
            CancellationToken.None);

        Assert.Equal(CustomerAddressStatus.ValidationFailed, result.Status);
        Assert.Contains("latitude", result.Errors!.Keys);
        Assert.Equal(0, geocoder.Calls);
        Assert.Null(store.CreatedAddress);
    }

    [Fact]
    public async Task Update_does_not_allow_access_to_another_users_address()
    {
        var ownerId = NanoId.New();
        var address = CustomerAddress.Create(
            NanoId.New(), ownerId, "Home", "Long Tran", PhoneNumber.Create("+84912345678"),
            "12 Nguyen Hue", "Ben Nghe", "District 1", "Ho Chi Minh City",
            10.7769m, 106.7009m, false, DateTimeOffset.UtcNow);
        var store = new FakeStore { Address = address };
        var geocoder = new FakeGeocoder(null);
        var handler = new CustomerAddressHandler(store, geocoder, TimeProvider.System);

        var result = await handler.UpdateAsync(
            NanoId.New().Value,
            address.Id.Value,
            new UpdateCustomerAddressCommand(null, null, null, null, null, null),
            CancellationToken.None);

        Assert.Equal(CustomerAddressStatus.NotFound, result.Status);
        Assert.Equal(0, geocoder.Calls);
    }

    [Fact]
    public async Task Update_does_not_spend_a_geocoding_request_when_rounded_coordinates_are_unchanged()
    {
        var ownerId = NanoId.New();
        var address = CustomerAddress.Create(
            NanoId.New(), ownerId, "Home", "Long Tran", PhoneNumber.Create("+84912345678"),
            "12 Nguyen Hue", "Ben Nghe", "District 1", "Ho Chi Minh City",
            10.7769m, 106.7009m, false, DateTimeOffset.UtcNow);
        var store = new FakeStore { Address = address };
        var geocoder = new FakeGeocoder(null);
        var handler = new CustomerAddressHandler(store, geocoder, TimeProvider.System);

        var result = await handler.UpdateAsync(
            ownerId.Value,
            address.Id.Value,
            new UpdateCustomerAddressCommand(null, null, null, 10.7769001m, 106.7009001m, null),
            CancellationToken.None);

        Assert.Equal(CustomerAddressStatus.Success, result.Status);
        Assert.Equal(0, geocoder.Calls);
    }

    private sealed class FakeGeocoder(ReverseGeocodedAddress? result) : IAddressReverseGeocoder
    {
        public int Calls { get; private set; }
        public decimal? LatitudeReceived { get; private set; }
        public decimal? LongitudeReceived { get; private set; }

        public Task<ReverseGeocodedAddress?> ReverseGeocodeAsync(
            decimal latitude,
            decimal longitude,
            CancellationToken cancellationToken)
        {
            Calls++;
            LatitudeReceived = latitude;
            LongitudeReceived = longitude;
            return Task.FromResult(result);
        }
    }

    private sealed class FakeStore : ICustomerAddressStore
    {
        public CustomerAddress? Address { get; init; }
        public CustomerAddress? CreatedAddress { get; private set; }

        public Task<IReadOnlyList<CustomerAddress>> ListAsync(NanoId userId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CustomerAddress>>(Address is not null && Address.UserId == userId ? [Address] : []);

        public Task<CustomerAddress?> FindAsync(NanoId userId, NanoId addressId, CancellationToken cancellationToken) =>
            Task.FromResult(Address is not null && Address.UserId == userId && Address.Id == addressId ? Address : null);

        public Task CreateAsync(CustomerAddress address, CancellationToken cancellationToken)
        {
            CreatedAddress = address;
            return Task.CompletedTask;
        }

        public Task<bool> UpdateAsync(CustomerAddress address, bool? isDefaultOverride, CancellationToken cancellationToken) =>
            Task.FromResult(Address is not null && Address.Id == address.Id && Address.UserId == address.UserId);

        public Task<bool> DeleteAsync(NanoId userId, NanoId addressId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> SetDefaultAsync(NanoId userId, NanoId addressId, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }
}
