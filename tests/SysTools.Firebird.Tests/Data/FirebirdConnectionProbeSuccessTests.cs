using Microsoft.Extensions.Logging;
using SysTools.Data.Connection;
using SysTools.Entities.Connection;
using SysTools.Firebird.Tests.TestDoubles;

namespace SysTools.Firebird.Tests.Data;

public sealed class FirebirdConnectionProbeSuccessTests
{
    [Fact]
    public async Task Probe_opens_with_linked_token_and_disposes_exactly_once()
    {
        var connection = new TrackingDbConnection();
        var factory = new FakeFirebirdConnectionFactory(_ => connection);
        var logger = new ListLogger<FirebirdConnectionProbe>();
        var probe = new FirebirdConnectionProbe(factory, logger);
        using var source = new CancellationTokenSource();

        var result = await probe.ProbeAsync(FirebirdTestConfiguration.Valid(), source.Token);

        Assert.Equal(ConnectionTestStatus.Success, result);
        Assert.Equal(1, factory.Calls);
        Assert.Equal(1, connection.OpenCalls);
        Assert.Equal(1, connection.DisposeCalls);
        Assert.True(connection.LastOpenToken.CanBeCanceled);
        Assert.NotEqual(source.Token, connection.LastOpenToken);
    }
}
