using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Business.Licensing;
using SysTools.Entities.Configuration;
using SysTools.Entities.Licensing;
using SysTools.Licensing.Tests.TestDoubles;

namespace SysTools.Licensing.Tests.Compatibility;

public sealed class LicenseMutationMatrixTests
{
    public static TheoryData<string, LicenseValidationStatus> Mutations => new()
    {
        { "", LicenseValidationStatus.MissingInput },
        { "[]", LicenseValidationStatus.SourceUnavailable },
        { "{", LicenseValidationStatus.InvalidJson },
        { "{}", LicenseValidationStatus.InvalidFields },
        { "{\"inicio\":\"2026-01-01 00:00:00\",\"fin\":\"2026-12-31 23:59:59\",\"firma\":\"AQID\"}", LicenseValidationStatus.InvalidFields },
        { "{\"uuid\":\"00112233-4455-6677-8899-AABBCCDDEEFF\",\"fin\":\"2026-12-31 23:59:59\",\"firma\":\"AQID\"}", LicenseValidationStatus.InvalidFields },
        { "{\"uuid\":\"00112233-4455-6677-8899-AABBCCDDEEFF\",\"inicio\":\"2026-01-01 00:00:00\",\"firma\":\"AQID\"}", LicenseValidationStatus.InvalidFields },
        { "{\"uuid\":\"00112233-4455-6677-8899-AABBCCDDEEFF\",\"inicio\":\"2026-01-01 00:00:00\",\"fin\":\"2026-12-31 23:59:59\"}", LicenseValidationStatus.InvalidFields },
        { LicenseJson.Valid(uuid: "bad"), LicenseValidationStatus.InvalidFields },
        { LicenseJson.Valid(uuid: "{00112233-4455-6677-8899-AABBCCDDEEFF}"), LicenseValidationStatus.InvalidFields },
        { LicenseJson.Valid(uuid: "00112233445566778899AABBCCDDEEFF"), LicenseValidationStatus.InvalidFields },
        { LicenseJson.Valid(uuid: "00112233-4455-6677-8899-AABBCCDDEEFG"), LicenseValidationStatus.InvalidFields },
        { LicenseJson.Valid(start: "2026/01/01 00:00:00"), LicenseValidationStatus.InvalidFields },
        { LicenseJson.Valid(start: "2026-02-29 00:00:00"), LicenseValidationStatus.InvalidFields },
        { LicenseJson.Valid(end: "2026-13-01 00:00:00"), LicenseValidationStatus.InvalidFields },
        { LicenseJson.Valid(start: "2027-01-01 00:00:00", end: "2026-12-31 23:59:59"), LicenseValidationStatus.InvalidFields },
        { LicenseJson.Valid(signature: ""), LicenseValidationStatus.InvalidFields },
        { LicenseJson.Valid(signature: "%%%"), LicenseValidationStatus.InvalidFields },
        { LicenseJson.Valid().Replace("\"uuid\":", "\"uuid\":\"00112233-4455-6677-8899-AABBCCDDEEFF\",\"uuid\":"), LicenseValidationStatus.InvalidFields },
        { LicenseJson.Valid().Replace("\"inicio\":", "\"inicio\":12,\"ignored\":"), LicenseValidationStatus.InvalidFields },
        { LicenseJson.Valid(LegacyLicenseSignatureTests.DistributorSignature, uuid: "11111111-2222-3333-4444-555555555555"), LicenseValidationStatus.InvalidSignature },
        { LicenseJson.Valid(LegacyLicenseSignatureTests.DistributorSignature, start: "2026-01-02 00:00:00"), LicenseValidationStatus.InvalidSignature },
        { LicenseJson.Valid(LegacyLicenseSignatureTests.DistributorSignature, end: "2026-12-30 23:59:59"), LicenseValidationStatus.InvalidSignature },
        { LicenseJson.Valid(Convert.ToBase64String(new byte[256])), LicenseValidationStatus.InvalidSignature },
        { LicenseJson.Valid(LegacyLicenseSignatureTests.DeveloperSignature, uuid: "00112233-4455-6677-8899-AABBCCDDEE00"), LicenseValidationStatus.InvalidSignature }
    };

    [Theory]
    [MemberData(nameof(Mutations))]
    public async Task Rejects_each_mutation_with_expected_status(
        string json,
        LicenseValidationStatus expected)
    {
        var service = new LicenseService(
            new StubLicenseSourceReader(),
            new StubHardwareIdProvider { Value = LicenseJson.HardwareId },
            new StubServerClockRepository { Value = new DateTime(2026, 6, 1) },
            new LegacyLicenseSignatureVerifier(),
            NullLogger<LicenseService>.Instance);

        var result = await service.ValidateAsync(json, new AppConfiguration());

        Assert.Equal(expected, result.Status);
        Assert.False(result.IsValid);
    }
}
