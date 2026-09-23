using System.Collections.Concurrent;
using SysTools.Data.Connection;
using SysTools.Entities.Connection;
using SysTools.Firebird.Tests.TestDoubles;

namespace SysTools.Firebird.Tests.Data;

public sealed class FirebirdConnectionLifecycleTests
{
    [Fact]
    [Trait("Category", "Lifecycle")]
    public async Task One_hundred_mixed_attempts_dispose_every_created_connection_once()
    {
        var connections = new ConcurrentBag<TrackingDbConnection>();
        var attemptIndex = -1;
        var factory = new FakeFirebirdConnectionFactory(_ =>
        {
            var index = Interlocked.Increment(ref attemptIndex);
            var connection = new TrackingDbConnection
            {
                OpenBehavior = (index % 3) switch
                {
                    0 => _ => Task.CompletedTask,
                    1 => _ => throw new InvalidOperationException("raw failure sentinel"),
                    _ => token => Task.FromCanceled(token)
                }
            };
            connections.Add(connection);
            return connection;
        });
        var probe = new FirebirdConnectionProbe(
            factory,
            new ListLogger<FirebirdConnectionProbe>(),
            new FirebirdErrorClassifier(),
            TimeSpan.FromSeconds(1));
        var results = new List<ConnectionTestStatus>(capacity: 100);

        for (var attempt = 0; attempt < 100; attempt++)
        {
            using var source = new CancellationTokenSource();
            if (attempt % 3 == 2)
            {
                source.Cancel();
            }

            results.Add(await probe.ProbeAsync(
                FirebirdTestConfiguration.Valid(),
                source.Token));
        }

        Assert.Equal(100, factory.Calls);
        Assert.Equal(100, connections.Count);
        Assert.Contains(ConnectionTestStatus.Success, results);
        Assert.Contains(ConnectionTestStatus.UnexpectedFailure, results);
        Assert.Contains(ConnectionTestStatus.Canceled, results);
        Assert.All(connections, connection => Assert.Equal(1, connection.DisposeCalls));
    }
}
