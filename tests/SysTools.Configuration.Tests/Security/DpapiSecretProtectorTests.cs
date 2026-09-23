using SysTools.Data.Configuration;

namespace SysTools.Configuration.Tests.Security;

public sealed class DpapiSecretProtectorTests
{
    [Fact]
    [Trait("Category", "Security")]
    public void CurrentUserProtection_RoundTripsAndDoesNotStorePlaintext()
    {
        const string plaintext = "Systools-Test-Secret-9f4d";
        var protector = new DpapiSecretProtector();

        var first = protector.Protect(plaintext);
        var second = protector.Protect(plaintext);

        Assert.NotEqual(plaintext, first);
        Assert.NotEqual(first, second);
        Assert.NotEmpty(Convert.FromBase64String(first));
        Assert.Equal(plaintext, protector.Unprotect(first));
        Assert.Equal(plaintext, protector.Unprotect(second));
    }
}
