namespace SysTools.Entities.Barcodes;

public sealed class BarcodeGenerationResult
{
    public const int MaximumImageLength = 1024 * 1024;

    private readonly byte[] _imageBytes;

    private BarcodeGenerationResult(
        BarcodeGenerationStatus status,
        string? normalizedValue,
        BarcodeType? type,
        int width,
        int height,
        byte[] imageBytes)
    {
        Status = status;
        NormalizedValue = normalizedValue;
        Type = type;
        Width = width;
        Height = height;
        _imageBytes = (byte[])imageBytes.Clone();
    }

    public BarcodeGenerationStatus Status { get; }

    public string Message => Status switch
    {
        BarcodeGenerationStatus.Success => "Código de barras generado correctamente.",
        BarcodeGenerationStatus.MissingInput => "Ingrese un código de barras.",
        BarcodeGenerationStatus.UnsupportedValue => "El código contiene caracteres no compatibles.",
        BarcodeGenerationStatus.InputTooLong => "El código excede la longitud permitida.",
        BarcodeGenerationStatus.GenerationFailed => "No fue posible generar el código de barras.",
        _ => throw new InvalidOperationException("Estado de código de barras no soportado.")
    };

    public string? NormalizedValue { get; }

    public BarcodeType? Type { get; }

    public int Width { get; }

    public int Height { get; }

    public int ImageLength => _imageBytes.Length;

    public byte[] GetImageBytes() => (byte[])_imageBytes.Clone();

    public static BarcodeGenerationResult Succeeded(
        string normalizedValue,
        BarcodeType type,
        int width,
        int height,
        byte[] imageBytes)
    {
        ArgumentException.ThrowIfNullOrEmpty(normalizedValue);
        ArgumentNullException.ThrowIfNull(imageBytes);
        if (normalizedValue.Length > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(normalizedValue));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        if (imageBytes.Length is < 1 or > MaximumImageLength)
        {
            throw new ArgumentOutOfRangeException(nameof(imageBytes));
        }

        return new BarcodeGenerationResult(
            BarcodeGenerationStatus.Success,
            normalizedValue,
            type,
            width,
            height,
            imageBytes);
    }

    public static BarcodeGenerationResult Failed(BarcodeGenerationStatus status)
    {
        if (!Enum.IsDefined(status) || status == BarcodeGenerationStatus.Success)
        {
            throw new ArgumentException("Se requiere un estado de fallo válido.", nameof(status));
        }

        return new BarcodeGenerationResult(status, null, null, 0, 0, []);
    }
}
