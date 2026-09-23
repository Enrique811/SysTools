using SysTools.Data.Connection;
using SysTools.Entities.Connection;
using SysTools.Firebird.Tests.TestDoubles;

namespace SysTools.Firebird.Tests.Data;

public sealed class FirebirdConnectionConcurrencyTests
{
    [Fact]
    [Trait("Category", "Lifecycle")]
    public async Task Concurrent_requests_have_independent_results_and_connections()
    {
        var firstOpening = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var firstConnection = new TrackingDbConnection
        {
            OpenBehavior = async token =>
            {
                firstOpening.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            }
        };
        var secondConnection = new TrackingDbConnection();
        var connectionIndex = -1;
        var connections = new[] { firstConnection, secondConnection };
        var factory = new FakeFirebirdConnectionFactory(
            _ => connections[Interlocked.Increment(ref connectionIndex)]);
        var probe = new FirebirdConnectionProbe(
            factory,
            new ListLogger<FirebirdConnectionProbe>(),
            new FirebirdErrorClassifier(),
            TimeSpan.FromSeconds(1));
        using var firstCancellation = new CancellationTokenSource();

        var firstAttempt = probe.ProbeAsync(
            FirebirdTestConfiguration.Valid(),
            firstCancellation.Token);
        await firstOpening.Task;
        var secondAttempt = probe.ProbeAsync(FirebirdTestConfiguration.Valid());
        firstCancellation.Cancel();

        var results = await Task.WhenAll(firstAttempt, secondAttempt);

        Assert.Equal(
            [ConnectionTestStatus.Canceled, ConnectionTestStatus.Success],
            results);
        Assert.NotSame(firstConnection, secondConnection);
        Assert.Equal(1, firstConnection.DisposeCalls);
        Assert.Equal(1, secondConnection.DisposeCalls);
    }
}
