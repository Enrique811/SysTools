using System.Collections.ObjectModel;
using SysTools.Business.Support;
using SysTools.Entities.Support;
using SysTools.Presentation.Commands;
using SysTools.Presentation.Modules.Configuration.Services;
using SysTools.Presentation.Shell.Services;
using SysTools.Presentation.ViewModels;

namespace SysTools.Presentation.Modules.Support;

public sealed class SupportViewModel : ViewModelBase, IAsyncModuleLifecycle
{
    private readonly ISupportWorkflow _workflow;
    private readonly IClipboardService _clipboard;
    private CancellationTokenSource? _lifecycle;
    private long _generation;
    private string _hardwareId = string.Empty;
    private bool _hardwareAvailable;
    private string _errorSummary = string.Empty;
    private string _errorDetail = string.Empty;
    private SupportChannel _selectedChannel;
    private SupportMailDraft? _currentDraft;
    private LogFileSummary? _selectedLog;
    private string _logPreview = string.Empty;
    private string _statusText = "Módulo pendiente de preparación.";
    private bool _isBusy;

    public SupportViewModel(ISupportWorkflow workflow, IClipboardService clipboard)
    {
        _workflow = workflow; _clipboard = clipboard;
        Logs = [];
        Channels = Enum.GetValues<SupportChannel>();
        PrepareLicenseCommand = new AsyncRelayCommand(PrepareLicenseAsync, () => !IsBusy && HardwareAvailable);
        PrepareErrorCommand = new AsyncRelayCommand(PrepareErrorAsync, () => !IsBusy);
        OpenDraftCommand = new AsyncRelayCommand(OpenDraftAsync, () => !IsBusy && CurrentDraft is not null);
        RefreshLogsCommand = new AsyncRelayCommand(RefreshLogsAsync, () => !IsBusy);
        LoadSelectedLogCommand = new AsyncRelayCommand(LoadSelectedLogAsync, () => !IsBusy && SelectedLog is not null);
        OpenLogFolderCommand = new AsyncRelayCommand(OpenLogFolderAsync, () => !IsBusy);
        CopyHardwareIdCommand = new RelayCommand(CopyHardwareId, () => HardwareAvailable && !IsBusy);
    }

