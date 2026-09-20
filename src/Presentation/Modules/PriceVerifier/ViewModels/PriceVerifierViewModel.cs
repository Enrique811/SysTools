using SysTools.Presentation.ViewModels;

namespace SysTools.Presentation.Modules.PriceVerifier.ViewModels;

public sealed class PriceVerifierViewModel : ViewModelBase
{
    public string ModuleTitle => "Verificador de precios";

    public string ModuleDescription => "Estructura inicial del módulo. Las consultas y acciones se habilitarán en features posteriores.";

    public string Barcode => string.Empty;

    public string AdditionalInformation => string.Empty;

    public string ProductDescription => "—";

    public string ProductPresentation => "—";

    public string StockDisplay => "—";

    public string FinalPriceDisplay => "—";

    public bool IsBarcodeAvailable => false;

    public bool IsAdditionalInformationAvailable => false;

    public bool IsSearchAvailable => false;

    public bool IsPrintAvailable => false;

    public bool IsSettingsAvailable => false;
}
