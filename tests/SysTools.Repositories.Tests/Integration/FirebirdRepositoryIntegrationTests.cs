using System.Data;
using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Data.Connection;
using SysTools.Data.Repositories;
using SysTools.Entities.Configuration;

namespace SysTools.Repositories.Tests.Integration;

public sealed class FirebirdRepositoryIntegrationTests
{
    [RepositoryIntegrationFact]
    [Trait("Category", "RepositoryIntegration")]
    public async Task Exact_product_query_succeeds_ten_consecutive_times()
    {
        Assert.True(RepositoryIntegrationFactAttribute.TryGetEnvironment(
            out var configuration,
            out var productCode,
            out _));
        if (string.IsNullOrWhiteSpace(productCode))
        {
            productCode = await RunReferenceSafelyAsync(
                () => GetAnyProductCodeAsync(configuration));
        }
        var repository = new FirebirdProductRepository(
            new FirebirdConnectionFactory(),
            NullLogger<FirebirdProductRepository>.Instance);

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var reference = await RunReferenceSafelyAsync(
                () => GetReferenceProductAsync(configuration, productCode));
            var product = await repository.GetByBarcodeAsync(configuration, productCode);

            Assert.NotNull(reference);
            Assert.NotNull(product);
            Assert.Equal(reference.Id, product.Id);
            Assert.Equal(reference.Barcode, product.Barcode);
            Assert.Equal(reference.Description, product.Description);
            Assert.Equal(reference.Presentation, product.Presentation);
            Assert.Equal(reference.PriceWithTax, product.PriceWithTax);
            Assert.Equal(reference.Stock, product.Stock);
        }
    }

    [RepositoryIntegrationFact]
    [Trait("Category", "RepositoryIntegration")]
    public async Task Prefix_query_returns_matching_products()
    {
        Assert.True(RepositoryIntegrationFactAttribute.TryGetEnvironment(
            out var configuration,
            out _,
            out var productPrefix));
        if (string.IsNullOrWhiteSpace(productPrefix))
        {
            productPrefix = await RunReferenceSafelyAsync(
                () => GetAnyProductDescriptionAsync(configuration));
        }
        var repository = new FirebirdProductRepository(
            new FirebirdConnectionFactory(),
            NullLogger<FirebirdProductRepository>.Instance);

        var referenceIds = await RunReferenceSafelyAsync(
            () => GetReferenceProductIdsAsync(configuration, productPrefix));
        var products = await repository.SearchByDescriptionAsync(configuration, productPrefix);

        Assert.NotEmpty(products);
        Assert.Equal(referenceIds, products.Select(product => product.Id).ToArray());
        Assert.All(
            products,
            product => Assert.StartsWith(
                productPrefix,
                product.Description,
                StringComparison.CurrentCultureIgnoreCase));
    }

    [RepositoryIntegrationFact]
    [Trait("Category", "RepositoryIntegration")]
    public async Task Server_clock_returns_ten_plausible_values()
    {
        Assert.True(RepositoryIntegrationFactAttribute.TryGetEnvironment(
            out var configuration,
            out _,
            out _));
        var repository = new FirebirdServerClockRepository(
            new FirebirdConnectionFactory(),
            NullLogger<FirebirdServerClockRepository>.Instance);

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var reference = await RunReferenceSafelyAsync(
                () => GetReferenceTimestampAsync(configuration));
            var timestamp = await repository.GetCurrentAsync(configuration);
            Assert.InRange(timestamp.Year, 2000, 2100);
            Assert.InRange((timestamp - reference).Duration(), TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }
    }

    private static async Task<ReferenceProduct?> GetReferenceProductAsync(
        AppConfiguration configuration,
        string barcode)
    {
        await using var connection = new FirebirdConnectionFactory().Create(configuration);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT A.ID, A.CODIGO, A.DESCRIPCION, A.TVENTA, A.PFINAL, B.CANTIDAD_ACTUAL
            FROM PRODUCTOS A
            LEFT JOIN INVENTARIO_BALANCES B ON A.ID = B.PRODUCTO_ID
            WHERE A.CODIGO = @barcode
            """;
        command.CommandTimeout = 5;
        AddTextParameter(command, "@barcode", barcode, 50);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new ReferenceProduct(
            Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture),
            Convert.ToString(reader.GetValue(1), CultureInfo.InvariantCulture) ?? string.Empty,
            reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.IsDBNull(4) ? null : Convert.ToDecimal(reader.GetValue(4), CultureInfo.InvariantCulture),
            reader.IsDBNull(5)
                ? "Sin registro"
                : Convert.ToString(reader.GetValue(5), CultureInfo.InvariantCulture) ?? "Sin registro");
    }

    private static async Task<string> GetAnyProductCodeAsync(
        AppConfiguration configuration)
    {
        await using var connection = new FirebirdConnectionFactory().Create(configuration);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT FIRST 1 A.CODIGO FROM PRODUCTOS A ORDER BY A.ID";
        command.CommandTimeout = 5;
        var value = await command.ExecuteScalarAsync();
        Assert.NotNull(value);
        Assert.NotEqual(DBNull.Value, value);
        var code = Convert.ToString(value, CultureInfo.InvariantCulture);
        Assert.False(string.IsNullOrWhiteSpace(code));
        return code;
    }

    private static async Task<string> GetAnyProductDescriptionAsync(
        AppConfiguration configuration)
    {
        await using var connection = new FirebirdConnectionFactory().Create(configuration);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT FIRST 1 A.DESCRIPCION
            FROM PRODUCTOS A
            WHERE A.DESCRIPCION IS NOT NULL
            ORDER BY A.ID
            """;
        command.CommandTimeout = 5;
        var value = await command.ExecuteScalarAsync();
        Assert.NotNull(value);
        Assert.NotEqual(DBNull.Value, value);
        var description = Convert.ToString(value, CultureInfo.InvariantCulture);
        Assert.False(string.IsNullOrWhiteSpace(description));
        return description;
    }

    private static async Task<int[]> GetReferenceProductIdsAsync(
        AppConfiguration configuration,
        string prefix)
    {
        await using var connection = new FirebirdConnectionFactory().Create(configuration);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT A.ID
            FROM PRODUCTOS A
            WHERE UPPER(A.DESCRIPCION) LIKE UPPER(@descriptionPrefix)
            ORDER BY A.DESCRIPCION, A.ID
            """;
        command.CommandTimeout = 5;
        AddTextParameter(command, "@descriptionPrefix", $"{prefix.Trim()}%", 256);
        await using var reader = await command.ExecuteReaderAsync();
        var ids = new List<int>();
        while (await reader.ReadAsync())
        {
            ids.Add(Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture));
        }

        return ids.ToArray();
    }

    private static async Task<DateTime> GetReferenceTimestampAsync(
        AppConfiguration configuration)
    {
        await using var connection = new FirebirdConnectionFactory().Create(configuration);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT CURRENT_TIMESTAMP FROM RDB$DATABASE";
        command.CommandTimeout = 5;
        var value = await command.ExecuteScalarAsync();
        Assert.NotNull(value);
        Assert.NotEqual(DBNull.Value, value);
        return Convert.ToDateTime(value, CultureInfo.InvariantCulture);
    }

    private static void AddTextParameter(
        System.Data.Common.DbCommand command,
        string name,
        string value,
        int size)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = DbType.String;
        parameter.Size = size;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static async Task<T> RunReferenceSafelyAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch
        {
            throw new InvalidOperationException(
                "La consulta Firebird de referencia autorizada no pudo completarse.");
        }
    }

    private sealed record ReferenceProduct(
        int Id,
        string Barcode,
        string Description,
        string? Presentation,
        decimal? PriceWithTax,
        string Stock);
}
