using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Business.Licensing;
using SysTools.Entities.Configuration;
using SysTools.Entities.Licensing;
using SysTools.Licensing.Tests.Compatibility;
using SysTools.Licensing.Tests.TestDoubles;

namespace SysTools.Licensing.Tests.Business;

public sealed class LicensePerformanceTests
{
    [Fact]
    public async Task Local_p95_meets_validation_targets()
    {
        var service = new LicenseService(
            new StubLicenseSourceReader(),
            new StubHardwareIdProvider { Value = LicenseJson.HardwareId },
            new StubServerClockRepository { Value = new DateTime(2026, 6, 1) },
            new StubSignatureVerifier { Issuer = LicenseIssuer.Developer },
            NullLogger<LicenseService>.Instance);
        var configuration = new AppConfiguration();
        await service.ValidateAsync(LicenseJson.Valid(), configuration);

        var rejection = await MeasureAsync(
            200,
            () => service.ValidateAsync(" ", configuration));
        var complete = await MeasureAsync(
            200,
            () => service.ValidateAsync(LicenseJson.Valid(), configuration));

        Assert.True(Percentile95(rejection) < TimeSpan.FromMilliseconds(50));
        Assert.True(Percentile95(complete) < TimeSpan.FromMilliseconds(250));
    }

    private static async Task<List<TimeSpan>> MeasureAsync(
        int iterations,
        Func<Task<LicenseValidationResult>> operation)
    {
        var values = new List<TimeSpan>(iterations);
        for (var index = 0; index < iterations; index++)
        {
            var stopwatch = Stopwatch.StartNew();
            await operation();
            stopwatch.Stop();
            values.Add(stopwatch.Elapsed);
        }

        return values;
    }

    private static TimeSpan Percentile95(List<TimeSpan> values)
    {
        values.Sort();
        return values[(int)Math.Ceiling(values.Count * 0.95) - 1];
    }
}
