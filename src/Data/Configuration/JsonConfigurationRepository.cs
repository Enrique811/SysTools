using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SysTools.Business.Configuration;
using SysTools.Entities.Configuration;

namespace SysTools.Data.Configuration;

public sealed class JsonConfigurationRepository : IConfigurationRepository
{
    internal const long MaximumDocumentSize = 1024 * 1024;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = false
    };

    private readonly IConfigurationPathProvider _pathProvider;
    private readonly ISecretProtector _secretProtector;
    private readonly IAtomicFileWriter _fileWriter;
    private readonly ILogger<JsonConfigurationRepository> _logger;
    private readonly SemaphoreSlim _operationLock = new(1, 1);

    public JsonConfigurationRepository(
        IConfigurationPathProvider pathProvider,
        ISecretProtector secretProtector,
        IAtomicFileWriter fileWriter,
        ILogger<JsonConfigurationRepository> logger)
    {
        _pathProvider = pathProvider;
        _secretProtector = secretProtector;
        _fileWriter = fileWriter;
        _logger = logger;
    }

    public async Task<ConfigurationRepositoryReadResult> ReadAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _operationLock.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return new(ConfigurationRepositoryReadStatus.Canceled);
        }

        try
        {
            var path = _pathProvider.GetConfigurationFilePath();
            if (!File.Exists(path)) return new(ConfigurationRepositoryReadStatus.NotFound);

            var document = await ReadDocumentAsync(path, cancellationToken);
            var protectedPassword = document.PasswordProtegido ?? string.Empty;
            var password = string.IsNullOrEmpty(protectedPassword)
                ? string.Empty
                : _secretProtector.Unprotect(protectedPassword);

            _logger.LogInformation(
                "ConfigurationLoadCompleted Operation={Operation} Outcome={Outcome}",
                "Load",
                ConfigurationRepositoryReadStatus.Found);
            return new(ConfigurationRepositoryReadStatus.Found, document.ToConfiguration(password));
        }
        catch (OperationCanceledException)
        {
            return new(ConfigurationRepositoryReadStatus.Canceled);
        }
        catch (JsonException exception)
        {
            LogFailure("Load", ConfigurationRepositoryReadStatus.InvalidContent, exception);
            return new(ConfigurationRepositoryReadStatus.InvalidContent);
        }
        catch (FormatException exception)
        {
            LogFailure("Load", ConfigurationRepositoryReadStatus.SecretUnavailable, exception);
            return new(ConfigurationRepositoryReadStatus.SecretUnavailable);
        }
        catch (CryptographicException exception)
        {
            LogFailure("Load", ConfigurationRepositoryReadStatus.SecretUnavailable, exception);
            return new(ConfigurationRepositoryReadStatus.SecretUnavailable);
        }
        catch (UnauthorizedAccessException exception)
        {
            LogFailure("Load", ConfigurationRepositoryReadStatus.AccessDenied, exception);
            return new(ConfigurationRepositoryReadStatus.AccessDenied);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException)
        {
            LogFailure("Load", ConfigurationRepositoryReadStatus.StorageFailure, exception);
            return new(ConfigurationRepositoryReadStatus.StorageFailure);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task<ConfigurationRepositoryWriteResult> WriteAsync(
        AppConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        try
        {
            await _operationLock.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return new(ConfigurationRepositoryWriteStatus.Canceled);
        }

        try
        {
            var path = _pathProvider.GetConfigurationFilePath();
            Dictionary<string, JsonElement>? extensionData = null;
            var schemaVersion = 1;
            if (File.Exists(path))
            {
                var existing = await ReadDocumentAsync(path, cancellationToken);
                extensionData = existing.ExtensionData;
                schemaVersion = Math.Max(1, existing.SchemaVersion);
            }

            var protectedPassword = string.IsNullOrEmpty(configuration.Password)
                ? string.Empty
                : _secretProtector.Protect(configuration.Password);
            var document = StoredConfigurationDocument.FromConfiguration(
                configuration,
                protectedPassword,
                schemaVersion,
                extensionData);
            var payload = JsonSerializer.SerializeToUtf8Bytes(document, SerializerOptions);
            await _fileWriter.WriteAsync(path, payload, cancellationToken);

            _logger.LogInformation(
                "ConfigurationSaveCompleted Operation={Operation} Outcome={Outcome}",
                "Save",
                ConfigurationRepositoryWriteStatus.Saved);
            return new(ConfigurationRepositoryWriteStatus.Saved);
        }
        catch (OperationCanceledException)
        {
            return new(ConfigurationRepositoryWriteStatus.Canceled);
        }
        catch (JsonException exception)
        {
            LogFailure("Save", ConfigurationRepositoryWriteStatus.InvalidExistingContent, exception);
            return new(ConfigurationRepositoryWriteStatus.InvalidExistingContent);
        }
        catch (FormatException exception)
        {
            LogFailure("Save", ConfigurationRepositoryWriteStatus.ProtectionFailure, exception);
            return new(ConfigurationRepositoryWriteStatus.ProtectionFailure);
        }
        catch (CryptographicException exception)
        {
            LogFailure("Save", ConfigurationRepositoryWriteStatus.ProtectionFailure, exception);
            return new(ConfigurationRepositoryWriteStatus.ProtectionFailure);
        }
        catch (UnauthorizedAccessException exception)
        {
            LogFailure("Save", ConfigurationRepositoryWriteStatus.AccessDenied, exception);
            return new(ConfigurationRepositoryWriteStatus.AccessDenied);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException)
        {
            LogFailure("Save", ConfigurationRepositoryWriteStatus.StorageFailure, exception);
            return new(ConfigurationRepositoryWriteStatus.StorageFailure);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    private static async Task<StoredConfigurationDocument> ReadDocumentAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(path);
        if (fileInfo.Length > MaximumDocumentSize)
        {
            throw new JsonException("El documento de configuración excede el tamaño permitido.");
        }

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var document = await JsonSerializer.DeserializeAsync<StoredConfigurationDocument>(
            stream,
            SerializerOptions,
            cancellationToken);
        return document ?? throw new JsonException("El documento de configuración está vacío.");
    }

    private void LogFailure(string operation, object outcome, Exception exception) =>
        _logger.LogError(
            "ConfigurationOperationFailed Operation={Operation} Outcome={Outcome} Category={Category}",
            operation,
            outcome,
            exception.GetType().Name);
}
