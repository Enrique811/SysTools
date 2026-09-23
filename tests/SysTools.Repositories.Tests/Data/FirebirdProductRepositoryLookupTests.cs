using SysTools.Data.Repositories;
using SysTools.Repositories.Tests.TestDoubles;

namespace SysTools.Repositories.Tests.Data;

public sealed class FirebirdProductRepositoryLookupTests
{
    [Fact]
    public async Task GetByBarcodeAsync_uses_parameterized_read_only_query_and_maps_product()
    {
        var reader = new TrackingDbDataReader(RepositoryRows.Products(
            [31, "000031", "Café molido", "P", 89.50m, 4d]).CreateDataReader());
        var command = new TrackingDbCommand
        {
            ReaderBehavior = _ => Task.FromResult<System.Data.Common.DbDataReader>(reader)
        };
        var connection = new TrackingDbConnection { CommandFactory = () => command };
        var factory = new FakeRepositoryConnectionFactory(() => connection);
        var logger = new RepositoryListLogger<FirebirdProductRepository>();
        var repository = new FirebirdProductRepository(factory, logger);
        var configuration = RepositoryTestConfiguration.Valid();

        var result = await repository.GetByBarcodeAsync(configuration, "000031");

        Assert.NotNull(result);
        Assert.Equal(31, result.Id);
        Assert.Equal("000031", result.Barcode);
        Assert.Contains("SELECT", command.CommandText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LEFT JOIN INVENTARIO_BALANCES", command.CommandText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHERE A.CODIGO = @barcode", command.CommandText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("INSERT", command.CommandText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE", command.CommandText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE", command.CommandText, StringComparison.OrdinalIgnoreCase);
        var parameter = Assert.Single(command.ParametersSnapshot);
        Assert.Equal("@barcode", parameter.ParameterName);
        Assert.Equal("000031", parameter.Value);
        Assert.Equal(50, parameter.Size);
        Assert.Equal(5, command.CommandTimeout);
        Assert.Same(configuration, Assert.Single(factory.Configurations));
        Assert.Equal(1, connection.OpenCalls);
        Assert.Equal(1, command.ReaderCalls);
        Assert.Equal(1, reader.DisposeCalls);
        Assert.Equal(1, command.DisposeCalls);
        Assert.Equal(1, connection.DisposeCalls);
    }

    [Fact]
    public async Task GetByBarcodeAsync_returns_null_when_no_row_exists()
    {
        var command = new TrackingDbCommand
        {
            ReaderBehavior = _ => Task.FromResult<System.Data.Common.DbDataReader>(
                RepositoryRows.Products().CreateDataReader())
        };
        var connection = new TrackingDbConnection { CommandFactory = () => command };
        var repository = new FirebirdProductRepository(
            new FakeRepositoryConnectionFactory(() => connection),
            new RepositoryListLogger<FirebirdProductRepository>());

        var result = await repository.GetByBarcodeAsync(
            RepositoryTestConfiguration.Valid(),
            "missing");

        Assert.Null(result);
        Assert.Equal(1, connection.DisposeCalls);
        Assert.Equal(1, command.DisposeCalls);
    }
}
