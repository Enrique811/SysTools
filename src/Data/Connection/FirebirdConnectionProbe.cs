using Microsoft.Extensions.Logging;
using SysTools.Business.Connection;
using SysTools.Entities.Configuration;
using SysTools.Entities.Connection;

namespace SysTools.Data.Connection;

public sealed class FirebirdConnectionProbe : IFirebirdConnectionProbe
{
    private readonly IFirebirdConnectionFactory _connectionFactory;
    private readonly ILogger<FirebirdConnectionProbe> _logger;
    private readonly FirebirdErrorClassifier _errorClassifier;
    private readonly TimeSpan _timeout;

    public FirebirdConnectionProbe(
        IFirebirdConnectionFactory connectionFactory,
        ILogger<FirebirdConnectionProbe> logger)
        : this(
            connectionFactory,
            logger,
            new FirebirdErrorClassifier(),
            TimeSpan.FromSeconds(FirebirdConnectionDefaults.ConnectionTimeoutSeconds))
    {
    }

    public FirebirdConnectionProbe(
        IFirebirdConnectionFactory connectionFactory,
        ILogger<FirebirdConnectionProbe> logger,
        TimeSpan timeout)
        : this(connectionFactory, logger, new FirebirdErrorClassifier(), timeout)
    {
    }

    public FirebirdConnectionProbe(
        IFirebirdConnectionFactory connectionFactory,
        ILogger<FirebirdConnectionProbe> logger,
        FirebirdErrorClassifier errorClassifier,
        TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(errorClassifier);
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        _connectionFactory = connectionFactory;
        _logger = logger;
        _errorClassifier = errorClassifier;
        _timeout = timeout;
    }

    public async Task<ConnectionTestStatus> ProbeAsync(
        AppConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        using var deadlineSource = new CancellationTokenSource(_timeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            deadlineSource.Token);

        try
        {
            await using var connection = _connectionFactory.Create(configuration);
            await connection.OpenAsync(linkedSource.Token).ConfigureAwait(false);
            return ConnectionTestStatus.Success;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LogFailure(ConnectionTestStatus.Canceled, nameof(OperationCanceledException), []);
            return ConnectionTestStatus.Canceled;
        }
        catch (OperationCanceledException) when (deadlineSource.IsCancellationRequested)
        {
            LogFailure(ConnectionTestStatus.Timeout, nameof(OperationCanceledException), []);
            return ConnectionTestStatus.Timeout;
        }
        catch (TimeoutException) when (cancellationToken.IsCancellationRequested)
        {
            LogFailure(ConnectionTestStatus.Canceled, nameof(TimeoutException), []);
            return ConnectionTestStatus.Canceled;
        }
        catch (TimeoutException)
        {
            LogFailure(ConnectionTestStatus.Timeout, nameof(TimeoutException), []);
            return ConnectionTestStatus.Timeout;
        }
        catch (Exception exception) when (cancellationToken.IsCancellationRequested)
        {
            LogFailure(ConnectionTestStatus.Canceled, exception.GetType().Name, []);
            return ConnectionTestStatus.Canceled;
        }
        catch (Exception exception)
        {
            var status = _errorClassifier.Classify(exception);
            LogFailure(status, exception.GetType().Name, _errorClassifier.GetErrorCodes(exception));
            return status;
        }
    }

    private void LogFailure(
        ConnectionTestStatus status,
        string exceptionType,
        IReadOnlyList<int> errorCodes) =>
        _logger.LogWarning(
            "FirebirdConnectionFailed Operation={Operation} Outcome={Outcome} ExceptionType={ExceptionType} ErrorCodes={ErrorCodes}",
            nameof(ProbeAsync),
            status,
            exceptionType,
            errorCodes);
}
