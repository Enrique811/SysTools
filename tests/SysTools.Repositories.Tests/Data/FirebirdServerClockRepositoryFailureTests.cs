using SysTools.Business.Repositories;
using SysTools.Data.Repositories;
using SysTools.Repositories.Tests.TestDoubles;

namespace SysTools.Repositories.Tests.Data;

public sealed class FirebirdServerClockRepositoryFailureTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(typeof(DBNull))]
    public async Task GetCurrentAsync_rejects_missing_server_value(object? marker)
    {
        var value = marker is Type ? DBNull.Value : null;
        var command = new TrackingDbCommand
        {
            ScalarBehavior = _ => Task.FromResult<object?>(value)
        };
        var connection = new TrackingDbConnection { CommandFactory = () => command };
        var repository = Create(connection);

        var exception = await Assert.ThrowsAsync<RepositoryAccessException>(() =>
            repository.GetCurrentAsync(RepositoryTestConfiguration.Valid()));

        Assert.Equal(RepositoryOperation.GetServerTimestamp, exception.Operation);
        Assert.Equal(1, command.DisposeCalls);
        Assert.Equal(1, connection.DisposeCalls);
    }

    [Fact]
    public async Task GetCurrentAsync_preserves_consumer_cancellation()
    {
        using var source = new CancellationTokenSource();
        var command = new TrackingDbCommand
        {
            ScalarBehavior = token =>
            {
                source.Cancel();
                return Task.FromCanceled<object?>(token);
            }
        };
        var connection = new TrackingDbConnection { CommandFactory = () => command };
        var repository = Create(connection);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repository.GetCurrentAsync(RepositoryTestConfiguration.Valid(), source.Token));

        Assert.Equal(1, command.DisposeCalls);
        Assert.Equal(1, connection.DisposeCalls);
    }

    [Fact]
    public async Task GetCurrentAsync_translates_provider_failure_safely()
    {
        const string sentinel = "Systools-Repo-Secret-ClockFailure";
        var command = new TrackingDbCommand
        {
            ScalarBehavior = _ => Task.FromException<object?>(new InvalidOperationException(sentinel))
        };
        var connection = new TrackingDbConnection { CommandFactory = () => command };
        var logger = new RepositoryListLogger<FirebirdServerClockRepository>();
        var repository = new FirebirdServerClockRepository(
            new FakeRepositoryConnectionFactory(() => connection),
            logger);

        var exception = await Assert.ThrowsAsync<RepositoryAccessException>(() =>
            repository.GetCurrentAsync(RepositoryTestConfiguration.Valid()));

        Assert.Equal(RepositoryOperation.GetServerTimestamp, exception.Operation);
        Assert.Null(exception.InnerException);
        Assert.DoesNotContain(sentinel, exception.ToString(), StringComparison.Ordinal);
        var log = Assert.Single(logger.Entries);
        Assert.Null(log.Exception);
        Assert.DoesNotContain(sentinel, log.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(InvalidOperationException), log.Message, StringComparison.Ordinal);
    }

    private static FirebirdServerClockRepository Create(TrackingDbConnection connection) =>
        new(
            new FakeRepositoryConnectionFactory(() => connection),
            new RepositoryListLogger<FirebirdServerClockRepository>());
}
