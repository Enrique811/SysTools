using Microsoft.Extensions.Logging;
using SysTools.Business.Configuration;
using SysTools.Data.Configuration;
using SysTools.Entities.Configuration;

namespace SysTools.Configuration.Tests.TestDoubles;

internal sealed class FakeConfigurationRepository : IConfigurationRepository
{
    public ConfigurationRepositoryReadResult ReadResult { get; set; } =
        new(ConfigurationRepositoryReadStatus.NotFound);

    public ConfigurationRepositoryWriteResult WriteResult { get; set; } =
        new(ConfigurationRepositoryWriteStatus.Saved);

    public int ReadCalls { get; private set; }
    public int WriteCalls { get; private set; }
    public AppConfiguration? LastWritten { get; private set; }

    public Task<ConfigurationRepositoryReadResult> ReadAsync(CancellationToken cancellationToken = default)
    {
        ReadCalls++;
        return Task.FromResult(ReadResult);
    }

    public Task<ConfigurationRepositoryWriteResult> WriteAsync(
        AppConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        WriteCalls++;
        LastWritten = configuration;
        return Task.FromResult(WriteResult);
    }
}

internal sealed class FakeSecretProtector : ISecretProtector
{
    public bool ThrowOnProtect { get; set; }
    public bool ThrowOnUnprotect { get; set; }

    public string Protect(string plaintext)
    {
        if (ThrowOnProtect) throw new System.Security.Cryptography.CryptographicException("protect failed");
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"protected:{plaintext}"));
    }

    public string Unprotect(string protectedValue)
    {
        if (ThrowOnUnprotect) throw new System.Security.Cryptography.CryptographicException("unprotect failed");
        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(protectedValue));
        return decoded.StartsWith("protected:", StringComparison.Ordinal)
            ? decoded["protected:".Length..]
            : throw new System.Security.Cryptography.CryptographicException("invalid payload");
    }
}

internal sealed class FakePathProvider(string path) : IConfigurationPathProvider
{
    public string GetConfigurationFilePath() => path;
}

internal sealed class FakeAtomicFileWriter : IAtomicFileWriter
{
    public Exception? ExceptionToThrow { get; set; }
    public int Calls { get; private set; }
    public byte[]? LastContent { get; private set; }

    public Task WriteAsync(string destinationPath, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)
    {
        Calls++;
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        LastContent = content.ToArray();
        return Task.CompletedTask;
    }
}

internal sealed class ListLogger<T> : ILogger<T>
{
    public List<string> Messages { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Messages.Add(formatter(state, exception));
    }
}

internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "SysTools.Configuration.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string File(string name = "configuracion.json") => System.IO.Path.Combine(Path, name);

    public void Dispose()
    {
        if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
    }
}
