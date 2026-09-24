namespace SysTools.Entities.Barcodes;

public enum BarcodeGenerationStatus
{
    Success,
    MissingInput,
    UnsupportedValue,
    InputTooLong,
    GenerationFailed
}
