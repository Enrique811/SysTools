using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using SysTools.Presentation.Commands;
using SysTools.Presentation.Modules.PriceVerifier.ViewModels;
using SysTools.Presentation.Shell.Models;
using SysTools.Presentation.Shell.Services;
using SysTools.Presentation.ViewModels;

namespace SysTools.Presentation.Shell.ViewModels;

public sealed class ShellViewModel : ViewModelBase
{
    public const string PriceVerifierModuleId = "price-verifier";

    private readonly ILogger<ShellViewModel> _logger;
    private readonly PriceVerifierViewModel _priceVerifier;
    private UtilityModuleItem _activeModule;

    public ShellViewModel(
        PriceVerifierViewModel priceVerifier,
        IModuleInitializer moduleInitializer,
        ILogger<ShellViewModel> logger)
    {
        _logger = logger;
        _priceVerifier = priceVerifier;
        Modules = new ObservableCollection<UtilityModuleItem>(CreateInitialModules(priceVerifier));
        ValidateUniqueIds(Modules);

        _activeModule = Modules.Single(module => module.Id == PriceVerifierModuleId);
        SelectModuleCommand = new RelayCommand(
            parameter => SelectModule(parameter as UtilityModuleItem),
            parameter => parameter is UtilityModuleItem module && module.IsEnabled);

        InitializeActiveModule(moduleInitializer);
        _priceVerifier.PropertyChanged += HandlePriceVerifierPropertyChanged;
    }

    public string ApplicationName => "SysTools";

    public string ApplicationSubtitle => "Suite operativa";

    public ObservableCollection<UtilityModuleItem> Modules { get; }

    public IEnumerable<UtilityModuleItem> UtilityModules => Modules.Where(x => x.Section == ModuleSection.Utilities);

    public IEnumerable<UtilityModuleItem> AdministrationModules => Modules.Where(x => x.Section == ModuleSection.Administration);

    public UtilityModuleItem ActiveModule
    {
        get => _activeModule;
        private set
        {
            if (SetProperty(ref _activeModule, value))
            {
                OnPropertyChanged(nameof(ActiveModuleContent));
            }
        }
    }

    public ViewModelBase ActiveModuleContent => ActiveModule.Content
        ?? throw new InvalidOperationException("The active module must expose content.");

    public AvailabilityStatus ConnectionStatus => _priceVerifier.ConnectionStatus;

    public AvailabilityStatus LicenseStatus => _priceVerifier.LicenseStatus;

    public string ConnectionStatusText => _priceVerifier.ConnectionStatusText;

    public string LicenseStatusText => _priceVerifier.LicenseStatusText;

    public OperationalMessage StatusMessage => _priceVerifier.StatusMessage;

    public RelayCommand SelectModuleCommand { get; }

    public bool RegisterModule(UtilityModuleItem module)
    {
        ArgumentNullException.ThrowIfNull(module);
        if (Modules.Any(existing => string.Equals(existing.Id, module.Id, StringComparison.Ordinal)))
        {
            return false;
        }

        Modules.Add(module);
        OnPropertyChanged(nameof(UtilityModules));
        OnPropertyChanged(nameof(AdministrationModules));
        return true;
    }

    public bool SelectModule(UtilityModuleItem? module)
    {
        if (module is null || !module.IsEnabled || module.Content is null || !Modules.Contains(module))
        {
            return false;
        }

        foreach (var item in Modules)
        {
            item.SetSelected(ReferenceEquals(item, module));
        }

        ActiveModule = module;
        _logger.LogInformation("Module {ModuleId} selected", module.Id);
        return true;
    }

    private void InitializeActiveModule(IModuleInitializer initializer)
    {
        try
        {
            initializer.Initialize(ActiveModule.Id);
            _logger.LogInformation("Module {ModuleId} initialized", ActiveModule.Id);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Recoverable module failure at {Stage} for {ModuleId}",
                "Initialization",
                ActiveModule.Id);
        }
    }

    private void HandlePriceVerifierPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PriceVerifierViewModel.ConnectionStatus)
            or nameof(PriceVerifierViewModel.ConnectionStatusText))
        {
            OnPropertyChanged(nameof(ConnectionStatus));
            OnPropertyChanged(nameof(ConnectionStatusText));
        }

        if (e.PropertyName is nameof(PriceVerifierViewModel.LicenseStatus)
            or nameof(PriceVerifierViewModel.LicenseStatusText))
        {
            OnPropertyChanged(nameof(LicenseStatus));
            OnPropertyChanged(nameof(LicenseStatusText));
        }

        if (e.PropertyName == nameof(PriceVerifierViewModel.StatusMessage))
        {
            OnPropertyChanged(nameof(StatusMessage));
        }
    }

    private static IReadOnlyList<UtilityModuleItem> CreateInitialModules(PriceVerifierViewModel priceVerifier) =>
    [
        new(PriceVerifierModuleId, "Verificador de precios", ModuleSection.Utilities, true, priceVerifier, true),
        new("label-printing", "Impresión de etiquetas", ModuleSection.Utilities, false),
        new("quick-inventory", "Inventario rápido", ModuleSection.Utilities, false),
        new("new-utility", "Nueva utilería", ModuleSection.Utilities, false),
        new("settings", "Configuración", ModuleSection.Administration, false),
        new("licensing", "Licencias", ModuleSection.Administration, false),
        new("system-logs", "Logs del sistema", ModuleSection.Administration, false)
    ];

    private static void ValidateUniqueIds(IEnumerable<UtilityModuleItem> modules)
    {
        var duplicate = modules.GroupBy(module => module.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Duplicate module id: {duplicate.Key}");
        }
    }
}
