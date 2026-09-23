using System.Data.Common;
using SysTools.Data.Repositories;
using SysTools.Entities.Products;
using SysTools.Repositories.Tests.TestDoubles;

namespace SysTools.Repositories.Tests.Data;

public sealed class FirebirdProductRepositorySearchTests
{
    [Fact]
    public async Task SearchByDescriptionAsync_uses_prefix_parameter_and_stable_order()
    {
        var reader = new TrackingDbDataReader(RepositoryRows.Products(
            [2, "2", "Café americano", "P", 20m, 2d],
            [3, "3", "Café molido", "P", 30m, 3d]).CreateDataReader());
        var command = new TrackingDbCommand
        {
            ReaderBehavior = _ => Task.FromResult<DbDataReader>(reader)
        };
        var connection = new TrackingDbConnection { CommandFactory = () => command };
        var repository = new FirebirdProductRepository(
            new FakeRepositoryConnectionFactory(() => connection),
            new RepositoryListLogger<FirebirdProductRepository>());

        var results = await repository.SearchByDescriptionAsync(
            RepositoryTestConfiguration.Valid(),
            "  café  ");

        Assert.Collection(
            results,
            product => Assert.Equal("Café americano", product.Description),
            product => Assert.Equal("Café molido", product.Description));
        Assert.Contains(
            "UPPER(A.DESCRIPCION) LIKE UPPER(@descriptionPrefix)",
            command.CommandText,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "ORDER BY A.DESCRIPCION, A.ID",
            command.CommandText,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("café", command.CommandText, StringComparison.OrdinalIgnoreCase);
        var parameter = Assert.Single(command.ParametersSnapshot);
        Assert.Equal("@descriptionPrefix", parameter.ParameterName);
        Assert.Equal("café%", parameter.Value);
        Assert.Equal(256, parameter.Size);
        Assert.Equal(5, command.CommandTimeout);
        Assert.Equal(1, reader.DisposeCalls);
        Assert.Equal(1, command.DisposeCalls);
        Assert.Equal(1, connection.DisposeCalls);
    }

    [Fact]
    public async Task SearchByDescriptionAsync_returns_read_only_empty_collection_without_matches()
    {
        var command = new TrackingDbCommand
        {
            ReaderBehavior = _ => Task.FromResult<DbDataReader>(
                RepositoryRows.Products().CreateDataReader())
        };
        var connection = new TrackingDbConnection { CommandFactory = () => command };
        var repository = new FirebirdProductRepository(
            new FakeRepositoryConnectionFactory(() => connection),
            new RepositoryListLogger<FirebirdProductRepository>());

        var results = await repository.SearchByDescriptionAsync(
            RepositoryTestConfiguration.Valid(),
            "none");

        Assert.Empty(results);
        var mutableView = Assert.IsAssignableFrom<ICollection<Product>>(results);
        Assert.True(mutableView.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => mutableView.Add(
            new Product(1, "1", "x", null, null, null)));
    }
}
