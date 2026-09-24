using SysTools.Business.Licensing;
using SysTools.Entities.Configuration;
using SysTools.Entities.Licensing;
using SysTools.Licensing.Tests.Compatibility;
using SysTools.Licensing.Tests.TestDoubles;

namespace SysTools.Licensing.Tests.Security;

public sealed class LicenseSecurityLoggingTests
{
    private static readonly string[] Sentinels =
    [
        "SENTINEL-LICENSE",
        "SENTINEL-SIGNATURE",
        "SENTINEL-UUID",
        "SENTINEL-PATH",
        "SENTINEL-HOST",
        "SENTINEL-PASSWORD",
        "SENTINEL-CONNECTION"
    ];

    [Fact]
    public async Task Source_exception_values_do_not_escape_result_or_log()
    {
        var logger = new CollectingLogger<LicenseService>();
        var source = new StubLicenseSourceReader
        {
            Exception = new IOException(string.Join('|', Sentinels))
        };
        var service = CreateService(source, new StubHardwareIdProvider(), new StubServerClockRepository(), logger);

        var result = await service.ValidateAsync("SENTINEL-PATH.lic", SensitiveConfiguration());

        Assert.Equal(LicenseValidationStatus.SourceUnavailable, result.Status);
        AssertSafe(result, logger.Messages);
    }

    [Fact]
    public async Task Hardware_and_server_exceptions_do_not_escape_result_or_log()
    {
        foreach (var stage in new[] { "hardware", "server" })
        {
            var logger = new CollectingLogger<LicenseService>();
            var hardware = new StubHardwareIdProvider { Value = LicenseJson.HardwareId };
            var clock = new StubServerClockRepository { Value = new DateTime(2026, 6, 1) };
            if (stage == "hardware")
            {
                hardware.Exception = new InvalidOperationException(string.Join('|', Sentinels));
            }
            else
            {
                clock.Exception = new InvalidOperationException(string.Join('|', Sentinels));
            }

            var result = await CreateService(
                    new StubLicenseSourceReader(),
                    hardware,
                    clock,
                    logger)
                .ValidateAsync(LicenseJson.Valid(), SensitiveConfiguration());

            AssertSafe(result, logger.Messages);
        }
    }

    [Fact]
    public async Task Requested_cancellation_is_not_logged()
    {
        var logger = new CollectingLogger<LicenseService>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var service = CreateService(
            new StubLicenseSourceReader(),
            new StubHardwareIdProvider(),
            new StubServerClockRepository(),
            logger);

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.ValidateAsync(
            LicenseJson.Valid(),
            SensitiveConfiguration(),
            cancellation.Token));

        Assert.Empty(logger.Messages);
    }

    private static LicenseService CreateService(
        StubLicenseSourceReader source,
        StubHardwareIdProvider hardware,
        StubServerClockRepository clock,
        CollectingLogger<LicenseService> logger) =>
        new(
            source,
            hardware,
            clock,
            new StubSignatureVerifier { Issuer = LicenseIssuer.Developer },
            logger);

    private static AppConfiguration SensitiveConfiguration() => new()
    {
        IpEmpresa = "SENTINEL-HOST",
        Password = "SENTINEL-PASSWORD",
        RutaEmpresa = "SENTINEL-CONNECTION",
        Licencia = "SENTINEL-LICENSE"
    };

    private static void AssertSafe(
        LicenseValidationResult result,
        IReadOnlyCollection<string> messages)
    {
        var visible = result.Message + string.Join('|', messages);
        foreach (var sentinel in Sentinels)
        {
            Assert.DoesNotContain(sentinel, visible, StringComparison.Ordinal);
        }
    }
}
