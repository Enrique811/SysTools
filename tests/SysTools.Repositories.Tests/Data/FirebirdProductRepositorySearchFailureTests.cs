using System.Data.Common;
using SysTools.Business.Repositories;
using SysTools.Data.Repositories;
using SysTools.Repositories.Tests.TestDoubles;

namespace SysTools.Repositories.Tests.Data;

public sealed class FirebirdProductRepositorySearchFailureTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchByDescriptionAsync_returns_empty_without_connection_for_blank_prefix(
        string? prefix)
    {
        var factory = new FakeRepositoryConnectionFactory(() => new TrackingDbConnection());
        var repository = new FirebirdProductRepository(
            factory,
            new RepositoryListLogger<FirebirdProductRepository>());

        var results = await repository.SearchByDescriptionAsync(
            RepositoryTestConfiguration.Valid(),
            prefix);

        Assert.Empty(results);
        Assert.Equal(0, factory.Calls);
    }

    [Fact]
    public async Task SearchByDescriptionAsync_preserves_cancellation_and_disposes_resources()
    {
        using var source = new CancellationTokenSource();
        var command = new TrackingDbCommand
        {
            ReaderBehavior = token =>
            {
                source.Cancel();
                return Task.FromCanceled<DbDataReader>(token);
            }
        };
        var connection = new TrackingDbConnection { CommandFactory = () => command };
        var repository = new FirebirdProductRepository(
            new FakeRepositoryConnectionFactory(() => connection),
            new RepositoryListLogger<FirebirdProductRepository>());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repository.SearchByDescriptionAsync(
                RepositoryTestConfiguration.Valid(),
                "Prod",
                source.Token));

        Assert.Equal(1, command.DisposeCalls);
        Assert.Equal(1, connection.DisposeCalls);
    }

    [Fact]
    public async Task SearchByDescriptionAsync_translates_failure_instead_of_returning_empty()
    {
        const string sentinel = "Systools-Repo-Description-ProviderFailure";
        var command = new TrackingDbCommand
        {
            ReaderBehavior = _ => Task.FromException<DbDataReader>(new InvalidOperationException(sentinel))
        };
        var connection = new TrackingDbConnection { CommandFactory = () => command };
        var logger = new RepositoryListLogger<FirebirdProductRepository>();
        var repository = new FirebirdProductRepository(
            new FakeRepositoryConnectionFactory(() => connection),
            logger);

        var exception = await Assert.ThrowsAsync<RepositoryAccessException>(() =>
            repository.SearchByDescriptionAsync(
                RepositoryTestConfiguration.Valid(),
                "Systools-Repo-Description-Input"));

        Assert.Equal(RepositoryOperation.SearchProductsByDescription, exception.Operation);
        Assert.DoesNotContain(sentinel, exception.ToString(), StringComparison.Ordinal);
        var log = Assert.Single(logger.Entries);
        Assert.Null(log.Exception);
        Assert.DoesNotContain(sentinel, log.Message, StringComparison.Ordinal);
        Assert.Equal(1, command.DisposeCalls);
        Assert.Equal(1, connection.DisposeCalls);
    }
}
