using SysTools.Entities.Barcodes;

namespace SysTools.Business.Barcodes;

public interface IBarcodeImageEncoder
{
    byte[] EncodePng(
        string normalizedValue,
        BarcodeType type,
        int width,
        int height,
        CancellationToken cancellationToken = default);
}
