using System.Text.Json;
using SysTools.Business.Configuration;
using SysTools.Business.ConfigurationEditor;
using SysTools.Data.Configuration;
using SysTools.Entities.Configuration;
using SysTools.Entities.ConfigurationEditor;

namespace SysTools.Data.ConfigurationEditor;

public sealed class ConfigurationRecoveryService : IConfigurationRecoveryService
{
    private readonly IConfigurationPathProvider _paths;
    private readonly ISecretProtector _protector;
    private readonly IAtomicFileWriter _writer;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public ConfigurationRecoveryService(IConfigurationPathProvider paths, ISecretProtector protector, IAtomicFileWriter writer)
    { _paths = paths; _protector = protector; _writer = writer; }

    public async Task<ConfigurationRecoveryResult> RecoverAsync(AppConfiguration configuration, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        string? backup = null;
        string? path = null;
        try
        {
            path = Path.GetFullPath(_paths.GetConfigurationFilePath());
            var directory = Path.GetDirectoryName(path) ?? throw new IOException();
            Directory.CreateDirectory(directory);
            if (File.Exists(path))
            {
                backup = Path.Combine(directory, $"configuracion.invalid.{DateTime.UtcNow:yyyyMMddHHmmssfff}.bak");
                File.Move(path, backup);
            }
            var protectedPassword = _protector.Protect(configuration.Password);
            var document = StoredConfigurationDocument.FromConfiguration(configuration, protectedPassword, 1, null);
            var payload = JsonSerializer.SerializeToUtf8Bytes(document, new JsonSerializerOptions { WriteIndented = true });
            await _writer.WriteAsync(path, payload, cancellationToken).ConfigureAwait(false);
            return new(ConfigurationRecoveryStatus.Recovered);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        { Restore(path, backup); return new(ConfigurationRecoveryStatus.Canceled); }
        catch (System.Security.Cryptography.CryptographicException)
        { Restore(path, backup); return new(ConfigurationRecoveryStatus.ProtectionFailure); }
        catch (UnauthorizedAccessException)
        { Restore(path, backup); return new(ConfigurationRecoveryStatus.AccessDenied); }
        catch
        { Restore(path, backup); return new(ConfigurationRecoveryStatus.StorageFailure); }
        finally { _lock.Release(); }
    }

    private static void Restore(string? path, string? backup)
    {
        try { if (path is not null && backup is not null && File.Exists(backup) && !File.Exists(path)) File.Move(backup, path); }
        catch { }
    }
}
