using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using SysTools.Data.Connection;
using SysTools.Entities.Configuration;

namespace SysTools.Repositories.Tests.TestDoubles;

internal sealed class FakeRepositoryConnectionFactory : IFirebirdConnectionFactory
{
    private readonly Func<TrackingDbConnection> _factory;

    internal FakeRepositoryConnectionFactory(Func<TrackingDbConnection> factory) =>
        _factory = factory;

    internal int Calls { get; private set; }

    internal List<AppConfiguration> Configurations { get; } = [];

    public DbConnection Create(AppConfiguration configuration)
    {
        Calls++;
        Configurations.Add(configuration);
        return _factory();
    }
}

internal sealed class TrackingDbConnection : DbConnection
{
    private ConnectionState _state = ConnectionState.Closed;
    private int _disposeRecorded;

    internal Func<CancellationToken, Task>? OpenBehavior { get; set; }

    internal Func<TrackingDbCommand>? CommandFactory { get; set; }

    internal List<TrackingDbCommand> Commands { get; } = [];

    internal int OpenCalls { get; private set; }

    internal int DisposeCalls { get; private set; }

    internal CancellationToken LastOpenToken { get; private set; }

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

    protected override DbCommand CreateDbCommand()
    {
        var command = CommandFactory?.Invoke() ?? new TrackingDbCommand();
        command.Connection = this;
        Commands.Add(command);
        return command;
    }

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

internal sealed class TrackingDbCommand : DbCommand
{
    private readonly TrackingDbParameterCollection _parameters = new();
    private int _disposeRecorded;

    internal Func<CancellationToken, Task<DbDataReader>> ReaderBehavior { get; set; } =
        static _ => Task.FromResult<DbDataReader>(RepositoryRows.Products().CreateDataReader());

    internal Func<CancellationToken, Task<object?>> ScalarBehavior { get; set; } =
        static _ => Task.FromResult<object?>(DateTime.UnixEpoch);

    internal int ReaderCalls { get; private set; }

    internal int ScalarCalls { get; private set; }

    internal int DisposeCalls { get; private set; }

    internal CancellationToken LastExecutionToken { get; private set; }

    [AllowNull]
    public override string CommandText { get; set; } = string.Empty;

    public override int CommandTimeout { get; set; }

    public override CommandType CommandType { get; set; } = CommandType.Text;

    public override bool DesignTimeVisible { get; set; }

    public override UpdateRowSource UpdatedRowSource { get; set; }

    protected override DbConnection? DbConnection { get; set; }

    protected override DbParameterCollection DbParameterCollection => _parameters;

    protected override DbTransaction? DbTransaction { get; set; }

    public new TrackingDbConnection? Connection
    {
        get => (TrackingDbConnection?)DbConnection;
        set => DbConnection = value;
    }

    internal IReadOnlyList<DbParameter> ParametersSnapshot =>
        _parameters.Cast<DbParameter>().ToArray();

    public override void Cancel()
    {
    }

    public override int ExecuteNonQuery() => throw new NotSupportedException();

    public override object? ExecuteScalar() => throw new NotSupportedException();

    public override void Prepare()
    {
    }

    protected override DbParameter CreateDbParameter() => new TrackingDbParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
        throw new NotSupportedException();

    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(
        CommandBehavior behavior,
        CancellationToken cancellationToken)
    {
        ReaderCalls++;
        LastExecutionToken = cancellationToken;
        return await ReaderBehavior(cancellationToken);
    }

    public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        ScalarCalls++;
        LastExecutionToken = cancellationToken;
        return await ScalarBehavior(cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        RecordDispose();
        base.Dispose(disposing);
    }

    public override ValueTask DisposeAsync()
    {
        RecordDispose();
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

internal sealed class TrackingDbDataReader : DbDataReader
{
    private readonly DbDataReader _inner;
    private int _disposeRecorded;

    internal TrackingDbDataReader(DbDataReader inner) => _inner = inner;

    internal Func<CancellationToken, Task<bool>>? ReadBehavior { get; set; }

    internal int DisposeCalls { get; private set; }

    public override int Depth => _inner.Depth;

    public override int FieldCount => _inner.FieldCount;

    public override bool HasRows => _inner.HasRows;

    public override bool IsClosed => _inner.IsClosed;

    public override int RecordsAffected => _inner.RecordsAffected;

    public override object this[int ordinal] => _inner[ordinal];

    public override object this[string name] => _inner[name];

    public override bool GetBoolean(int ordinal) => _inner.GetBoolean(ordinal);

    public override byte GetByte(int ordinal) => _inner.GetByte(ordinal);

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) =>
        _inner.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);

