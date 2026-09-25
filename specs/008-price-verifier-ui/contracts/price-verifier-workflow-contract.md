# Contract: Price verifier workflow and UI lifecycle

## Business application contract

```csharp
public interface IPriceVerifierWorkflow
{
    Task<PriceVerifierPreparationResult> PrepareAsync(
        CancellationToken cancellationToken = default);

    Task<PriceVerifierLookupResult> LookupAsync(
        string? barcode,
        CancellationToken cancellationToken = default);

    void Invalidate();
}
```

### PrepareAsync

1. Invalidate the prior session and begin a new epoch.
2. Load configuration through `IConfigurationService.LoadAsync`.
3. Require a configuration with validation `IsConnectionReady`.
4. Test connection through `IConnectionTestService.TestAsync`; only `Success` continues.
5. Validate `configuration.Licencia` through `ILicenseService.ValidateAsync`; only `IsValid` continues.
6. Commit the private ready session only if its epoch remains current.
7. Return a safe result; never return configuration or exception data.

`DefaultCreated` is not readiness by itself. Requested cancellation propagates. A late non-cooperative completion from an invalidated epoch cannot commit or publish a session.

### LookupAsync

1. Require a current Ready session; otherwise return `OperationalFailure` without repository access.
2. Normalize outer whitespace once, preserving leading zeroes and interior content.
3. Return `MissingInput` for empty and `InputTooLong` above 50 before calling products.
4. Call `IProductService.GetByBarcodeAsync` exactly once.
5. Return `NotFound` when null.
6. If Product has `PriceWithTax`, call `IPriceFormatterService.Format` once with cached `FormatoPrecio`.
7. Return Success with Product and optional formatted price.
8. Translate expected catalog failures to `OperationalFailure`, invalidate session and return zero product data.

Cancellation propagates. Unexpected fatal exceptions are not hidden. Business logs only Stage, Status/Category and DurationMs.

### Invalidate

- Clears private configuration/readiness.
- Advances epoch so older completions cannot mutate current state.
- Does not log or return former configuration.

## Presentation lifecycle contract

```csharp
public interface IAsyncModuleLifecycle
{
    Task ActivateAsync(CancellationToken cancellationToken = default);
    void Deactivate();
}
```

- Initial View load activates once after shell can render.
- Reactivation means returning through shell navigation, not OS window activation or Alt-Tab.
- Activate runs Prepare and publishes only for its current generation.
- Deactivate cancels lifecycle, invalidates workflow and suppresses late publication.
- Closing shell deactivates active lifecycle.

## Command contract

`AsyncRelayCommand` implements WPF `ICommand` and exposes awaitable `ExecuteAsync` for tests.

- Async delegate plus synchronous availability predicate.
- `CanExecute == false` while active.
- Repeated Execute is ignored; never queued and never cancels active execution.
- Raises `CanExecuteChanged` at both execution boundaries.
- Recoverable errors never escape through `async void` to dispatcher.

## ViewModel publication contract

### Prepare / Retry

- Start: Busy=true, barcode disabled, connection/license Pending, fields neutral.
- Ready: Busy=false, capture enabled, statuses Available, safe message, additional information and focus request.
- Blocked: Busy=false, capture disabled, safe status/message, Retry enabled and fields neutral.

Retry repeats configuration, connection and license. No periodic/per-barcode revalidation occurs while Ready.

### Submit barcode

- Enter invokes query with current two-way Barcode.
- Busy/unavailable command cannot execute; direct repeated invocation is also ignored.
- During query: Busy=true and capture disabled.
- Success: publish atomically, retain Barcode and request focus/select-all.
- Missing/InputTooLong/NotFound: retain Ready, neutral fields, safe message and focus request.
- OperationalFailure: neutral fields, block capture, expose Retry and coherent shell statuses.
- Cancellation/deactivation: publish nothing from canceled generation.

## Focus and accessibility contract

- View translates `FocusRequestVersion` into `Focus()` plus `SelectAll()` through dispatcher.
- Capture label is associated with TextBox; Enter works without mouse.
- Busy/availability have explicit text, not only color.
- Operational message is announced politely with severity in accessible name.
- Buscar, Imprimir and Configuracion remain visible, disabled and outside tab sequence.
- Essential content stays visible at 1280x720 and wraps at 125% DPI.

## Shell coherence contract

`ShellViewModel` proxies PriceVerifierViewModel connection, license and message properties and forwards change notifications. It has no independent mutable copies.

## Composition contract

- Register `IPriceVerifierWorkflow -> PriceVerifierWorkflow` singleton.
- Resolve one singleton `PriceVerifierViewModel`; shell and View use the same instance.
- Existing service registrations remain unchanged.
- Data types are never injected into Views/ViewModels.

## Privacy and scope contract

Allowed diagnostic properties: Stage, preparation/lookup Status, failure Category, DurationMs.

Forbidden in UI state/new logs: barcode value/fragment, Product serialization/full fields, host, database path, username, password, configuration serialization, license content/path/signature, UUID, raw exception message or stack trace.

Forbidden calls: `SearchByDescriptionAsync`, barcode image generation, label queue capture, report/preview/printing APIs and configuration Save/edit operations.
