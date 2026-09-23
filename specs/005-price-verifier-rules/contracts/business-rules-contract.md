# Business Rules Contract

## `IProductService`

```csharp
Task<Product?> GetByBarcodeAsync(
    AppConfiguration configuration,
    string? barcode,
    CancellationToken cancellationToken = default);

Task<IReadOnlyList<Product>> SearchByDescriptionAsync(
    AppConfiguration configuration,
    string? descriptionPrefix,
    CancellationToken cancellationToken = default);
```

### Contract rules

- `configuration == null` throws `ArgumentNullException`.
- Null/empty/whitespace input returns `null` or an empty read-only list and invokes the repository zero times.
- Nonempty input is trimmed only at the edges before delegation; barcode remains text and preserves interior characters and leading zeroes.
- Returned `Product` instances are not cloned or mutated; search order is preserved.
- Consumer cancellation remains `OperationCanceledException` with the original cancellation semantics.
- `RepositoryAccessException` is propagated unchanged; no provider message, SQL, configuration or product payload is added.

## `IPriceFormatterService`

```csharp
string Format(decimal amount, string? formatCode);
```

### Contract rules

- Exact ordinal code `MX` selects Mexico behavior: `MidpointRounding.ToEven`, two decimals, `$` prefix and `.` decimal separator.
- Any other code, including null, empty or different casing, selects legacy fallback `CO`: `MidpointRounding.ToEven`, zero decimals and explicit `es-CO` currency formatting.
- Output is independent of `CultureInfo.CurrentCulture` and `CurrentUICulture`.
- The service accepts the full `decimal` range supported by formatting and does not convert through floating point.

## `LabelData`

```csharp
LabelData(Product product, string formattedPrice, string? information);
```

### Contract rules

- Null product throws `ArgumentNullException`.
- A product whose `PriceWithTax` is null throws `ArgumentException`; a label cannot represent an unknown price.
- Null/empty/whitespace formatted price throws `ArgumentException`.
- Null information becomes `string.Empty`; nonnull information is preserved exactly.

## `ILabelQueueService`

```csharp
LabelQueueResult Capture(LabelData data, int columns);
int Cancel();
IReadOnlyList<PendingLabel> GetPending();
```

### Capture contract

- Null data throws `ArgumentNullException` without changing state.
- Columns outside 1..3 throw `ArgumentOutOfRangeException` without changing state.
- With no pending labels and `columns == 1`, returns `Completed = [data]`, `Pending = []`, `Capacity = 1` and keeps internal state empty.
- With `columns == 2|3`, appends a pending label with the next base-1 position.
- Before capacity, returns a pending snapshot, no completed elements and the correct `Remaining`.
- On capacity, atomically returns all label data in capture order as `Completed`, returns no pending elements and clears internal state.
- If pending capacity differs from `columns`, throws `InvalidOperationException` before changing state.
- Each returned collection is a detached read-only snapshot.

### Cancellation and inspection contract

- `Cancel()` atomically clears all pending labels and returns their previous count; when empty it returns `0`.
- `GetPending()` returns a detached read-only snapshot in position order.
- A completed row belongs to the caller after return and is not affected by a later cancel or capture.

## Composition contract

- `IProductService` resolves to `ProductService`.
- `IPriceFormatterService` resolves to `PriceFormatterService`.
- `ILabelQueueService` resolves to a single `LabelQueueService` instance for the application session.
- No Business or Entities type introduced here references Presentation, Data, WPF, Firebird or report libraries.
