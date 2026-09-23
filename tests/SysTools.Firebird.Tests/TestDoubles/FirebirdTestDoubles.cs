using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using SysTools.Business.Connection;
using SysTools.Data.Connection;
using SysTools.Entities.Configuration;
using SysTools.Entities.Connection;

namespace SysTools.Firebird.Tests.TestDoubles;

internal sealed class FakeFirebirdConnectionProbe : IFirebirdConnectionProbe
{
    public Func<AppConfiguration, CancellationToken, Task<ConnectionTestStatus>> Behavior { get; set; } =
        static (_, _) => Task.FromResult(ConnectionTestStatus.Success);

    public int Calls { get; private set; }

    public AppConfiguration? LastConfiguration { get; private set; }

    public CancellationToken LastCancellationToken { get; private set; }

    public Task<ConnectionTestStatus> ProbeAsync(
        AppConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        LastConfiguration = configuration;
        LastCancellationToken = cancellationToken;
        return Behavior(configuration, cancellationToken);
    }
}

internal sealed class FakeFirebirdConnectionFactory : IFirebirdConnectionFactory
{
    private readonly Func<AppConfiguration, DbConnection> _factory;

    public FakeFirebirdConnectionFactory(Func<AppConfiguration, DbConnection> factory) =>
        _factory = factory;

    public int Calls { get; private set; }

    public List<AppConfiguration> Configurations { get; } = [];

    public DbConnection Create(AppConfiguration configuration)
    {
        Calls++;
        Configurations.Add(configuration);
        return _factory(configuration);
    }
}

internal sealed class TrackingDbConnection : DbConnection
{
    private ConnectionState _state = ConnectionState.Closed;
    private int _disposeRecorded;

    public Func<CancellationToken, Task>? OpenBehavior { get; set; }

    public int OpenCalls { get; private set; }

    public int DisposeCalls { get; private set; }

    public CancellationToken LastOpenToken { get; private set; }

    [AllowNull]
    public override string ConnectionString { get; set; } = string.Empty;

    public override string Database => "test";

    public override string DataSource => "test";

    public override string ServerVersion => "test";

    public override ConnectionState State => _state;

    public override void ChangeDatabase(string databaseName) =>
        throw new NotSupportedException();

    public override void Close() => _state = ConnectionState.Closed;

    public override void Open()
    {
        OpenCalls++;
        _state = ConnectionState.Open;
    }

    public override async Task OpenAsync(CancellationToken cancellationToken)
    {
        OpenCalls++;
        LastOpenToken = cancellationToken;
        if (OpenBehavior is not null)
        {
            await OpenBehavior(cancellationToken);
        }

        _state = ConnectionState.Open;
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
        throw new NotSupportedException();

    protected override DbCommand CreateDbCommand() =>
        throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        RecordDispose();
        _state = ConnectionState.Closed;
        base.Dispose(disposing);
    }

    public override ValueTask DisposeAsync()
    {
        RecordDispose();
        _state = ConnectionState.Closed;
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    private void RecordDispose()
    {
        if (Interlocked.Exchange(ref _disposeRecorded, 1) == 0)
        {
            DisposeCalls++;
        }
    }
}

internal sealed record CapturedLogEntry(
    LogLevel Level,
    EventId EventId,
    string Message,
    Exception? Exception,
    IReadOnlyDictionary<string, object?> Properties);

internal sealed class ListLogger<T> : ILogger<T>
{
    public List<CapturedLogEntry> Entries { get; } = [];

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

internal static class FirebirdTestConfiguration
{
    internal static AppConfiguration Valid(
        string password = "Systools-Fb-Secret-Test") =>
        new()
        {
            IpEmpresa = "192.0.2.10",
            RutaEmpresa = @"C:\Datos de prueba\empresa.fdb",
            Usuario = "SYSDBA_TEST",
            Password = password,
            Licencia = "Systools-Fb-License-Test"
        };
}
