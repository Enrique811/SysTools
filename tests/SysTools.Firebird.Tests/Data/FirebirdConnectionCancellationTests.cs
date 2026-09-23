using SysTools.Data.Connection;
using SysTools.Entities.Connection;
using SysTools.Firebird.Tests.TestDoubles;

namespace SysTools.Firebird.Tests.Data;

public sealed class FirebirdConnectionCancellationTests
{
    [Fact]
    [Trait("Category", "Lifecycle")]
    public async Task Probe_returns_canceled_for_pre_canceled_consumer_and_disposes_once()
    {
        var connection = new TrackingDbConnection
        {
            OpenBehavior = token => Task.FromCanceled(token)
        };
        var probe = CreateProbe(connection, TimeSpan.FromSeconds(1));
        using var source = new CancellationTokenSource();
        source.Cancel();

        var status = await probe.ProbeAsync(FirebirdTestConfiguration.Valid(), source.Token);

        Assert.Equal(ConnectionTestStatus.Canceled, status);
        Assert.Equal(1, connection.DisposeCalls);
    }

    [Fact]
    [Trait("Category", "Lifecycle")]
    public async Task Probe_returns_canceled_when_consumer_cancels_during_open()
    {
        var opening = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var connection = new TrackingDbConnection
        {
            OpenBehavior = async token =>
            {
                opening.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            }
        };
        var probe = CreateProbe(connection, TimeSpan.FromSeconds(1));
        using var source = new CancellationTokenSource();

        var pending = probe.ProbeAsync(FirebirdTestConfiguration.Valid(), source.Token);
        await opening.Task;
        source.Cancel();
        var status = await pending;

        Assert.Equal(ConnectionTestStatus.Canceled, status);
        Assert.Equal(1, connection.DisposeCalls);
    }

    [Fact]
    [Trait("Category", "Lifecycle")]
    public async Task Consumer_cancellation_wins_when_deadline_is_also_signaled()
    {
        using var source = new CancellationTokenSource();
        var connection = new TrackingDbConnection
        {
            OpenBehavior = async token =>
            {
                await Task.Delay(40);
                source.Cancel();
                token.ThrowIfCancellationRequested();
            }
        };
        var probe = CreateProbe(connection, TimeSpan.FromMilliseconds(10));

        var status = await probe.ProbeAsync(FirebirdTestConfiguration.Valid(), source.Token);

        Assert.Equal(ConnectionTestStatus.Canceled, status);
        Assert.Equal(1, connection.DisposeCalls);
    }

    [Fact]
    [Trait("Category", "Lifecycle")]
    public async Task Consumer_cancellation_wins_over_provider_timeout_exception()
    {
        using var source = new CancellationTokenSource();
        var connection = new TrackingDbConnection
        {
            OpenBehavior = _ =>
            {
                source.Cancel();
                throw new TimeoutException("raw timeout sentinel");
            }
        };
        var probe = CreateProbe(connection, TimeSpan.FromSeconds(1));

        var status = await probe.ProbeAsync(FirebirdTestConfiguration.Valid(), source.Token);

        Assert.Equal(ConnectionTestStatus.Canceled, status);
        Assert.Equal(1, connection.DisposeCalls);
    }

    private static FirebirdConnectionProbe CreateProbe(
        TrackingDbConnection connection,
        TimeSpan timeout) =>
        new(
            new FakeFirebirdConnectionFactory(_ => connection),
            new ListLogger<FirebirdConnectionProbe>(),
            new FirebirdErrorClassifier(),
            timeout);
}
