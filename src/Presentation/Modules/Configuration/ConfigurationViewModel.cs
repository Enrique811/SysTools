using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using SysTools.Business.ConfigurationEditor;
using SysTools.Entities.Configuration;
using SysTools.Entities.ConfigurationEditor;
using SysTools.Presentation.Modules.Configuration.Services;
using SysTools.Presentation.ViewModels;

namespace SysTools.Presentation.Modules.Configuration;

public sealed class ConfigurationViewModel : ViewModelBase
{
    private readonly IConfigurationEditorWorkflow _workflow;
    private readonly IConfigurationFilePicker _picker;
    private readonly IClipboardService _clipboard;
    private readonly IConfirmationService _confirmation;
    private readonly ILogger<ConfigurationViewModel> _logger;
    private CancellationTokenSource? _lifecycle;
    private ConfigurationDraft _initial = new();
    private string _ipEmpresa = string.Empty, _rutaEmpresa = string.Empty, _usuario = "SYSDBA";
    private string _ambiente = "a", _impresora = string.Empty, _formatoPrecio = "MX";
    private string _reporte = string.Empty, _informacion = string.Empty, _statusMessage = "Cargando configuración...";
    private string _licenseStatus = "No se ha validado una licencia nueva.";
    private int _columnas = 1;
    private bool _isBusy, _hasStoredPassword, _hasStoredLicense, _hasNewPassword, _isRecoveryRequired;
    private bool _requiresInitialAttention = true;
    private long _revision;
    private string? _proofToken, _hardwareId;
    private LicenseChange _licenseChange = LicenseChange.KeepExisting;

    public ConfigurationViewModel(
        IConfigurationEditorWorkflow workflow,
        IConfigurationFilePicker picker,
        IClipboardService clipboard,
        IConfirmationService confirmation,
        ILogger<ConfigurationViewModel> logger)
    {
        _workflow = workflow; _picker = picker; _clipboard = clipboard; _confirmation = confirmation; _logger = logger;
    }

    public event EventHandler<ConfigurationDialogResult>? CloseRequested;
    public ObservableCollection<OperationalOption> Printers { get; } = [];
    public ObservableCollection<OperationalOption> Templates { get; } = [];
    public ObservableCollection<string> ValidationIssues { get; } = [];
    public IReadOnlyList<int> ColumnOptions { get; } = [1, 2, 3];

