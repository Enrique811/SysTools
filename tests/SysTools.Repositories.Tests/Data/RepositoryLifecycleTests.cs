using System.Collections.Concurrent;
using System.Data.Common;
using SysTools.Business.Repositories;
using SysTools.Data.Repositories;
using SysTools.Repositories.Tests.TestDoubles;

namespace SysTools.Repositories.Tests.Data;

public sealed class RepositoryLifecycleTests
{
    [Fact]
    [Trait("Category", "RepositoryLifecycle")]
    public async Task One_hundred_mixed_operations_dispose_every_created_resource_once()
    {
        var connections = new ConcurrentBag<TrackingDbConnection>();
        var commands = new ConcurrentBag<TrackingDbCommand>();
        var created = 0;
        var factory = new FakeRepositoryConnectionFactory(() =>
        {
            var sequence = Interlocked.Increment(ref created);
            var command = new TrackingDbCommand();
            command.ReaderBehavior = (sequence % 3) switch
            {
                0 => _ => Task.FromResult<DbDataReader>(RepositoryRows.Products(
                    [sequence, sequence.ToString(), "Producto", "P", 1m, 1d]).CreateDataReader()),
                1 => _ => Task.FromResult<DbDataReader>(RepositoryRows.Products().CreateDataReader()),
                _ => _ => Task.FromException<DbDataReader>(new InvalidOperationException("controlled"))
            };
            var connection = new TrackingDbConnection { CommandFactory = () => command };
            commands.Add(command);
            connections.Add(connection);
            return connection;
        });
        var repository = new FirebirdProductRepository(
            factory,
            new RepositoryListLogger<FirebirdProductRepository>());

        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (attempt % 4 == 3)
            {
                using var canceled = new CancellationTokenSource();
                canceled.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                    repository.GetByBarcodeAsync(
                        RepositoryTestConfiguration.Valid(),
                        "1",
                        canceled.Token));
                continue;
            }

            try
            {
                await repository.GetByBarcodeAsync(
                    RepositoryTestConfiguration.Valid(),
                    "1");
            }
            catch (RepositoryAccessException)
            {
            }
        }

        Assert.Equal(75, factory.Calls);
        Assert.Equal(75, connections.Count);
        Assert.Equal(75, commands.Count);
        Assert.All(connections, connection => Assert.Equal(1, connection.DisposeCalls));
        Assert.All(commands, command => Assert.Equal(1, command.DisposeCalls));
    }

    [Fact]
    [Trait("Category", "RepositoryLifecycle")]
    public async Task Concurrent_requests_use_independent_connections_commands_and_results()
    {
        var queue = new ConcurrentQueue<TrackingDbConnection>(
        [
            CreateConnection(1, "Uno"),
            CreateConnection(2, "Dos")
        ]);
        var factory = new FakeRepositoryConnectionFactory(() =>
            queue.TryDequeue(out var connection)
                ? connection
                : throw new InvalidOperationException("No connection queued."));
        var repository = new FirebirdProductRepository(
            factory,
            new RepositoryListLogger<FirebirdProductRepository>());

        var results = await Task.WhenAll(
            repository.GetByBarcodeAsync(RepositoryTestConfiguration.Valid(), "1"),
            repository.GetByBarcodeAsync(RepositoryTestConfiguration.Valid(), "2"));

        Assert.Equal([1, 2], results.Select(product => product!.Id).Order().ToArray());
        Assert.Equal(2, factory.Calls);
    }

    private static TrackingDbConnection CreateConnection(int id, string description)
    {
        var command = new TrackingDbCommand
        {
            ReaderBehavior = _ => Task.FromResult<DbDataReader>(RepositoryRows.Products(
                [id, id.ToString(), description, "P", 1m, 1d]).CreateDataReader())
        };
        return new TrackingDbConnection { CommandFactory = () => command };
    }
}
