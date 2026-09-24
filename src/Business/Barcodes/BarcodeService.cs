using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SysTools.Entities.Barcodes;

namespace SysTools.Business.Barcodes;

public sealed class BarcodeService : IBarcodeService
{
    public const int MaximumInputLength = 50;

    private const int StandardWidth = 340;
    private const int StandardHeight = 56;
    private const int Code128MinimumWidth = 500;
    private const int Code128Height = 60;
    private const int Code128WidthPerCharacter = 18;

    private readonly BarcodeClassifier _classifier = new();
    private readonly IBarcodeImageEncoder _encoder;
    private readonly ILogger<BarcodeService> _logger;

    public BarcodeService(
        IBarcodeImageEncoder encoder,
        ILogger<BarcodeService> logger)
    {
        ArgumentNullException.ThrowIfNull(encoder);
        ArgumentNullException.ThrowIfNull(logger);
        _encoder = encoder;
        _logger = logger;
    }

    public BarcodeType DetectType(string? input) => _classifier.DetectType(input);

    public BarcodeGenerationResult Generate(
        string? input,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = BarcodeClassifier.Normalize(input);
        if (value.Length == 0)
        {
            return Failure(BarcodeGenerationStatus.MissingInput, "Normalize", null, 0);
        }

        if (value.Length > MaximumInputLength)
        {
            return Failure(
                BarcodeGenerationStatus.InputTooLong,
                "ValidateInput",
                null,
                value.Length);
        }

        var type = _classifier.DetectType(value);
        if (type == BarcodeType.Code128 && value.Any(character => character > 127))
        {
            return Failure(
                BarcodeGenerationStatus.UnsupportedValue,
                "ValidateInput",
                type,
                value.Length);
        }

        var (width, height) = type == BarcodeType.Code128
            ? (Math.Max(Code128MinimumWidth, value.Length * Code128WidthPerCharacter), Code128Height)
            : (StandardWidth, StandardHeight);

        cancellationToken.ThrowIfCancellationRequested();
        byte[] bytes;
        try
        {
            bytes = _encoder.EncodePng(value, type, width, height, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsRecoverable(exception))
        {
            return Failure(
                BarcodeGenerationStatus.GenerationFailed,
                "Encode",
                type,
                value.Length);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!PngOutputValidator.IsComplete(bytes, width, height))
        {
            return Failure(
                BarcodeGenerationStatus.GenerationFailed,
                "ValidateOutput",
                type,
                value.Length);
        }

        _logger.LogInformation(
            "Barcode generation completed at {Stage} with {Status}, {BarcodeType} and length {InputLength}",
            "Complete",
            BarcodeGenerationStatus.Success,
            type,
            value.Length);
        return BarcodeGenerationResult.Succeeded(value, type, width, height, bytes);
    }

    private BarcodeGenerationResult Failure(
        BarcodeGenerationStatus status,
        string stage,
        BarcodeType? type,
        int inputLength)
    {
        _logger.LogWarning(
            "Barcode generation completed at {Stage} with {Status}, {BarcodeType} and length {InputLength}",
            stage,
            status,
            type,
            inputLength);
        return BarcodeGenerationResult.Failed(status);
    }

    private static bool IsRecoverable(Exception exception) =>
        exception is BarcodeEncodingException
            or ArgumentException
            or InvalidOperationException
            or IOException
            or ExternalException;
}
