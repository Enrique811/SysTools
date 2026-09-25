using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using SysTools.Business.PriceVerifier;
using SysTools.Entities.Connection;
using SysTools.Entities.Licensing;
using SysTools.Entities.PriceVerifier;
using SysTools.Presentation.Modules.PriceVerifier.ViewModels;
using SysTools.Presentation.Shell.Services;
using SysTools.Presentation.Shell.ViewModels;

namespace SysTools.Presentation.Tests;

internal static class TestDoubles
{
    public static PriceVerifierWorkflowStub ReadyWorkflow() => new()
    {
        PrepareHandler = _ => Task.FromResult(new PriceVerifierPreparationResult(
            PriceVerifierPreparationStatus.Ready,
            "Modulo disponible.",
            ConnectionTestStatus.Success,
            LicenseValidationStatus.Valid,
            "Informacion"))
    };

    public static PriceVerifierViewModel CreatePriceVerifier(PriceVerifierWorkflowStub? workflow = null) =>
        new(workflow ?? ReadyWorkflow(), NullLogger<PriceVerifierViewModel>.Instance);

    public static ShellViewModel CreateShell(
        IModuleInitializer? initializer = null,
        PriceVerifierViewModel? priceVerifier = null) =>
        new(priceVerifier ?? CreatePriceVerifier(), initializer ?? new DefaultModuleInitializer(), NullLogger<ShellViewModel>.Instance);
}

internal sealed class ThrowingInitializer : IModuleInitializer
{
    public void Initialize(string moduleId) => throw new InvalidOperationException("C:\\private\\source.cs internal stack trace");
}

internal sealed class ControlledOperation<T>
{
    private readonly TaskCompletionSource<T> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal Task<T> Task => _completion.Task;
    internal void Complete(T value) => _completion.TrySetResult(value);
    internal void Fail(Exception exception) => _completion.TrySetException(exception);
}

internal sealed class PriceVerifierWorkflowStub : IPriceVerifierWorkflow
{
    internal Func<CancellationToken, Task<PriceVerifierPreparationResult>> PrepareHandler { get; set; } =
        static _ => Task.FromResult(new PriceVerifierPreparationResult(
            PriceVerifierPreparationStatus.ConfigurationUnavailable,
            "Configuracion no disponible."));

    internal Func<string?, CancellationToken, Task<PriceVerifierLookupResult>> LookupHandler { get; set; } =
        static (_, _) => Task.FromResult(new PriceVerifierLookupResult(
            PriceVerifierLookupStatus.NotFound,
            "Producto no encontrado."));

    internal int PrepareCalls { get; private set; }
    internal int LookupCalls { get; private set; }
    internal int InvalidateCalls { get; private set; }

    public Task<PriceVerifierPreparationResult> PrepareAsync(CancellationToken cancellationToken = default)
    {
        PrepareCalls++;
        return PrepareHandler(cancellationToken);
    }

    public Task<PriceVerifierLookupResult> LookupAsync(string? barcode, CancellationToken cancellationToken = default)
    {
        LookupCalls++;
        return LookupHandler(barcode, cancellationToken);
    }

    public void Invalidate() => InvalidateCalls++;
}

internal sealed record PresentationLog(
    LogLevel Level,
    IReadOnlyDictionary<string, object?> Properties,
    Exception? Exception);

internal sealed class CollectingLogger<T> : ILogger<T>
{
    internal List<PresentationLog> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var properties = state as IEnumerable<KeyValuePair<string, object?>>;
        Entries.Add(new PresentationLog(
            logLevel,
            properties?.ToDictionary(x => x.Key, x => x.Value) ?? new Dictionary<string, object?>(),
            exception));
    }
}