    public string IpEmpresa { get => _ipEmpresa; set => SetConnection(ref _ipEmpresa, value); }
    public string RutaEmpresa { get => _rutaEmpresa; set => SetConnection(ref _rutaEmpresa, value); }
    public string Usuario { get => _usuario; set => SetConnection(ref _usuario, value); }
    public string Ambiente { get => _ambiente; set => SetDirty(ref _ambiente, value); }
    public string Impresora { get => _impresora; set => SetDirty(ref _impresora, value); }
    public string FormatoPrecio { get => _formatoPrecio; set => SetDirty(ref _formatoPrecio, value); }
    public string Reporte { get => _reporte; set => SetDirty(ref _reporte, value); }
    public string Informacion { get => _informacion; set => SetDirty(ref _informacion, value); }
    public int Columnas { get => _columnas; set { if (SetProperty(ref _columnas, value)) { ValidationIssues.Clear(); NotifyDirty(); } } }
    public bool HasStoredPassword { get => _hasStoredPassword; private set { if (SetProperty(ref _hasStoredPassword, value)) OnPropertyChanged(nameof(CredentialStatus)); } }
    public bool HasStoredLicense { get => _hasStoredLicense; private set => SetProperty(ref _hasStoredLicense, value); }
    public bool IsRecoveryRequired { get => _isRecoveryRequired; private set => SetProperty(ref _isRecoveryRequired, value); }
    public bool RequiresInitialAttention { get => _requiresInitialAttention; private set => SetProperty(ref _requiresInitialAttention, value); }
    public string? HardwareId { get => _hardwareId; private set => SetProperty(ref _hardwareId, value); }
    public string CredentialStatus => HasStoredPassword ? "Existe una credencial guardada. Déjelo vacío para conservarla." : "Capture una contraseña.";
    public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }
    public string LicenseStatus { get => _licenseStatus; private set => SetProperty(ref _licenseStatus, value); }
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value)) OnPropertyChanged(nameof(IsIdle));
        }
    }
    public bool IsIdle => !IsBusy;
    public bool IsDirty => _hasNewPassword || _licenseChange != LicenseChange.KeepExisting || Draft() != _initial;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _lifecycle?.Cancel(); _lifecycle?.Dispose();
        _lifecycle = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var operationToken = _lifecycle.Token;
        IsBusy = true;
        try
        {
            var snapshot = await _workflow.OpenAsync(operationToken);
            operationToken.ThrowIfCancellationRequested();
            Apply(snapshot.Draft);
            _initial = snapshot.Draft;
            HasStoredPassword = snapshot.HasStoredPassword;
            HasStoredLicense = snapshot.HasStoredLicense;
            LicenseStatus = snapshot.HasStoredLicense
                ? "Existe una referencia de licencia guardada."
                : "No hay una licencia guardada.";
            IsRecoveryRequired = snapshot.Status == ConfigurationEditorOpenStatus.RecoveryRequired;
            RequiresInitialAttention = snapshot.Status != ConfigurationEditorOpenStatus.Ready || snapshot.RequiresConnectionTest;
            HardwareId = snapshot.Hardware.Value;
            Replace(Printers, snapshot.Printers); Replace(Templates, snapshot.Templates);
            StatusMessage = snapshot.Status switch
            {
                ConfigurationEditorOpenStatus.Ready => "Configuración lista para editar.",
                ConfigurationEditorOpenStatus.RecoveryRequired => "La configuración requiere recuperación.",
                _ => "No fue posible cargar la configuración."
            };
        }
        catch (OperationCanceledException) when (operationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            ReportFailure("Open", exception, "No fue posible cargar la configuración.");
        }
        finally { IsBusy = false; }
    }

    public void NotifyPasswordChanged(bool hasValue)
    {
        _hasNewPassword = hasValue; _revision++; _proofToken = null; ValidationIssues.Clear(); NotifyDirty();
    }

    public async Task TestConnectionAsync(string password)
    {
        if (IsBusy) return;
        var operationToken = Token;
        IsBusy = true; StatusMessage = "Probando conexión...";
        try
        {
            var result = await _workflow.TestConnectionAsync(Draft(), password, _revision, operationToken);
            operationToken.ThrowIfCancellationRequested();
            _proofToken = result.ProofToken;
            StatusMessage = result.Result.Message;
        }
        catch (OperationCanceledException) when (operationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            ReportFailure("ConnectionTest", exception, "No fue posible probar la conexión.");
        }
        finally { IsBusy = false; }
    }

    public async Task SaveAsync(string password)
    {
        if (IsBusy) return;
        var operationToken = Token;
        IsBusy = true; StatusMessage = "Guardando configuración...";
        try
        {
            ValidationIssues.Clear();
            var result = IsRecoveryRequired
                ? await _workflow.RecoverAsync(Draft(), password, _revision, _proofToken, _confirmation.ConfirmRecovery(), operationToken)
                : await _workflow.SaveAsync(Draft(), password, _revision, _proofToken, _licenseChange, operationToken);
            operationToken.ThrowIfCancellationRequested();
            StatusMessage = result.Message;
            ReplaceIssues(result.Issues);
            if (result.IsSaved)
            {
                _hasNewPassword = false;
                _licenseChange = LicenseChange.KeepExisting;
                _initial = Draft();
                CloseRequested?.Invoke(this, new ConfigurationDialogResult(ConfigurationDialogOutcome.Saved));
            }
        }
        catch (OperationCanceledException) when (operationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            ReportFailure("Save", exception, "No fue posible guardar la configuración.");
        }
        finally { IsBusy = false; }
    }

    public void BrowseDatabase()
    {
        if (IsBusy) return;
        var value = _picker.PickDatabase(); if (value is not null) RutaEmpresa = value;
    }

    public async Task ImportLicenseAsync(string password)
    {
        if (IsBusy) return;
        var value = _picker.PickLicense(); if (value is null) return;
        var operationToken = Token;
        IsBusy = true;
        try
        {
            var result = await _workflow.ValidateLicenseAsync(Draft(), password, value, _revision, operationToken);
            operationToken.ThrowIfCancellationRequested();
            StatusMessage = result.Summary.Message;
            LicenseStatus = FormatLicenseSummary(result.Summary);
            if (result.Summary.IsValid) { HasStoredLicense = true; _licenseChange = LicenseChange.ReplaceValidated; NotifyDirty(); }
        }
        catch (OperationCanceledException) when (operationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            ReportFailure("LicenseValidation", exception, "No fue posible validar la licencia.");
        }
        finally { IsBusy = false; }
    }

    public void CopyHardwareId()
    {
        StatusMessage = HardwareId is not null && _clipboard.TrySetText(HardwareId)
            ? "Identificador copiado." : "No fue posible copiar el identificador.";
    }

    public bool RequestCancel()
    {
        if (!CanDiscardChanges()) return false;
        CloseRequested?.Invoke(this, new ConfigurationDialogResult(ConfigurationDialogOutcome.Canceled));
        return true;
    }

    public bool CanDiscardChanges() => !IsDirty || _confirmation.ConfirmDiscard();

    public void Deactivate()
    {
        _lifecycle?.Cancel(); _lifecycle?.Dispose(); _lifecycle = null; _workflow.Invalidate();
    }

    private CancellationToken Token => _lifecycle?.Token ?? CancellationToken.None;
    private void SetConnection(ref string field, string? value)
    { if (SetProperty(ref field, value ?? string.Empty)) { _revision++; _proofToken = null; ValidationIssues.Clear(); NotifyDirty(); } }
    private void SetDirty(ref string field, string? value)
    { if (SetProperty(ref field, value ?? string.Empty)) { ValidationIssues.Clear(); NotifyDirty(); } }
    private void NotifyDirty() => OnPropertyChanged(nameof(IsDirty));
    private void ReportFailure(string stage, Exception exception, string message)
    {
        _logger.LogWarning(
            "Configuration UI operation failed at {Stage} with {Category}",
            stage,
            exception.GetType().Name);
        StatusMessage = message;
    }
    private ConfigurationDraft Draft() => new() { IpEmpresa=IpEmpresa, RutaEmpresa=RutaEmpresa, Usuario=Usuario, Ambiente=Ambiente, Impresora=Impresora, FormatoPrecio=FormatoPrecio, Reporte=Reporte, Columnas=Columnas, Informacion=Informacion };
    private void Apply(ConfigurationDraft d) { _ipEmpresa=d.IpEmpresa; _rutaEmpresa=d.RutaEmpresa; _usuario=d.Usuario; _ambiente=d.Ambiente; _impresora=d.Impresora; _formatoPrecio=d.FormatoPrecio; _reporte=d.Reporte; _columnas=d.Columnas; _informacion=d.Informacion; OnPropertyChanged(string.Empty); }
    private static void Replace(ObservableCollection<OperationalOption> target, IEnumerable<OperationalOption> values) { target.Clear(); foreach (var value in values) target.Add(value); }
    private void ReplaceIssues(IEnumerable<ConfigurationIssue> issues)
    {
        ValidationIssues.Clear();
        foreach (var issue in issues)
        {
            var field = issue.Field switch
            {
                "ipEmpresa" => "Servidor",
                "rutaEmpresa" => "Base de datos",
                "usuario" => "Usuario",
                "password" => "Contraseña",
                "ambiente" => "Ambiente",
                "formatoPrecio" => "Formato de precio",
                "columnas" => "Columnas",
                _ => "Configuración"
            };
            ValidationIssues.Add($"{field}: {issue.Message}");
        }
    }

    private static string FormatLicenseSummary(LicenseSummary summary)
    {
        if (!summary.IsValid) return summary.Message;
        var from = summary.ValidFrom?.ToString("yyyy-MM-dd") ?? "sin inicio";
        var until = summary.ValidUntil?.ToString("yyyy-MM-dd") ?? "sin vencimiento";
        return $"{summary.Message} Emisor: {summary.Issuer}. Vigencia: {from} a {until}.";
    }
}
