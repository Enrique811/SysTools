using SysTools.Business.Licensing;
using SysTools.Entities.Licensing;
using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Entities.Configuration;
using SysTools.Licensing.Tests.TestDoubles;

namespace SysTools.Licensing.Tests.Compatibility;

public sealed class LegacyLicenseSignatureTests
{
    internal const string DistributorSignature =
        "Ci3glwlz3PxYfv6MuGCvx9duN7+0IJUI9EBwNQyieRcrvksTiCD6DL7a86XFwaJyhsitD7uslZGBV9CnzM60j1zB6GhjmjM5ZiMey2+LTtMcqSM2NsuslS9rC7C2UHXBh02ZC66zQTip+TmNGya7s3w3EWPJWWEXu9vk9x9H2uUmsEN8zKCdQfZytS5neCOljQbEfLFbRUWR02Q9FatId1dpzc5Peam/wRbHNR2ijeVnm5MIcpPv8aCUMC8iPWYSlckwMz2qUsHAAgaESouc4GcPsnkRFz8tVKAezWviR67s3t9ENc3lPmnz6fxcZp/LB1N+j3VHgBiiT8pNl44pMQ==";
    internal const string DeveloperSignature =
        "asaJLJAiBYh6qGZlrGAykEx4yMsFMjNS4Ev4JABG5UqkXXH1SEJwS9LyqBdHmYboKyOe1drCFND9PFoDBL4+cBGehO65m3M2o4V3lKJtsBauD2ce+mROwDvlrhtCOlZ11GhMy8nk8JiM42FNOg8r8fw3WomdQVQmNy+3lOfsqIvqIjzl/5lyUE494ZijPDx2YCrlXunhqyl1msut7Y7vaCYmhbUIALPbxXNDnx51H4fXifqmEbF0ZBSBKwFI6wnZR7PNuI10QNK8+596Xfi3z+2jnIAL+HjHuhOSHzemclvkh33a0VToZXqIvedFkm2csBQK9fArhrvneOWRaV4jTw==";

    [Theory]
    [InlineData(DistributorSignature, LicenseIssuer.DistributorColombia)]
    [InlineData(DeveloperSignature, LicenseIssuer.Developer)]
    public void Identifies_both_legacy_issuers(string signature, LicenseIssuer issuer)
    {
        var document = LicenseDocumentParser.Parse(LicenseJson.Valid(signature)).Document!;
        var verifier = new LegacyLicenseSignatureVerifier();

        Assert.Equal(issuer, verifier.Verify(document.SignedPayload, document.Signature));
    }

    [Fact]
    public void Rejects_signature_after_payload_mutation()
    {
        var document = LicenseDocumentParser.Parse(LicenseJson.Valid(DistributorSignature)).Document!;
        var mutated = document.SignedPayload.ToArray();
        mutated[0] ^= 1;

        var issuer = new LegacyLicenseSignatureVerifier().Verify(mutated, document.Signature);

        Assert.Equal(LicenseIssuer.None, issuer);
    }

    [Fact]
    public void Invalid_base64_is_a_field_failure()
    {
        var result = LicenseDocumentParser.Parse(LicenseJson.Valid("not base64"));

        Assert.Equal(LicenseParseFailure.InvalidFields, result.Failure);
    }

    [Theory]
    [InlineData(DistributorSignature, LicenseIssuer.DistributorColombia)]
    [InlineData(DeveloperSignature, LicenseIssuer.Developer)]
    public async Task Service_accepts_complete_legacy_vector(
        string signature,
        LicenseIssuer issuer)
    {
        var source = new StubLicenseSourceReader();
        var hardware = new StubHardwareIdProvider { Value = LicenseJson.HardwareId };
        var clock = new StubServerClockRepository { Value = new DateTime(2026, 6, 1) };
        var service = new LicenseService(
            source,
            hardware,
            clock,
            new LegacyLicenseSignatureVerifier(),
            NullLogger<LicenseService>.Instance);

        var result = await service.ValidateAsync(
            LicenseJson.Valid(signature),
            new AppConfiguration());

        Assert.True(result.IsValid);
        Assert.Equal(issuer, result.Issuer);
        Assert.Equal(0, source.Calls);
        Assert.Equal(1, hardware.Calls);
        Assert.Equal(1, clock.Calls);
    }

    [Fact]
    public async Task Service_rejects_invalid_signature_before_external_checks()
    {
        var hardware = new StubHardwareIdProvider { Value = LicenseJson.HardwareId };
        var clock = new StubServerClockRepository();
        var service = new LicenseService(
            new StubLicenseSourceReader(),
            hardware,
            clock,
            new LegacyLicenseSignatureVerifier(),
            NullLogger<LicenseService>.Instance);

        var result = await service.ValidateAsync(
            LicenseJson.Valid(Convert.ToBase64String(new byte[256])),
            new AppConfiguration());

        Assert.Equal(LicenseValidationStatus.InvalidSignature, result.Status);
        Assert.Equal(0, hardware.Calls);
        Assert.Equal(0, clock.Calls);
    }
}
