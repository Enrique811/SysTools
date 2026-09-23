# Contracts: Product repository and server clock

## `IProductRepository`

```csharp
Task<Product?> GetByBarcodeAsync(
    AppConfiguration configuration,
    string barcode,
    CancellationToken cancellationToken = default);

Task<IReadOnlyList<Product>> SearchByDescriptionAsync(
    AppConfiguration configuration,
    string descriptionPrefix,
    CancellationToken cancellationToken = default);
```

### Preconditions

- `configuration` is not null and was loaded by the existing configuration service.
- `barcode` is not null, empty or whitespace and is at most 50 characters.
- A null, empty or whitespace `descriptionPrefix` is accepted and returns an empty collection without opening a connection.

### Results

- Exact match: one immutable `Product`.
- No exact match: `null`.
- Search match: an immutable/read-only collection ordered by description, then identifier for deterministic ties.
- No search match: an empty immutable/read-only collection.
- A provider/network/query failure: `RepositoryAccessException` with the matching `RepositoryOperation` and a safe catalog message.
- Consumer cancellation: `OperationCanceledException` associated with the supplied token.

### Security and lifecycle

- Inputs are bound parameters; callers cannot alter the SQL structure.
- Each invocation owns and disposes its connection, command and reader.
- No contract exposes SQL, provider types, configuration values or raw exceptions.

## `IServerClockRepository`

```csharp
Task<DateTime> GetCurrentAsync(
    AppConfiguration configuration,
    CancellationToken cancellationToken = default);
```

### Results

- Success: the exact timestamp returned by Firebird.
- Missing/null row, provider/network/query failure: `RepositoryAccessException` for `GetServerTimestamp`.
- Consumer cancellation: `OperationCanceledException` associated with the supplied token.

### Time semantics

- The implementation must execute `CURRENT_TIMESTAMP` against the configured server.
- It must not call the local clock as a source or fallback.
- It must not infer or convert a timezone because Firebird `TIMESTAMP` supplies none.

## Composition contract

The composition root registers both interfaces and their Firebird implementations as stateless services. Resolving either repository must not open a connection. Business, Entities, Views and ViewModels remain free of Firebird provider references.
