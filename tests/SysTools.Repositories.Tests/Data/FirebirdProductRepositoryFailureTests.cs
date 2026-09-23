using System.Data.Common;
using SysTools.Business.Repositories;
using SysTools.Data.Repositories;
using SysTools.Repositories.Tests.TestDoubles;

namespace SysTools.Repositories.Tests.Data;

public sealed class FirebirdProductRepositoryFailureTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByBarcodeAsync_rejects_missing_barcode_before_creating_connection(string? barcode)
    {
        var factory = new FakeRepositoryConnectionFactory(() => new TrackingDbConnection());
        var repository = new FirebirdProductRepository(
            factory,
            new RepositoryListLogger<FirebirdProductRepository>());

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            repository.GetByBarcodeAsync(RepositoryTestConfiguration.Valid(), barcode!));

        Assert.Equal(0, factory.Calls);
    }

    [Fact]
    public async Task GetByBarcodeAsync_rejects_barcode_longer_than_fifty_before_connection()
    {
        var factory = new FakeRepositoryConnectionFactory(() => new TrackingDbConnection());
        var repository = new FirebirdProductRepository(
            factory,
            new RepositoryListLogger<FirebirdProductRepository>());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            repository.GetByBarcodeAsync(RepositoryTestConfiguration.Valid(), new string('1', 51)));

        Assert.Equal(0, factory.Calls);
    }

    [Fact]
    public async Task GetByBarcodeAsync_preserves_consumer_cancellation_during_open()
    {
        using var source = new CancellationTokenSource();
        var connection = new TrackingDbConnection
        {
            OpenBehavior = token =>
            {
                source.Cancel();
                return Task.FromCanceled(token);
            }
        };
        var repository = Create(connection);

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repository.GetByBarcodeAsync(RepositoryTestConfiguration.Valid(), "1", source.Token));

        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.Equal(1, connection.DisposeCalls);
    }

    [Fact]
    public async Task GetByBarcodeAsync_preserves_consumer_cancellation_during_execution()
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
        var repository = Create(connection);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repository.GetByBarcodeAsync(RepositoryTestConfiguration.Valid(), "1", source.Token));

        Assert.Equal(1, command.DisposeCalls);
        Assert.Equal(1, connection.DisposeCalls);
    }

    [Fact]
    public async Task GetByBarcodeAsync_preserves_consumer_cancellation_during_read()
    {
        using var source = new CancellationTokenSource();
        var reader = new TrackingDbDataReader(RepositoryRows.Products(
            [1, "1", "Producto", "P", 1m, 1d]).CreateDataReader())
        {
            ReadBehavior = token =>
            {
                source.Cancel();
                return Task.FromCanceled<bool>(token);
            }
        };
        var command = new TrackingDbCommand
        {
            ReaderBehavior = _ => Task.FromResult<DbDataReader>(reader)
        };
        var connection = new TrackingDbConnection { CommandFactory = () => command };
        var repository = Create(connection);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repository.GetByBarcodeAsync(RepositoryTestConfiguration.Valid(), "1", source.Token));

        Assert.Equal(1, reader.DisposeCalls);
        Assert.Equal(1, command.DisposeCalls);
        Assert.Equal(1, connection.DisposeCalls);
    }

    [Fact]
    public async Task GetByBarcodeAsync_translates_failure_without_exposing_source_exception()
    {
        const string sentinel = "Systools-Repo-Secret-Provider-Message";
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
            repository.GetByBarcodeAsync(RepositoryTestConfiguration.Valid(), "1"));

        Assert.Equal(RepositoryOperation.GetProductByBarcode, exception.Operation);
        Assert.Null(exception.InnerException);
        Assert.DoesNotContain(sentinel, exception.ToString(), StringComparison.Ordinal);
        var log = Assert.Single(logger.Entries);
        Assert.Null(log.Exception);
        Assert.DoesNotContain(sentinel, log.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(InvalidOperationException), log.Message, StringComparison.Ordinal);
        Assert.Equal(1, command.DisposeCalls);
        Assert.Equal(1, connection.DisposeCalls);
    }

    private static FirebirdProductRepository Create(TrackingDbConnection connection) =>
        new(
            new FakeRepositoryConnectionFactory(() => connection),
            new RepositoryListLogger<FirebirdProductRepository>());
}
