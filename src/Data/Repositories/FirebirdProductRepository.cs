using System.Data;
using Microsoft.Extensions.Logging;
using SysTools.Business.Repositories;
using SysTools.Data.Connection;
using SysTools.Entities.Configuration;
using SysTools.Entities.Products;

namespace SysTools.Data.Repositories;

public sealed class FirebirdProductRepository : IProductRepository
{
    private const int CommandTimeoutSeconds = 5;
    private const string ProductSelect = """
        SELECT
            A.ID AS CODIGO,
            A.CODIGO AS CODIGO_BARRAS,
            A.DESCRIPCION AS DESCRIPCION,
            A.TVENTA AS PRESENTACION,
            A.PFINAL AS PRECIO_IVA,
            B.CANTIDAD_ACTUAL AS STOCK
        FROM PRODUCTOS A
        LEFT JOIN INVENTARIO_BALANCES B ON A.ID = B.PRODUCTO_ID
        """;

    private readonly IFirebirdConnectionFactory _connectionFactory;
    private readonly ILogger<FirebirdProductRepository> _logger;

    public FirebirdProductRepository(
        IFirebirdConnectionFactory connectionFactory,
        ILogger<FirebirdProductRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<Product?> GetByBarcodeAsync(
        AppConfiguration configuration,
        string barcode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ValidateBarcode(barcode);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await using var connection = _connectionFactory.Create(configuration);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = $"{ProductSelect} WHERE A.CODIGO = @barcode";
            command.CommandType = CommandType.Text;
            command.CommandTimeout = CommandTimeoutSeconds;
            AddTextParameter(command, "@barcode", barcode, 50);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);
            return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
                ? ProductDataReaderMapper.Map(reader)
                : null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw FirebirdRepositoryDiagnostics.Translate(
                _logger,
                RepositoryOperation.GetProductByBarcode,
                exception);
        }
    }

    public async Task<IReadOnlyList<Product>> SearchByDescriptionAsync(
        AppConfiguration configuration,
        string? descriptionPrefix,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (string.IsNullOrWhiteSpace(descriptionPrefix))
        {
            return Array.Empty<Product>();
        }

        cancellationToken.ThrowIfCancellationRequested();
        var normalizedPrefix = descriptionPrefix.Trim();

        try
        {
            await using var connection = _connectionFactory.Create(configuration);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = $"""
                {ProductSelect}
                WHERE UPPER(A.DESCRIPCION) LIKE UPPER(@descriptionPrefix)
                ORDER BY A.DESCRIPCION, A.ID
                """;
            command.CommandType = CommandType.Text;
            command.CommandTimeout = CommandTimeoutSeconds;
            AddTextParameter(
                command,
                "@descriptionPrefix",
                $"{normalizedPrefix}%",
                256);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);
            var products = new List<Product>();
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                products.Add(ProductDataReaderMapper.Map(reader));
            }

            return Array.AsReadOnly(products.ToArray());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw FirebirdRepositoryDiagnostics.Translate(
                _logger,
                RepositoryOperation.SearchProductsByDescription,
                exception);
        }
    }

    private static void ValidateBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            throw new ArgumentException("El codigo de barras es obligatorio.", nameof(barcode));
        }

        if (barcode.Length > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(barcode));
        }
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
}
