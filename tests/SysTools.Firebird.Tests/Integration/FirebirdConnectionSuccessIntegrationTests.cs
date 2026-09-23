using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Data.Connection;
using SysTools.Entities.Configuration;
using SysTools.Entities.Connection;

namespace SysTools.Firebird.Tests.Integration;

public sealed class FirebirdConnectionSuccessIntegrationTests
{
    [Trait("Category", "FirebirdIntegration")]
    [FirebirdIntegrationFact]
    public async Task Ten_real_connections_succeed_within_six_seconds_each()
    {
        var configuration = LoadConfiguration();
        var probe = new FirebirdConnectionProbe(
            new FirebirdConnectionFactory(),
            NullLogger<FirebirdConnectionProbe>.Instance);

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await probe.ProbeAsync(configuration);
            stopwatch.Stop();

            Assert.Equal(ConnectionTestStatus.Success, result);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(6));
        }
    }

    private static AppConfiguration LoadConfiguration()
    {
        var host = Environment.GetEnvironmentVariable("SYSTOOLS_FIREBIRD_TEST_HOST");
        var database = Environment.GetEnvironmentVariable("SYSTOOLS_FIREBIRD_TEST_DATABASE");
        var user = Environment.GetEnvironmentVariable("SYSTOOLS_FIREBIRD_TEST_USER");
        var password = Environment.GetEnvironmentVariable("SYSTOOLS_FIREBIRD_TEST_PASSWORD");

        return new AppConfiguration
        {
            IpEmpresa = host!,
            RutaEmpresa = database!,
            Usuario = user!,
            Password = password!
        };
    }
}
