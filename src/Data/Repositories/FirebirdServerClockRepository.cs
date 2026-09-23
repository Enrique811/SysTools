using System.Data;
using System.Globalization;
using Microsoft.Extensions.Logging;
using SysTools.Business.Repositories;
using SysTools.Data.Connection;
using SysTools.Entities.Configuration;

namespace SysTools.Data.Repositories;

public sealed class FirebirdServerClockRepository : IServerClockRepository
{
    private const int CommandTimeoutSeconds = 5;
    private const string ServerTimestampSql =
        "SELECT CURRENT_TIMESTAMP AS HORAFECHA FROM RDB$DATABASE";

    private readonly IFirebirdConnectionFactory _connectionFactory;
    private readonly ILogger<FirebirdServerClockRepository> _logger;

    public FirebirdServerClockRepository(
        IFirebirdConnectionFactory connectionFactory,
        ILogger<FirebirdServerClockRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<DateTime> GetCurrentAsync(
        AppConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await using var connection = _connectionFactory.Create(configuration);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = ServerTimestampSql;
            command.CommandType = CommandType.Text;
            command.CommandTimeout = CommandTimeoutSeconds;

            var value = await command.ExecuteScalarAsync(cancellationToken)
                .ConfigureAwait(false);
            if (value is null or DBNull)
            {
                throw new InvalidDataException(
                    "Firebird no devolvio un timestamp de servidor.");
            }

            return Convert.ToDateTime(value, CultureInfo.InvariantCulture);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw FirebirdRepositoryDiagnostics.Translate(
                _logger,
                RepositoryOperation.GetServerTimestamp,
                exception);
        }
    }
}
