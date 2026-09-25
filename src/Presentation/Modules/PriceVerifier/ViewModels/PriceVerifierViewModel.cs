using Microsoft.Extensions.Logging;
using SysTools.Business.PriceVerifier;
using SysTools.Business.Labels;
using SysTools.Entities.Labels;
using SysTools.Entities.PriceVerifier;
using SysTools.Entities.Products;
using SysTools.Presentation.Commands;
using SysTools.Presentation.Modules.PriceVerifier.Search;
using SysTools.Presentation.Modules.Configuration.Services;
using SysTools.Presentation.Modules.Labels;
using SysTools.Presentation.Shell.Models;
using SysTools.Presentation.Shell.Services;
using SysTools.Presentation.ViewModels;

namespace SysTools.Presentation.Modules.PriceVerifier.ViewModels;

public sealed class PriceVerifierViewModel : ViewModelBase, IAsyncModuleLifecycle
{
    private const string Neutral = "—";
    private readonly IPriceVerifierWorkflow _workflow;
    private readonly IProductSearchDialogService _searchDialog;
    private readonly ILogger<PriceVerifierViewModel> _logger;
    private readonly IConfigurationDialogService? _configurationDialog;
    private readonly ILabelOutputWorkflow? _labelOutput;
    private readonly ILabelPreviewDialogService? _labelPreview;
    private CancellationTokenSource? _lifecycleCancellation;
    private long _generation;
    private string _barcode = string.Empty;
    private string _additionalInformation = string.Empty;
    private string _productDescription = Neutral;
    private string _productPresentation = Neutral;
    private string _stockDisplay = Neutral;
    private string _finalPriceDisplay = Neutral;
    private bool _isBusy;
    private bool _isReady;
    private bool _isSearchOpen;
    private int _focusRequestVersion;
    private Product? _currentProduct;
    private string? _currentFormattedPrice;
    private bool _currentProductCaptured, _hasPendingLabels, _canRetryLabelOutput;
    private string _labelProgressText = "Sin etiquetas pendientes.";
    private AvailabilityStatus _connectionStatus = AvailabilityStatus.Unavailable;
    private AvailabilityStatus _licenseStatus = AvailabilityStatus.Unavailable;
    private OperationalMessage _statusMessage = new(
        "Módulo pendiente de preparación.", MessageSeverity.Information);

    public PriceVerifierViewModel(
        IPriceVerifierWorkflow workflow,
        IProductSearchDialogService searchDialog,
        ILogger<PriceVerifierViewModel> logger,
        IConfigurationDialogService? configurationDialog = null,
        ILabelOutputWorkflow? labelOutput = null,
        ILabelPreviewDialogService? labelPreview = null)
    {
        _workflow = workflow;
        _searchDialog = searchDialog;
        _logger = logger;
        _configurationDialog = configurationDialog;
        _labelOutput = labelOutput;
        _labelPreview = labelPreview;
        RetryCommand = new AsyncRelayCommand(PrepareForCurrentLifecycleAsync, () => CanRetry);
        SubmitBarcodeCommand = new AsyncRelayCommand(SubmitBarcodeAsync, () => IsBarcodeAvailable);
        OpenSearchCommand = new AsyncRelayCommand(OpenSearchAsync, () => IsSearchAvailable);
        OpenConfigurationCommand = new AsyncRelayCommand(OpenConfigurationAsync, () => IsSettingsAvailable);
        CaptureLabelCommand = new AsyncRelayCommand(CaptureLabelAsync, () => IsPrintAvailable);
        RetryLabelOutputCommand = new AsyncRelayCommand(RetryLabelOutputAsync, () => CanRetryLabelOutput);
        CancelPendingLabelsCommand = new RelayCommand(_ => CancelPendingLabels(), _ => CanCancelPendingLabels);
    }

    public string ModuleTitle => "Verificador de precios";
    public string ModuleDescription => "Consulte descripción, presentación, existencia y precio por código de barras.";

