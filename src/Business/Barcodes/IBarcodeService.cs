using SysTools.Entities.Barcodes;

namespace SysTools.Business.Barcodes;

public interface IBarcodeService
{
    BarcodeType DetectType(string? input);

    BarcodeGenerationResult Generate(
        string? input,
        CancellationToken cancellationToken = default);
}
