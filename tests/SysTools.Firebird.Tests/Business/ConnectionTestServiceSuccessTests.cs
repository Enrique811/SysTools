using SysTools.Business.Connection;
using SysTools.Entities.Connection;
using SysTools.Firebird.Tests.TestDoubles;

namespace SysTools.Firebird.Tests.Business;

public sealed class ConnectionTestServiceSuccessTests
{
    [Fact]
    public async Task Test_accepts_unsaved_configuration_and_returns_safe_success()
    {
        var configuration = FirebirdTestConfiguration.Valid();
        var probe = new FakeFirebirdConnectionProbe();
        var logger = new ListLogger<ConnectionTestService>();
        var service = new ConnectionTestService(probe, logger);

        var result = await service.TestAsync(configuration);

        Assert.Equal(ConnectionTestStatus.Success, result.Status);
        Assert.Equal("La conexión con Firebird se estableció correctamente.", result.Message);
        Assert.True(result.Duration >= TimeSpan.Zero);
        Assert.Empty(result.Issues);
        Assert.Same(configuration, probe.LastConfiguration);
        Assert.Equal("Systools-Fb-Secret-Test", configuration.Password);
        Assert.Contains(logger.Entries, entry => entry.Message.Contains("ConnectionTestStarted", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry => entry.Message.Contains("ConnectionTestCompleted", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Test_forwards_consumer_cancellation_token()
    {
        var probe = new FakeFirebirdConnectionProbe();
        var service = new ConnectionTestService(probe, new ListLogger<ConnectionTestService>());
        using var source = new CancellationTokenSource();

        await service.TestAsync(FirebirdTestConfiguration.Valid(), source.Token);

        Assert.Equal(source.Token, probe.LastCancellationToken);
    }
}