    public override char GetChar(int ordinal) => _inner.GetChar(ordinal);

    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) =>
        _inner.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);

    public override string GetDataTypeName(int ordinal) => _inner.GetDataTypeName(ordinal);

    public override DateTime GetDateTime(int ordinal) => _inner.GetDateTime(ordinal);

    public override decimal GetDecimal(int ordinal) => _inner.GetDecimal(ordinal);

    public override double GetDouble(int ordinal) => _inner.GetDouble(ordinal);

    public override IEnumerator GetEnumerator() => ((IEnumerable)_inner).GetEnumerator();

    public override Type GetFieldType(int ordinal) => _inner.GetFieldType(ordinal);

    public override float GetFloat(int ordinal) => _inner.GetFloat(ordinal);

    public override Guid GetGuid(int ordinal) => _inner.GetGuid(ordinal);

    public override short GetInt16(int ordinal) => _inner.GetInt16(ordinal);

    public override int GetInt32(int ordinal) => _inner.GetInt32(ordinal);

    public override long GetInt64(int ordinal) => _inner.GetInt64(ordinal);

    public override string GetName(int ordinal) => _inner.GetName(ordinal);

    public override int GetOrdinal(string name) => _inner.GetOrdinal(name);

    public override string GetString(int ordinal) => _inner.GetString(ordinal);

    public override object GetValue(int ordinal) => _inner.GetValue(ordinal);

    public override int GetValues(object[] values) => _inner.GetValues(values);

    public override bool IsDBNull(int ordinal) => _inner.IsDBNull(ordinal);

    public override bool NextResult() => _inner.NextResult();

    public override Task<bool> NextResultAsync(CancellationToken cancellationToken) =>
        _inner.NextResultAsync(cancellationToken);

    public override bool Read() => _inner.Read();

    public override Task<bool> ReadAsync(CancellationToken cancellationToken) =>
        ReadBehavior?.Invoke(cancellationToken) ?? _inner.ReadAsync(cancellationToken);

    public override void Close()
    {
        RecordDispose();
        _inner.Close();
    }

    protected override void Dispose(bool disposing)
    {
        RecordDispose();
        _inner.Dispose();
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        RecordDispose();
        await _inner.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private void RecordDispose()
    {
        if (Interlocked.Exchange(ref _disposeRecorded, 1) == 0)
        {
            DisposeCalls++;
        }
    }
}

internal sealed class TrackingDbParameter : DbParameter
{
    public override DbType DbType { get; set; }

    public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;

    public override bool IsNullable { get; set; }

    [AllowNull]
    public override string ParameterName { get; set; } = string.Empty;

    [AllowNull]
    public override string SourceColumn { get; set; } = string.Empty;

    public override object? Value { get; set; }

    public override bool SourceColumnNullMapping { get; set; }

    public override int Size { get; set; }

    public override byte Precision { get; set; }

    public override byte Scale { get; set; }

    public override void ResetDbType()
    {
    }
}

internal sealed class TrackingDbParameterCollection : DbParameterCollection
{
    private readonly List<DbParameter> _items = [];

    public override int Count => _items.Count;

    public override object SyncRoot => ((ICollection)_items).SyncRoot;

    public override int Add(object value)
    {
        _items.Add((DbParameter)value);
        return _items.Count - 1;
    }

    public override void AddRange(Array values)
    {
        foreach (var value in values)
        {
            Add(value!);
        }
    }

    public override void Clear() => _items.Clear();

    public override bool Contains(object value) => _items.Contains((DbParameter)value);

    public override bool Contains(string value) => IndexOf(value) >= 0;

    public override void CopyTo(Array array, int index) =>
        ((ICollection)_items).CopyTo(array, index);

    public override IEnumerator GetEnumerator() => _items.GetEnumerator();

    public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);

    public override int IndexOf(string parameterName) =>
        _items.FindIndex(item => string.Equals(item.ParameterName, parameterName, StringComparison.Ordinal));

    public override void Insert(int index, object value) =>
        _items.Insert(index, (DbParameter)value);

    public override void Remove(object value) => _items.Remove((DbParameter)value);

    public override void RemoveAt(int index) => _items.RemoveAt(index);

    public override void RemoveAt(string parameterName)
    {
        var index = IndexOf(parameterName);
        if (index >= 0)
        {
            RemoveAt(index);
        }
    }

    protected override DbParameter GetParameter(int index) => _items[index];

    protected override DbParameter GetParameter(string parameterName) =>
        _items[IndexOf(parameterName)];

    protected override void SetParameter(int index, DbParameter value) => _items[index] = value;

    protected override void SetParameter(string parameterName, DbParameter value)
    {
        var index = IndexOf(parameterName);
        if (index < 0)
        {
            _items.Add(value);
        }
        else
        {
            _items[index] = value;
        }
    }
}

internal static class RepositoryRows
{
    internal static DataTable Products(params object?[][] rows)
    {
        var table = new DataTable();
        table.Columns.Add("CODIGO", typeof(int));
        table.Columns.Add("CODIGO_BARRAS", typeof(string));
        table.Columns.Add("DESCRIPCION", typeof(string));
        table.Columns.Add("PRESENTACION", typeof(string));
        table.Columns.Add("PRECIO_IVA", typeof(decimal));
        table.Columns.Add("STOCK", typeof(object));

        foreach (var row in rows)
        {
            table.Rows.Add(row.Select(value => value ?? DBNull.Value).ToArray());
        }

        return table;
    }
}
