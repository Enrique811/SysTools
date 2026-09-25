namespace SysTools.Presentation.Modules.PriceVerifier.Search;

public enum ProductSearchDialogOutcome
{
    Selected,
    Canceled,
    OperationalFailure
}

public sealed class ProductSearchDialogResult : EventArgs
{
    public ProductSearchDialogResult(ProductSearchDialogOutcome outcome, string? selectedBarcode = null)
    {
        if (!Enum.IsDefined(outcome))
        {
            throw new ArgumentOutOfRangeException(nameof(outcome));
        }

        if (outcome == ProductSearchDialogOutcome.Selected)
        {
            if (string.IsNullOrWhiteSpace(selectedBarcode))
            {
                throw new ArgumentException("La seleccion requiere un codigo.", nameof(selectedBarcode));
            }

            if (selectedBarcode.Length > 50)
            {
                throw new ArgumentOutOfRangeException(nameof(selectedBarcode));
            }
        }
        else if (selectedBarcode is not null)
        {
            throw new ArgumentException("Solo una seleccion admite codigo.", nameof(selectedBarcode));
        }

        Outcome = outcome;
        SelectedBarcode = selectedBarcode;
    }

    public ProductSearchDialogOutcome Outcome { get; }
    public string? SelectedBarcode { get; }

    public static ProductSearchDialogResult Canceled() => new(ProductSearchDialogOutcome.Canceled);
    public static ProductSearchDialogResult OperationalFailure() => new(ProductSearchDialogOutcome.OperationalFailure);
    public static ProductSearchDialogResult Selected(string barcode) => new(ProductSearchDialogOutcome.Selected, barcode);
}