    public string Barcode
    {
        get => _barcode;
        set => SetProperty(ref _barcode, value ?? string.Empty);
    }

    public string AdditionalInformation
    {
        get => _additionalInformation;
        private set
        {
            if (SetProperty(ref _additionalInformation, value))
            {
                OnPropertyChanged(nameof(IsAdditionalInformationAvailable));
            }
        }
    }

    public string ProductDescription { get => _productDescription; private set => SetProperty(ref _productDescription, value); }
    public string ProductPresentation { get => _productPresentation; private set => SetProperty(ref _productPresentation, value); }
    public string StockDisplay { get => _stockDisplay; private set => SetProperty(ref _stockDisplay, value); }
    public string FinalPriceDisplay { get => _finalPriceDisplay; private set => SetProperty(ref _finalPriceDisplay, value); }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                NotifyAvailabilityChanged();
            }
        }
    }

    public bool IsAvailable => _isReady && !IsBusy;
    public bool IsBarcodeAvailable => IsAvailable;
    public bool IsAdditionalInformationAvailable => AdditionalInformation.Length > 0;
    public bool IsSearchAvailable => IsAvailable && !_isSearchOpen;
    public bool IsPrintAvailable => IsAvailable && _currentProduct is not null && !_currentProductCaptured && _labelOutput is not null;
    public bool CanRetryLabelOutput => IsAvailable && _canRetryLabelOutput && _labelOutput is not null;
    public bool CanCancelPendingLabels => !IsBusy && _hasPendingLabels && _labelOutput is not null;
    public string LabelProgressText { get => _labelProgressText; private set => SetProperty(ref _labelProgressText, value); }
    public bool IsSettingsAvailable => !IsBusy && _configurationDialog is not null;
    public bool CanRetry => !_isReady && !IsBusy;

    public int FocusRequestVersion { get => _focusRequestVersion; private set => SetProperty(ref _focusRequestVersion, value); }
    public AvailabilityStatus ConnectionStatus { get => _connectionStatus; private set => SetProperty(ref _connectionStatus, value); }
    public AvailabilityStatus LicenseStatus { get => _licenseStatus; private set => SetProperty(ref _licenseStatus, value); }
    public string ConnectionStatusText => $"Conexión: {AvailabilityText(ConnectionStatus)}";
    public string LicenseStatusText => $"Licencia: {AvailabilityText(LicenseStatus)}";

    public OperationalMessage StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public AsyncRelayCommand RetryCommand { get; }
    public AsyncRelayCommand SubmitBarcodeCommand { get; }
    public AsyncRelayCommand OpenSearchCommand { get; }
    public AsyncRelayCommand OpenConfigurationCommand { get; }
    public AsyncRelayCommand CaptureLabelCommand { get; }
    public AsyncRelayCommand RetryLabelOutputCommand { get; }
    public RelayCommand CancelPendingLabelsCommand { get; }

    public async Task ActivateAsync(CancellationToken cancellationToken = default)
    {
        _lifecycleCancellation?.Cancel();
        _lifecycleCancellation?.Dispose();
        _lifecycleCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var generation = Interlocked.Increment(ref _generation);
        await PrepareAsync(generation, _lifecycleCancellation.Token);
    }

    public void Deactivate()
    {
        Interlocked.Increment(ref _generation);
        _lifecycleCancellation?.Cancel();
        _lifecycleCancellation?.Dispose();
        _lifecycleCancellation = null;
        _searchDialog.CloseActive();
        _labelPreview?.CloseActive();
        _labelOutput?.Invalidate();
        _isSearchOpen = false;
        _hasPendingLabels = false;
        _canRetryLabelOutput = false;
        _workflow.Invalidate();
        _isReady = false;
        IsBusy = false;
        ClearProduct();
        AdditionalInformation = string.Empty;
        ConnectionStatus = AvailabilityStatus.Unavailable;
        LicenseStatus = AvailabilityStatus.Unavailable;
        StatusMessage = new OperationalMessage("Módulo desactivado.", MessageSeverity.Information);
        NotifyStatusTextChanged();
        NotifyAvailabilityChanged();
    }

    private Task PrepareForCurrentLifecycleAsync()
    {
        _lifecycleCancellation ??= new CancellationTokenSource();
        var generation = Interlocked.Increment(ref _generation);
        return PrepareAsync(generation, _lifecycleCancellation.Token);
    }

    private async Task PrepareAsync(long generation, CancellationToken cancellationToken)
    {
        _isReady = false;
        IsBusy = true;
        ClearProduct();
        AdditionalInformation = string.Empty;
        ConnectionStatus = AvailabilityStatus.Pending;
        LicenseStatus = AvailabilityStatus.Pending;
        StatusMessage = new OperationalMessage("Preparando el verificador…", MessageSeverity.Information);
        NotifyStatusTextChanged();

        try
        {
            var result = await _workflow.PrepareAsync(cancellationToken);
            if (!IsCurrent(generation, cancellationToken)) return;
            PublishPreparation(result);
            _logger.LogInformation("Price verifier completed at {Stage} with {Status}", "Preparation", result.Status);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        finally
        {
            if (generation == Volatile.Read(ref _generation))
            {
                IsBusy = false;
                if (_isReady) FocusRequestVersion++;
            }
        }
    }

    private async Task SubmitBarcodeAsync()
    {
        var cancellationToken = _lifecycleCancellation?.Token ?? CancellationToken.None;
        var generation = Volatile.Read(ref _generation);
        if (!IsAvailable || cancellationToken.IsCancellationRequested) return;

        IsBusy = true;
        ClearProduct();
        StatusMessage = new OperationalMessage("Consultando producto…", MessageSeverity.Information);
        try
        {
            var result = await _workflow.LookupAsync(Barcode, cancellationToken);
            if (!IsCurrent(generation, cancellationToken)) return;
            PublishLookup(result);
            _logger.LogInformation("Price verifier completed at {Stage} with {Status}", "Lookup", result.Status);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        finally
        {
            if (generation == Volatile.Read(ref _generation))
            {
                IsBusy = false;
                if (_isReady) FocusRequestVersion++;
            }
        }
    }

    private async Task OpenSearchAsync()
    {
        var cancellationToken = _lifecycleCancellation?.Token ?? CancellationToken.None;
        var generation = Volatile.Read(ref _generation);
        if (!IsSearchAvailable || cancellationToken.IsCancellationRequested) return;

        _isSearchOpen = true;
        NotifyAvailabilityChanged();
        ProductSearchDialogResult result;
        try
        {
            result = _searchDialog.ShowDialog(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        finally
        {
            _isSearchOpen = false;
            NotifyAvailabilityChanged();
        }

        if (!IsCurrent(generation, cancellationToken)) return;
        if (result.Outcome == ProductSearchDialogOutcome.Selected)
        {
            Barcode = result.SelectedBarcode!;
            await SubmitBarcodeAsync();
        }
        else if (result.Outcome == ProductSearchDialogOutcome.OperationalFailure)
        {
            _isReady = false;
            ClearProduct();
            ConnectionStatus = AvailabilityStatus.Error;
            LicenseStatus = AvailabilityStatus.Unavailable;
            StatusMessage = new OperationalMessage(
                "No fue posible consultar el catalogo. Reintente la preparacion.",
                MessageSeverity.Error);
            NotifyStatusTextChanged();
            NotifyAvailabilityChanged();
        }
        else
        {
            FocusRequestVersion++;
        }
    }

    private async Task OpenConfigurationAsync()
    {
        if (_configurationDialog is null || IsBusy) return;
        var token = _lifecycleCancellation?.Token ?? CancellationToken.None;
        var result = await _configurationDialog.ShowDialogAsync(ConfigurationDialogMode.Edit, token);
        if (result.IsSaved && !token.IsCancellationRequested)
        {
            var generation = Interlocked.Increment(ref _generation);
            await PrepareAsync(generation, token);
        }
        else if (!token.IsCancellationRequested) FocusRequestVersion++;
    }

    private async Task CaptureLabelAsync()
    {
        if (!IsPrintAvailable || _labelOutput is null || _currentProduct is null || _currentFormattedPrice is null) return;
        var token = _lifecycleCancellation?.Token ?? CancellationToken.None;
        var generation = Volatile.Read(ref _generation);
        IsBusy = true;
        try
        {
            var result = await _labelOutput.CaptureAsync(_currentProduct, _currentFormattedPrice, token);
            if (!IsCurrent(generation, token)) return;
            if (result.Status is not (LabelOutputStatus.Busy or LabelOutputStatus.InvalidProduct or LabelOutputStatus.ConfigurationUnavailable))
                _currentProductCaptured = true;
            await PublishLabelOutputAsync(result, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        finally { if (generation == Volatile.Read(ref _generation)) { IsBusy = false; FocusRequestVersion++; } }
    }

    private async Task RetryLabelOutputAsync()
    {
        if (!CanRetryLabelOutput || _labelOutput is null) return;
        var token = _lifecycleCancellation?.Token ?? CancellationToken.None;
        var generation = Volatile.Read(ref _generation);
        IsBusy = true;
        try
        {
            var result = await _labelOutput.RetryAsync(token);
            if (!IsCurrent(generation, token)) return;
            await PublishLabelOutputAsync(result, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        finally { if (generation == Volatile.Read(ref _generation)) { IsBusy = false; FocusRequestVersion++; } }
    }

    private async Task PublishLabelOutputAsync(LabelOutputResult result, CancellationToken token)
    {
        _canRetryLabelOutput = result.CanRetry;
        _hasPendingLabels = result.Status == LabelOutputStatus.Pending || result.CanRetry;
        LabelProgressText = result.Status == LabelOutputStatus.Pending
            ? $"Fila de etiquetas: {result.Pending}/{result.Capacity}; faltan {result.Remaining}."
            : result.Message;
        StatusMessage = new OperationalMessage(result.Message,
            result.Status is LabelOutputStatus.Pending or LabelOutputStatus.PreviewReady or LabelOutputStatus.Printed
                ? MessageSeverity.Information : MessageSeverity.Warning);
        NotifyAvailabilityChanged();
        if (result.Status == LabelOutputStatus.PreviewReady && result.Preview is not null && _labelPreview is not null)
        {
            await _labelPreview.ShowAsync(result.Preview, token);
            _hasPendingLabels = false; _canRetryLabelOutput = false;
            LabelProgressText = "Vista previa cerrada; fila completada.";
            NotifyAvailabilityChanged();
        }
        else if (result.Status == LabelOutputStatus.Printed)
        {
            _hasPendingLabels = false; _canRetryLabelOutput = false;
            NotifyAvailabilityChanged();
        }
    }

    private void CancelPendingLabels()
    {
        if (_labelOutput is null || !CanCancelPendingLabels) return;
        var result = _labelOutput.Cancel();
        _hasPendingLabels = false; _canRetryLabelOutput = false;
        LabelProgressText = result.HadCompletedRow
            ? "Fila completada descartada."
            : $"Etiquetas pendientes descartadas: {result.Discarded}.";
        StatusMessage = new OperationalMessage(LabelProgressText, MessageSeverity.Information);
        NotifyAvailabilityChanged(); FocusRequestVersion++;
    }

    private void PublishPreparation(PriceVerifierPreparationResult result)
    {
        _isReady = result.IsReady;
        AdditionalInformation = result.IsReady ? result.AdditionalInformation : string.Empty;
        ConnectionStatus = result.Status switch
        {
            PriceVerifierPreparationStatus.Ready or PriceVerifierPreparationStatus.LicenseUnavailable => AvailabilityStatus.Available,
            _ => AvailabilityStatus.Error
        };
        LicenseStatus = result.Status switch
        {
            PriceVerifierPreparationStatus.Ready => AvailabilityStatus.Available,
            PriceVerifierPreparationStatus.LicenseUnavailable => AvailabilityStatus.Error,
            _ => AvailabilityStatus.Unavailable
        };
        StatusMessage = new OperationalMessage(result.Message, result.IsReady ? MessageSeverity.Information : MessageSeverity.Error);
        NotifyStatusTextChanged();
        NotifyAvailabilityChanged();
    }

    private void PublishLookup(PriceVerifierLookupResult result)
    {
        if (result.Status == PriceVerifierLookupStatus.Success)
        {
            var product = result.Product!;
            _currentProduct = product;
            _currentFormattedPrice = result.FormattedPrice;
            _currentProductCaptured = false;
            ProductDescription = Neutralize(product.Description);
            ProductPresentation = Neutralize(product.Presentation);
            StockDisplay = Neutralize(product.Stock);
            FinalPriceDisplay = Neutralize(result.FormattedPrice);
            StatusMessage = new OperationalMessage(result.Message, MessageSeverity.Information);
            NotifyAvailabilityChanged();
            return;
        }

        ClearProduct();
        if (result.Status == PriceVerifierLookupStatus.OperationalFailure)
        {
            _isReady = false;
            ConnectionStatus = AvailabilityStatus.Error;
            LicenseStatus = AvailabilityStatus.Unavailable;
            NotifyStatusTextChanged();
            NotifyAvailabilityChanged();
        }

        StatusMessage = new OperationalMessage(
            result.Message,
            result.Status == PriceVerifierLookupStatus.OperationalFailure ? MessageSeverity.Error : MessageSeverity.Warning);
    }

    private bool IsCurrent(long generation, CancellationToken token) =>
        generation == Volatile.Read(ref _generation) && !token.IsCancellationRequested;

    private void ClearProduct()
    {
        _currentProduct = null;
        _currentFormattedPrice = null;
        _currentProductCaptured = false;
        ProductDescription = Neutral;
        ProductPresentation = Neutral;
        StockDisplay = Neutral;
        FinalPriceDisplay = Neutral;
    }

    private void NotifyAvailabilityChanged()
    {
        OnPropertyChanged(nameof(IsAvailable));
        OnPropertyChanged(nameof(IsBarcodeAvailable));
        OnPropertyChanged(nameof(IsSearchAvailable));
        OnPropertyChanged(nameof(CanRetry));
        OnPropertyChanged(nameof(IsSettingsAvailable));
        OnPropertyChanged(nameof(IsPrintAvailable));
        OnPropertyChanged(nameof(CanRetryLabelOutput));
        OnPropertyChanged(nameof(CanCancelPendingLabels));
        RetryCommand.NotifyCanExecuteChanged();
        SubmitBarcodeCommand.NotifyCanExecuteChanged();
        OpenSearchCommand.NotifyCanExecuteChanged();
        OpenConfigurationCommand.NotifyCanExecuteChanged();
        CaptureLabelCommand.NotifyCanExecuteChanged();
        RetryLabelOutputCommand.NotifyCanExecuteChanged();
        CancelPendingLabelsCommand.NotifyCanExecuteChanged();
    }

    private void NotifyStatusTextChanged()
    {
        OnPropertyChanged(nameof(ConnectionStatusText));
        OnPropertyChanged(nameof(LicenseStatusText));
    }

    private static string Neutralize(string? value) => string.IsNullOrWhiteSpace(value) ? Neutral : value;
    private static string AvailabilityText(AvailabilityStatus status) => status switch
    {
        AvailabilityStatus.Pending => "Preparando",
        AvailabilityStatus.Available => "Disponible",
        AvailabilityStatus.Error => "Error",
        _ => "No disponible"
    };
}