    public string ModuleTitle => "Soporte y logs";
    public string ModuleDescription => "Prepare solicitudes de soporte y consulte evidencia técnica local.";
    public IReadOnlyList<SupportChannel> Channels { get; }
    public ObservableCollection<LogFileSummary> Logs { get; }
    public string HardwareId { get => _hardwareId; private set => SetProperty(ref _hardwareId, value); }
    public bool HardwareAvailable { get => _hardwareAvailable; private set { if (SetProperty(ref _hardwareAvailable, value)) NotifyCommands(); } }
    public string ErrorSummary { get => _errorSummary; set => SetProperty(ref _errorSummary, value ?? string.Empty); }
    public string ErrorDetail { get => _errorDetail; set => SetProperty(ref _errorDetail, value ?? string.Empty); }
    public SupportChannel SelectedChannel { get => _selectedChannel; set => SetProperty(ref _selectedChannel, value); }
    public SupportMailDraft? CurrentDraft { get => _currentDraft; private set { if (SetProperty(ref _currentDraft, value)) NotifyCommands(); } }
    public LogFileSummary? SelectedLog { get => _selectedLog; set { if (SetProperty(ref _selectedLog, value)) NotifyCommands(); } }
    public string LogPreview { get => _logPreview; private set => SetProperty(ref _logPreview, value); }
    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }
    public bool IsBusy { get => _isBusy; private set { if (SetProperty(ref _isBusy, value)) NotifyCommands(); } }

    public AsyncRelayCommand PrepareLicenseCommand { get; }
    public AsyncRelayCommand PrepareErrorCommand { get; }
    public AsyncRelayCommand OpenDraftCommand { get; }
    public AsyncRelayCommand RefreshLogsCommand { get; }
    public AsyncRelayCommand LoadSelectedLogCommand { get; }
    public AsyncRelayCommand OpenLogFolderCommand { get; }
    public RelayCommand CopyHardwareIdCommand { get; }

    public async Task ActivateAsync(CancellationToken cancellationToken = default)
    {
        _lifecycle?.Cancel(); _lifecycle?.Dispose();
        _lifecycle = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var generation = Interlocked.Increment(ref _generation);
        await RunBusyAsync(async token =>
        {
            var initialized = await _workflow.InitializeAsync(token);
            if (!IsCurrent(generation)) return;
            HardwareAvailable = initialized.HardwareAvailable;
            HardwareId = initialized.HardwareId;
            StatusText = initialized.Message;
            await RefreshLogsCoreAsync(token, generation);
        }, _lifecycle.Token, generation);
    }

    public void Deactivate()
    {
        _lifecycle?.Cancel(); _lifecycle?.Dispose(); _lifecycle = null;
        Interlocked.Increment(ref _generation);
        _workflow.Invalidate();
        CurrentDraft = null; LogPreview = string.Empty; IsBusy = false;
    }

    private async Task PrepareLicenseAsync()
    {
        var generation = Volatile.Read(ref _generation);
        await RunBusyAsync(async token =>
        {
            var result = await _workflow.PrepareLicenseRequestAsync(token);
            if (IsCurrent(generation)) Apply(result);
        }, generation: generation);
    }

    private Task PrepareErrorAsync()
    {
        var result = _workflow.PrepareErrorReport(ErrorSummary, ErrorDetail);
        Apply(result);
        return Task.CompletedTask;
    }

    private async Task OpenDraftAsync()
    {
        var generation = Volatile.Read(ref _generation);
        await RunBusyAsync(async token =>
        {
            if (CurrentDraft is null) return;
            var result = await _workflow.OpenDraftAsync(CurrentDraft, SelectedChannel, token);
            if (IsCurrent(generation)) StatusText = result.Message;
        }, generation: generation);
    }

    private async Task RefreshLogsAsync()
    {
        var generation = Volatile.Read(ref _generation);
        await RunBusyAsync(token => RefreshLogsCoreAsync(token, generation), generation: generation);
    }
    private async Task RefreshLogsCoreAsync(CancellationToken token, long generation)
    {
        var previous = SelectedLog?.Id;
        var result = await _workflow.GetLogsAsync(token);
        if (!IsCurrent(generation)) return;
        Logs.Clear();
        foreach (var file in result.Files) Logs.Add(file);
        SelectedLog = Logs.FirstOrDefault(file => string.Equals(file.Id, previous, StringComparison.OrdinalIgnoreCase));
        StatusText = result.Status switch
        {
            LogCatalogStatus.Available => $"{Logs.Count} log(s) disponible(s).",
            LogCatalogStatus.Empty => "La carpeta de logs está vacía.",
            LogCatalogStatus.DirectoryUnavailable => "La carpeta de logs todavía no existe.",
            LogCatalogStatus.AccessDenied => "No fue posible acceder a los logs.",
            _ => "No fue posible consultar los logs."
        };
    }

    private async Task LoadSelectedLogAsync()
    {
        var generation = Volatile.Read(ref _generation);
        await RunBusyAsync(async token =>
        {
            if (SelectedLog is null) return;
            var result = await _workflow.ReadLogAsync(SelectedLog.Id, token);
            if (!IsCurrent(generation)) return;
            LogPreview = result.Status == LogPreviewStatus.Loaded ? result.Content : string.Empty;
            StatusText = result.Status == LogPreviewStatus.Loaded
                ? (result.IsTruncated ? "Vista previa cargada y acotada." : "Vista previa cargada.")
                : "No fue posible leer el log seleccionado.";
        }, generation: generation);
    }

    private async Task OpenLogFolderAsync()
    {
        var generation = Volatile.Read(ref _generation);
        await RunBusyAsync(async token =>
        {
            var result = await _workflow.OpenLogFolderAsync(token);
            if (IsCurrent(generation)) StatusText = result.Message;
        }, generation: generation);
    }

    private void CopyHardwareId()
    {
        StatusText = HardwareAvailable && _clipboard.TrySetText(HardwareId)
            ? "Identificador copiado."
            : "No fue posible copiar el identificador.";
    }

    private void Apply(SupportActionResult result)
    {
        CurrentDraft = result.Draft;
        StatusText = result.Message;
    }

    private Task RunBusyAsync(Func<CancellationToken, Task> operation, long generation) => RunBusyAsync(operation, _lifecycle?.Token ?? CancellationToken.None, generation);
    private async Task RunBusyAsync(Func<CancellationToken, Task> operation, CancellationToken token, long generation)
    {
        if (IsBusy) return;
        IsBusy = true;
        try { await operation(token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        finally { if (IsCurrent(generation)) IsBusy = false; }
    }

    private bool IsCurrent(long generation) => generation == Volatile.Read(ref _generation);

    private void NotifyCommands()
    {
        PrepareLicenseCommand.NotifyCanExecuteChanged(); PrepareErrorCommand.NotifyCanExecuteChanged();
        OpenDraftCommand.NotifyCanExecuteChanged(); RefreshLogsCommand.NotifyCanExecuteChanged();
        LoadSelectedLogCommand.NotifyCanExecuteChanged(); OpenLogFolderCommand.NotifyCanExecuteChanged();
        CopyHardwareIdCommand.NotifyCanExecuteChanged();
    }
}
