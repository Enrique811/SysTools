using SysTools.Data.Connection;
using SysTools.Entities.Connection;
using SysTools.Firebird.Tests.TestDoubles;

namespace SysTools.Firebird.Tests.Data;

public sealed class FirebirdConnectionProbeFailureTests
{
    [Fact]
    public async Task Probe_maps_timeout_exception_and_disposes_connection_once()
    {
        var connection = new TrackingDbConnection
        {
            OpenBehavior = _ => throw new TimeoutException("sentinel raw timeout")
        };
        var probe = CreateProbe(connection);

        var status = await probe.ProbeAsync(FirebirdTestConfiguration.Valid());

        Assert.Equal(ConnectionTestStatus.Timeout, status);
        Assert.Equal(1, connection.DisposeCalls);
    }

    [Fact]
    public async Task Probe_maps_its_internal_deadline_to_timeout()
    {
        var connection = new TrackingDbConnection
        {
            OpenBehavior = token => Task.Delay(Timeout.InfiniteTimeSpan, token)
        };
        var probe = CreateProbe(
            connection,
            new FirebirdErrorClassifier(),
            TimeSpan.FromMilliseconds(20));

        var status = await probe.ProbeAsync(FirebirdTestConfiguration.Valid());

        Assert.Equal(ConnectionTestStatus.Timeout, status);
        Assert.Equal(1, connection.DisposeCalls);
    }

    [Theory]
    [InlineData(ConnectionTestStatus.AuthenticationFailed)]
    [InlineData(ConnectionTestStatus.ServerUnavailable)]
    [InlineData(ConnectionTestStatus.DatabaseUnavailable)]
    public async Task Probe_uses_classifier_for_recognized_provider_failures(
        ConnectionTestStatus classifiedStatus)
    {
        var connection = new TrackingDbConnection
        {
            OpenBehavior = _ => throw new InvalidOperationException("localized sentinel")
        };
        var probe = CreateProbe(
            connection,
            new StubFirebirdErrorClassifier(classifiedStatus),
            TimeSpan.FromSeconds(1));

        var status = await probe.ProbeAsync(FirebirdTestConfiguration.Valid());

        Assert.Equal(classifiedStatus, status);
        Assert.Equal(1, connection.DisposeCalls);
    }

    [Fact]
    public async Task Probe_maps_unknown_exception_to_unexpected_failure()
    {
        var connection = new TrackingDbConnection
        {
            OpenBehavior = _ => throw new InvalidOperationException("localized sentinel")
        };
        var probe = CreateProbe(connection);

        var status = await probe.ProbeAsync(FirebirdTestConfiguration.Valid());

        Assert.Equal(ConnectionTestStatus.UnexpectedFailure, status);
        Assert.Equal(1, connection.DisposeCalls);
    }

    private static FirebirdConnectionProbe CreateProbe(
        TrackingDbConnection connection,
        FirebirdErrorClassifier? classifier = null,
        TimeSpan? timeout = null) =>
        new(
            new FakeFirebirdConnectionFactory(_ => connection),
            new ListLogger<FirebirdConnectionProbe>(),
            classifier ?? new FirebirdErrorClassifier(),
            timeout ?? TimeSpan.FromSeconds(1));

    private sealed class StubFirebirdErrorClassifier(ConnectionTestStatus status)
        : FirebirdErrorClassifier
    {
        public override ConnectionTestStatus Classify(Exception exception) => status;
    }
}
