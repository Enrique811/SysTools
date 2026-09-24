using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Business.Licensing;
using SysTools.Entities.Configuration;
using SysTools.Entities.Licensing;
using SysTools.Licensing.Tests.Compatibility;
using SysTools.Licensing.Tests.TestDoubles;

namespace SysTools.Licensing.Tests.Business;

public sealed class LicenseHardwareValidationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-uuid")]
    [InlineData("{00112233-4455-6677-8899-AABBCCDDEEFF}")]
    public async Task Unavailable_or_noncanonical_uuid_fails_before_server(string? value)
    {
        var hardware = new StubHardwareIdProvider { Value = value };
        var clock = new StubServerClockRepository();

        var result = await CreateService(hardware, clock).ValidateAsync(
            LicenseJson.Valid(),
            new AppConfiguration());

        Assert.Equal(LicenseValidationStatus.HardwareIdUnavailable, result.Status);
        Assert.Equal(0, clock.Calls);
    }

    [Fact]
    public async Task Different_uuid_fails_before_server()
    {
        var clock = new StubServerClockRepository();

        var result = await CreateService(
                new StubHardwareIdProvider { Value = "11111111-2222-3333-4444-555555555555" },
                clock)
            .ValidateAsync(LicenseJson.Valid(), new AppConfiguration());

        Assert.Equal(LicenseValidationStatus.HardwareMismatch, result.Status);
        Assert.Equal(0, clock.Calls);
    }

    [Fact]
    public async Task Uuid_comparison_trims_and_ignores_case()
    {
        var result = await CreateService(
                new StubHardwareIdProvider { Value = " 00112233-4455-6677-8899-aabbccddeeff " },
                new StubServerClockRepository { Value = new DateTime(2026, 6, 1) })
            .ValidateAsync(LicenseJson.Valid(), new AppConfiguration());

        Assert.True(result.IsValid);
    }

    private static LicenseService CreateService(
        StubHardwareIdProvider hardware,
        StubServerClockRepository clock) =>
        new(
            new StubLicenseSourceReader(),
            hardware,
            clock,
            new StubSignatureVerifier { Issuer = LicenseIssuer.Developer },
            NullLogger<LicenseService>.Instance);
}
