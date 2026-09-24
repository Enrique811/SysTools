using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Business.Licensing;
using SysTools.Business.Repositories;
using SysTools.Entities.Configuration;
using SysTools.Entities.Licensing;
using SysTools.Licensing.Tests.Compatibility;
using SysTools.Licensing.Tests.TestDoubles;

namespace SysTools.Licensing.Tests.Business;

public sealed class LicenseValidityTests
{
    [Theory]
    [InlineData("2026-01-01 00:00:00", LicenseValidationStatus.Valid)]
    [InlineData("2026-12-31 23:59:59", LicenseValidationStatus.Valid)]
    [InlineData("2025-12-31 23:59:59", LicenseValidationStatus.NotYetValid)]
    [InlineData("2027-01-01 00:00:00", LicenseValidationStatus.Expired)]
    public async Task Uses_inclusive_server_boundaries_only(
        string serverValue,
        LicenseValidationStatus expected)
    {
        var clock = new StubServerClockRepository
        {
            Value = DateTime.Parse(serverValue, System.Globalization.CultureInfo.InvariantCulture)
        };

        var result = await CreateService(clock).ValidateAsync(
            LicenseJson.Valid(),
            new AppConfiguration());

        Assert.Equal(expected, result.Status);
        Assert.Equal(1, clock.Calls);
    }

    [Fact]
    public async Task Repository_failure_is_safe_server_time_status()
    {
        var clock = new StubServerClockRepository
        {
            Exception = new RepositoryAccessException(RepositoryOperation.GetServerTimestamp)
        };

        var result = await CreateService(clock).ValidateAsync(
            LicenseJson.Valid(),
            new AppConfiguration());

        Assert.Equal(LicenseValidationStatus.ServerTimeUnavailable, result.Status);
        Assert.Null(result.ServerTime);
    }

    [Fact]
    public async Task Requested_cancellation_is_propagated()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            CreateService(new StubServerClockRepository()).ValidateAsync(
                LicenseJson.Valid(),
                new AppConfiguration(),
                cancellation.Token));
    }

    private static LicenseService CreateService(StubServerClockRepository clock) =>
        new(
            new StubLicenseSourceReader(),
            new StubHardwareIdProvider { Value = LicenseJson.HardwareId },
            clock,
            new StubSignatureVerifier { Issuer = LicenseIssuer.Developer },
            NullLogger<LicenseService>.Instance);
}
