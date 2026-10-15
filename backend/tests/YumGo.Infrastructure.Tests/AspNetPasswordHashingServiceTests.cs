using YumGo.Domain.Identity;
using YumGo.Infrastructure.Security;

namespace YumGo.Infrastructure.Tests;

public sealed class AspNetPasswordHashingServiceTests
{
    [Fact]
    public void Hash_uses_a_verifiable_one_way_identity_hash()
    {
        const string password = "Secure!Pass1";
        var service = new AspNetPasswordHashingService();

        var hash = service.Hash(password);

        Assert.NotEqual(password, hash.Value);
        Assert.True(service.Verify(password, hash));
        Assert.False(service.Verify("Wrong!Pass1", hash));
        Assert.StartsWith("AQAAAA", hash.Value, StringComparison.Ordinal);
    }
}
