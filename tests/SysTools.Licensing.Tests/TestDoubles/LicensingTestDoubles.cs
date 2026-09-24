using Microsoft.Extensions.Logging;
using SysTools.Business.Licensing;
using SysTools.Business.Repositories;
using SysTools.Entities.Configuration;
using SysTools.Entities.Licensing;

namespace SysTools.Licensing.Tests.TestDoubles;

internal sealed class StubLicenseSourceReader : ILicenseSourceReader
{
    public LicenseSourceReadResult Result { get; set; } =
        LicenseSourceReadResult.Failed(LicenseSourceFailure.NotFound);

    public Exception? Exception { get; set; }

    public int Calls { get; private set; }

    public string? LastPath { get; private set; }

    public Task<LicenseSourceReadResult> ReadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        LastPath = path;
        cancellationToken.ThrowIfCancellationRequested();
        if (Exception is not null)
        {
            throw Exception;
        }

        return Task.FromResult(Result);
    }
}

internal sealed class StubHardwareIdProvider : IHardwareIdProvider
{
    public string? Value { get; set; }

    public Exception? Exception { get; set; }

    public int Calls { get; private set; }

    public Task<string?> GetHardwareIdAsync(CancellationToken cancellationToken = default)
    {
        Calls++;
        cancellationToken.ThrowIfCancellationRequested();
        if (Exception is not null)
        {
            throw Exception;
        }

        return Task.FromResult(Value);
    }
}

internal sealed class StubServerClockRepository : IServerClockRepository
{
    public DateTime Value { get; set; } = new(2026, 6, 1);

    public Exception? Exception { get; set; }

    public int Calls { get; private set; }

    public Task<DateTime> GetCurrentAsync(
        AppConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        cancellationToken.ThrowIfCancellationRequested();
        if (Exception is not null)
        {
            throw Exception;
        }

        return Task.FromResult(Value);
    }
}

internal sealed class StubSignatureVerifier : ILicenseSignatureVerifier
{
    public LicenseIssuer Issuer { get; set; } = LicenseIssuer.Developer;

    public int Calls { get; private set; }

    public LicenseIssuer Verify(ReadOnlySpan<byte> payload, ReadOnlySpan<byte> signature)
    {
        Calls++;
        return Issuer;
    }
}

internal sealed class CollectingLogger<T> : ILogger<T>
{
    public List<string> Messages { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        Messages.Add(formatter(state, exception));
}
