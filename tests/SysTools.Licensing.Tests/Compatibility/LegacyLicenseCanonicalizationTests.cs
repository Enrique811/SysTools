using System.Text;
using SysTools.Business.Licensing;

namespace SysTools.Licensing.Tests.Compatibility;

public sealed class LegacyLicenseCanonicalizationTests
{
    [Fact]
    public void Produces_exact_Java_payload_bytes()
    {
        var result = LicenseDocumentParser.Parse(LicenseJson.Valid(
            uuid: " 00112233-4455-6677-8899-aabbccddeeff "));

        var payload = Encoding.UTF8.GetString(result.Document!.SignedPayload);

        Assert.Equal(
            "uuid=00112233-4455-6677-8899-AABBCCDDEEFF\n"
            + "inicio=2026-01-01 00:00:00\n"
            + "fin=2026-12-31 23:59:59",
            payload);
        Assert.DoesNotContain('\r', payload);
        Assert.False(payload.EndsWith('\n'));
    }
}
