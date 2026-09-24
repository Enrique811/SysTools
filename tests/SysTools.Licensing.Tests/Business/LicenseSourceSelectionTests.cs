using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Business.Licensing;
using SysTools.Entities.Configuration;
using SysTools.Entities.Licensing;
using SysTools.Licensing.Tests.Compatibility;
using SysTools.Licensing.Tests.TestDoubles;

namespace SysTools.Licensing.Tests.Business;

public sealed class LicenseSourceSelectionTests
{
    [Fact]
    public async Task Direct_json_never_invokes_source_reader()
    {
        var source = new StubLicenseSourceReader { Exception = new InvalidOperationException() };
        var service = CreateService(source);

        var result = await service.ValidateAsync(
            $"  {LicenseJson.Valid()}  ",
            new AppConfiguration());

        Assert.True(result.IsValid);
        Assert.Equal(0, source.Calls);
    }

    [Fact]
    public async Task File_path_is_trimmed_and_read_once()
    {
        var source = new StubLicenseSourceReader
        {
            Result = LicenseSourceReadResult.Success(LicenseJson.Valid())
        };
        var service = CreateService(source);

        var result = await service.ValidateAsync("  licenses/demo.lic  ", new AppConfiguration());

        Assert.True(result.IsValid);
        Assert.Equal(1, source.Calls);
        Assert.Equal("licenses/demo.lic", source.LastPath);
    }

    [Fact]
    public async Task Source_failure_has_safe_status()
    {
        var source = new StubLicenseSourceReader
        {
            Result = LicenseSourceReadResult.Failed(LicenseSourceFailure.NotFound)
        };

        var result = await CreateService(source).ValidateAsync(
            "sentinel-secret-path.lic",
            new AppConfiguration());

        Assert.Equal(LicenseValidationStatus.SourceUnavailable, result.Status);
        Assert.DoesNotContain("sentinel", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Missing_input_is_controlled(string? value)
    {
        var result = await CreateService(new StubLicenseSourceReader())
            .ValidateAsync(value, new AppConfiguration());

        Assert.Equal(LicenseValidationStatus.MissingInput, result.Status);
    }

    private static LicenseService CreateService(StubLicenseSourceReader source) =>
        new(
            source,
            new StubHardwareIdProvider { Value = LicenseJson.HardwareId },
            new StubServerClockRepository { Value = new DateTime(2026, 6, 1) },
            new StubSignatureVerifier { Issuer = LicenseIssuer.Developer },
            NullLogger<LicenseService>.Instance);
}
