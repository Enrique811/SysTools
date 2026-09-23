using Microsoft.Extensions.Logging;
using SysTools.Entities.Configuration;

namespace SysTools.Business.Configuration;

public sealed class ConfigurationService : IConfigurationService
{
    private readonly IConfigurationRepository _repository;
    private readonly ConfigurationValidator _validator;
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(
        IConfigurationRepository repository,
        ConfigurationValidator validator,
        ILogger<ConfigurationService> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public ConfigurationValidationResult Validate(AppConfiguration configuration) =>
        _validator.Validate(configuration);

    public async Task<ConfigurationLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ConfigurationLoadStarted Operation={Operation}", "Load");
        var readResult = await _repository.ReadAsync(cancellationToken);

        if (readResult.Status == ConfigurationRepositoryReadStatus.Found && readResult.Configuration is not null)
        {
            var validation = Validate(readResult.Configuration);
            LogLoadCompleted(ConfigurationLoadStatus.Loaded, validation);
            return new(ConfigurationLoadStatus.Loaded, readResult.Configuration, validation, validation.Issues);
        }

        if (readResult.Status == ConfigurationRepositoryReadStatus.NotFound)
        {
            var defaults = AppConfiguration.CreateDefault();
            var validation = Validate(defaults);
            var writeResult = await _repository.WriteAsync(defaults, cancellationToken);
            if (writeResult.Status == ConfigurationRepositoryWriteStatus.Saved)
            {
                _logger.LogInformation(
                    "ConfigurationDefaultCreated Operation={Operation} Outcome={Outcome}",
                    "Load",
                    ConfigurationLoadStatus.DefaultCreated);
                return new(ConfigurationLoadStatus.DefaultCreated, defaults, validation, validation.Issues);
            }

            return MapLoadWriteFailure(writeResult.Status, validation);
        }

        return MapReadFailure(readResult.Status);
    }

    public async Task<ConfigurationSaveResult> SaveAsync(
        AppConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var validation = Validate(configuration);
        if (!validation.IsPersistable)
        {
            _logger.LogWarning(
                "ConfigurationValidationFailed Operation={Operation} IssueCodes={IssueCodes} IssueFields={IssueFields}",
                "Save",
                validation.Issues.Select(issue => issue.Code).ToArray(),
                validation.Issues.Select(issue => issue.Field).ToArray());
            return new(ConfigurationSaveStatus.ValidationFailed, validation, validation.Issues);
        }

        var writeResult = await _repository.WriteAsync(configuration, cancellationToken);
        var status = writeResult.Status switch
        {
            ConfigurationRepositoryWriteStatus.Saved => ConfigurationSaveStatus.Saved,
            ConfigurationRepositoryWriteStatus.InvalidExistingContent => ConfigurationSaveStatus.InvalidExistingContent,
            ConfigurationRepositoryWriteStatus.ProtectionFailure => ConfigurationSaveStatus.ProtectionFailure,
            ConfigurationRepositoryWriteStatus.AccessDenied => ConfigurationSaveStatus.AccessDenied,
            ConfigurationRepositoryWriteStatus.Canceled => ConfigurationSaveStatus.Canceled,
            _ => ConfigurationSaveStatus.StorageFailure
        };
        var issues = status == ConfigurationSaveStatus.Saved
            ? validation.Issues
            : [IssueForSaveStatus(status)];
        _logger.LogInformation(
            "ConfigurationSaveCompleted Operation={Operation} Outcome={Outcome} IsConnectionReady={IsConnectionReady}",
            "Save",
            status,
            validation.IsConnectionReady);
        return new(status, validation, issues);
    }

    private ConfigurationLoadResult MapReadFailure(ConfigurationRepositoryReadStatus repositoryStatus)
    {
        var status = repositoryStatus switch
        {
            ConfigurationRepositoryReadStatus.InvalidContent => ConfigurationLoadStatus.InvalidContent,
            ConfigurationRepositoryReadStatus.SecretUnavailable => ConfigurationLoadStatus.SecretUnavailable,
            ConfigurationRepositoryReadStatus.AccessDenied => ConfigurationLoadStatus.AccessDenied,
            ConfigurationRepositoryReadStatus.Canceled => ConfigurationLoadStatus.Canceled,
            _ => ConfigurationLoadStatus.StorageFailure
        };
        return new(status, issues: [IssueForLoadStatus(status)]);
    }

    private ConfigurationLoadResult MapLoadWriteFailure(
        ConfigurationRepositoryWriteStatus repositoryStatus,
        ConfigurationValidationResult validation)
    {
        var status = repositoryStatus switch
        {
            ConfigurationRepositoryWriteStatus.AccessDenied => ConfigurationLoadStatus.AccessDenied,
            ConfigurationRepositoryWriteStatus.Canceled => ConfigurationLoadStatus.Canceled,
            _ => ConfigurationLoadStatus.StorageFailure
        };
        return new(status, validation: validation, issues: [IssueForLoadStatus(status)]);
    }

    private void LogLoadCompleted(
        ConfigurationLoadStatus status,
        ConfigurationValidationResult validation) =>
        _logger.LogInformation(
            "ConfigurationLoadCompleted Operation={Operation} Outcome={Outcome} IsConnectionReady={IsConnectionReady} IssueCount={IssueCount}",
            "Load",
            status,
            validation.IsConnectionReady,
            validation.Issues.Count);

    private static ConfigurationIssue IssueForLoadStatus(ConfigurationLoadStatus status) => status switch
    {
        ConfigurationLoadStatus.InvalidContent => Error(ConfigurationIssueCode.MalformedConfiguration, "La configuración local no tiene un formato válido."),
        ConfigurationLoadStatus.SecretUnavailable => Error(ConfigurationIssueCode.SecretUnavailable, "La credencial guardada debe capturarse nuevamente."),
        ConfigurationLoadStatus.AccessDenied => Error(ConfigurationIssueCode.AccessDenied, "No fue posible acceder a la configuración local."),
        ConfigurationLoadStatus.Canceled => Error(ConfigurationIssueCode.OperationCanceled, "La operación de configuración fue cancelada."),
        _ => Error(ConfigurationIssueCode.StorageUnavailable, "No fue posible usar el almacenamiento de configuración.")
    };

    private static ConfigurationIssue IssueForSaveStatus(ConfigurationSaveStatus status) => status switch
    {
        ConfigurationSaveStatus.InvalidExistingContent => Error(ConfigurationIssueCode.MalformedConfiguration, "La configuración existente requiere revisión antes de reemplazarla."),
        ConfigurationSaveStatus.ProtectionFailure => Error(ConfigurationIssueCode.SecretUnavailable, "No fue posible proteger la credencial."),
        ConfigurationSaveStatus.AccessDenied => Error(ConfigurationIssueCode.AccessDenied, "No fue posible actualizar la configuración local."),
        ConfigurationSaveStatus.Canceled => Error(ConfigurationIssueCode.OperationCanceled, "La operación de configuración fue cancelada."),
        _ => Error(ConfigurationIssueCode.StorageUnavailable, "No fue posible guardar la configuración local.")
    };

    private static ConfigurationIssue Error(ConfigurationIssueCode code, string message) =>
        new(code, null, message, ConfigurationIssueSeverity.Error);
}
