using Microsoft.Extensions.Logging;
using SysTools.Entities.Configuration;

namespace SysTools.Repositories.Tests.TestDoubles;

internal sealed record CapturedRepositoryLog(
    LogLevel Level,
    EventId EventId,
    string Message,
    Exception? Exception,
    IReadOnlyDictionary<string, object?> Properties);

internal sealed class RepositoryListLogger<T> : ILogger<T>
{
    internal List<CapturedRepositoryLog> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var properties = state is IEnumerable<KeyValuePair<string, object?>> values
            ? values.Where(pair => pair.Key != "{OriginalFormat}")
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)
            : new Dictionary<string, object?>();
        Entries.Add(new(logLevel, eventId, formatter(state, exception), exception, properties));
    }
}

internal static class RepositoryTestConfiguration
{
    internal static AppConfiguration Valid(
        string password = "Systools-Repo-Secret-Password") =>
        new()
        {
            IpEmpresa = "Systools-Repo-Secret-Host",
            RutaEmpresa = @"C:\Systools-Repo-Secret-Database\test.fdb",
            Usuario = "Systools-Repo-Secret-User",
            Password = password,
            Licencia = "Systools-Repo-Secret-License"
        };
}
