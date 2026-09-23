using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SysTools.Entities.Configuration;
using SysTools.Entities.Connection;

namespace SysTools.Business.Connection;

public sealed class ConnectionTestService : IConnectionTestService
{
    private readonly IFirebirdConnectionProbe _probe;
    private readonly ConnectionConfigurationValidator _validator;
    private readonly ILogger<ConnectionTestService> _logger;

    public ConnectionTestService(
        IFirebirdConnectionProbe probe,
        ILogger<ConnectionTestService> logger)
        : this(probe, new ConnectionConfigurationValidator(), logger)
    {
    }

    public ConnectionTestService(
        IFirebirdConnectionProbe probe,
        ConnectionConfigurationValidator validator,
        ILogger<ConnectionTestService> logger)
    {
        ArgumentNullException.ThrowIfNull(probe);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(logger);
        _probe = probe;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ConnectionTestResult> TestAsync(
        AppConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var startedAt = Stopwatch.GetTimestamp();
        _logger.LogInformation(
            "ConnectionTestStarted Operation={Operation}",
            nameof(TestAsync));

        var issues = _validator.Validate(configuration);
        if (issues.Count != 0)
        {
            var validationDuration = Stopwatch.GetElapsedTime(startedAt);
            _logger.LogWarning(
                "ConnectionValidationFailed Operation={Operation} Outcome={Outcome} IssueFields={IssueFields} IssueCount={IssueCount} DurationMs={DurationMs}",
                nameof(TestAsync),
                ConnectionTestStatus.InvalidConfiguration,
                issues.Select(issue => issue.Field).ToArray(),
                issues.Count,
                validationDuration.TotalMilliseconds);
            return new ConnectionTestResult(
                ConnectionTestStatus.InvalidConfiguration,
                MessageFor(ConnectionTestStatus.InvalidConfiguration),
                validationDuration,
                issues);
        }

        var status = await _probe.ProbeAsync(configuration, cancellationToken)
            .ConfigureAwait(false);
        var duration = Stopwatch.GetElapsedTime(startedAt);

        _logger.LogInformation(
            "ConnectionTestCompleted Operation={Operation} Outcome={Outcome} DurationMs={DurationMs}",
            nameof(TestAsync),
            status,
            duration.TotalMilliseconds);

        return new ConnectionTestResult(status, MessageFor(status), duration);
    }

    private static string MessageFor(ConnectionTestStatus status) => status switch
    {
        ConnectionTestStatus.Success =>
            "La conexión con Firebird se estableció correctamente.",
        ConnectionTestStatus.InvalidConfiguration =>
            "Completa los datos requeridos para conectar con Firebird.",
        ConnectionTestStatus.AuthenticationFailed =>
            "Firebird rechazó las credenciales proporcionadas.",
        ConnectionTestStatus.ServerUnavailable =>
            "No fue posible contactar al servidor Firebird.",
        ConnectionTestStatus.DatabaseUnavailable =>
            "El servidor respondió, pero la base de datos no está disponible.",
        ConnectionTestStatus.Timeout =>
            "La conexión excedió el tiempo permitido.",
        ConnectionTestStatus.Canceled =>
            "La prueba de conexión fue cancelada.",
        ConnectionTestStatus.UnexpectedFailure =>
            "No fue posible comprobar la conexión con Firebird.",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}
