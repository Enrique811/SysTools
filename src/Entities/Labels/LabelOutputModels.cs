using SysTools.Entities.Barcodes;

namespace SysTools.Entities.Labels;

public enum LabelOutputEnvironment { Preview, Production }
public enum LabelOutputStatus
{
    Pending, PreviewReady, Printed, Busy, InvalidProduct, ConfigurationUnavailable,
    TemplateMissing, TemplateIncompatible, TemplateNotApproved, BarcodeFailed,
    RenderFailed, PrinterUnavailable, PrintRejected, OutcomeUnknown, Canceled, Stale
}
public enum LabelTemplateStatus { Available, Missing, Invalid, Incompatible }
public enum LabelRenderStatus { Rendered, Failed, Canceled }
public enum LabelPrintStatus { Accepted, PrinterUnavailable, Rejected, OutcomeUnknown, Canceled }

public sealed record LabelRowContext
{
    public LabelRowContext(LabelOutputEnvironment environment, int capacity, string templateId, string printerId, string? information)
    {
        if (!Enum.IsDefined(environment)) throw new ArgumentOutOfRangeException(nameof(environment));
        if (capacity is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (string.IsNullOrWhiteSpace(templateId)) throw new ArgumentException("Template id required.", nameof(templateId));
        if (environment == LabelOutputEnvironment.Production && string.IsNullOrWhiteSpace(printerId))
            throw new ArgumentException("Printer required for production.", nameof(printerId));
        Environment = environment; Capacity = capacity; TemplateId = templateId;
        PrinterId = printerId ?? string.Empty; Information = information ?? string.Empty;
    }
    public LabelOutputEnvironment Environment { get; }
    public int Capacity { get; }
    public string TemplateId { get; }
    public string PrinterId { get; }
    public string Information { get; }
}

public sealed class PreparedLabel
{
    private readonly byte[] _barcodeBytes;
    public PreparedLabel(int position, LabelData data, BarcodeGenerationResult barcode)
    {
        if (position is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(position));
        ArgumentNullException.ThrowIfNull(data); ArgumentNullException.ThrowIfNull(barcode);
        if (barcode.Status != BarcodeGenerationStatus.Success || barcode.NormalizedValue is null || barcode.Type is null)
            throw new ArgumentException("Successful barcode required.", nameof(barcode));
        Position = position; Data = data; BarcodeValue = barcode.NormalizedValue; BarcodeType = barcode.Type.Value;
        BarcodeWidth = barcode.Width; BarcodeHeight = barcode.Height; _barcodeBytes = barcode.GetImageBytes();
    }
    public int Position { get; }
    public LabelData Data { get; }
    public string BarcodeValue { get; }
    public BarcodeType BarcodeType { get; }
    public int BarcodeWidth { get; }
    public int BarcodeHeight { get; }
    public byte[] GetBarcodeBytes() => (byte[])_barcodeBytes.Clone();
}

public sealed class PreparedLabelRow
{
    public PreparedLabelRow(string operationId, LabelRowContext context, DateTime serverDate, IEnumerable<PreparedLabel> labels)
    {
        if (!Guid.TryParseExact(operationId, "N", out _)) throw new ArgumentException("Opaque operation id required.", nameof(operationId));
        Context = context ?? throw new ArgumentNullException(nameof(context));
        var values = labels?.ToArray() ?? throw new ArgumentNullException(nameof(labels));
        if (values.Length != context.Capacity || values.Where((x, i) => x.Position != i + 1).Any())
            throw new ArgumentException("Labels must fill contiguous row positions.", nameof(labels));
        OperationId = operationId; ServerDate = DateTime.SpecifyKind(serverDate, DateTimeKind.Unspecified);
        Labels = Array.AsReadOnly(values);
    }
    public string OperationId { get; }
    public LabelRowContext Context { get; }
    public DateTime ServerDate { get; }
    public IReadOnlyList<PreparedLabel> Labels { get; }
}

public sealed record LabelTemplateDescriptor
{
    public LabelTemplateDescriptor(string id, string version, int capacity, decimal cellWidthMm, decimal pageHeightMm, int dpi, bool productionApproved, string sha256 = "")
    {
        if (string.IsNullOrWhiteSpace(id) || Path.GetFileName(id) != id || !id.EndsWith(".label.json", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Safe template id required.", nameof(id));
        if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("Version required.", nameof(version));
        if (capacity is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (cellWidthMm is < 20 or > 150 || pageHeightMm is < 20 or > 150) throw new ArgumentOutOfRangeException(nameof(cellWidthMm));
        if (dpi is < 96 or > 600) throw new ArgumentOutOfRangeException(nameof(dpi));
        if (sha256.Length != 0 && (sha256.Length != 64 || sha256.Any(character => !Uri.IsHexDigit(character))))
            throw new ArgumentException("SHA-256 must be a hexadecimal digest.", nameof(sha256));
        Id=id; Version=version; Capacity=capacity; CellWidthMm=cellWidthMm; PageHeightMm=pageHeightMm; Dpi=dpi; ProductionApproved=productionApproved;
        Sha256 = sha256.ToUpperInvariant();
    }
    public string Id { get; }
    public string Version { get; }
    public int Capacity { get; }
    public decimal CellWidthMm { get; }
    public decimal PageHeightMm { get; }
    public int Dpi { get; }
    public bool ProductionApproved { get; }
    public string Sha256 { get; }
}

public sealed class LabelPreviewDocument
{
    public const int MaximumBytes = 10 * 1024 * 1024;
    private readonly byte[] _bytes;
    public LabelPreviewDocument(string operationId, int widthPixels, int heightPixels, decimal widthMm, decimal heightMm, byte[] pngBytes)
    {
        if (!Guid.TryParseExact(operationId, "N", out _)) throw new ArgumentException("Operation id required.", nameof(operationId));
        if (widthPixels <= 0 || heightPixels <= 0 || widthMm <= 0 || heightMm <= 0) throw new ArgumentOutOfRangeException(nameof(widthPixels));
        ArgumentNullException.ThrowIfNull(pngBytes);
        if (pngBytes.Length is < 1 or > MaximumBytes) throw new ArgumentOutOfRangeException(nameof(pngBytes));
        OperationId=operationId; WidthPixels=widthPixels; HeightPixels=heightPixels; WidthMm=widthMm; HeightMm=heightMm; _bytes=(byte[])pngBytes.Clone();
    }
    public string OperationId { get; }
    public int WidthPixels { get; }
    public int HeightPixels { get; }
    public decimal WidthMm { get; }
    public decimal HeightMm { get; }
    public int ByteLength => _bytes.Length;
    public byte[] GetPngBytes() => (byte[])_bytes.Clone();
}

public sealed record LabelTemplateResult(LabelTemplateStatus Status, LabelTemplateDescriptor? Template = null);
public sealed record LabelRenderResult(LabelRenderStatus Status, LabelPreviewDocument? Document = null);
public sealed record LabelPrintResult(LabelPrintStatus Status);
public sealed record LabelCancelResult(int Discarded, bool HadCompletedRow);

public sealed record LabelWorkflowSnapshot
{
    public LabelWorkflowSnapshot(int pending, int capacity, bool hasCompletedRow, bool canRetry, bool isBusy)
    {
        if (pending < 0) throw new ArgumentOutOfRangeException(nameof(pending));
        if (capacity is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (pending > capacity) throw new ArgumentOutOfRangeException(nameof(pending));
        if (hasCompletedRow && pending != capacity) throw new ArgumentException("A completed row must fill its capacity.", nameof(pending));
        if (canRetry && !hasCompletedRow) throw new ArgumentException("Only a completed row can be retried.", nameof(canRetry));
        Pending = pending;
        Capacity = capacity;
        HasCompletedRow = hasCompletedRow;
        CanRetry = canRetry;
        IsBusy = isBusy;
    }

    public int Pending { get; }
    public int Capacity { get; }
    public int Remaining => Capacity > 0 ? Capacity - Pending : 0;
    public bool HasCompletedRow { get; }
    public bool CanRetry { get; }
    public bool IsBusy { get; }
}

public sealed class LabelOutputResult
{
    public LabelOutputResult(LabelOutputStatus status, string message, int pending = 0, int capacity = 0, bool canRetry = false, LabelPreviewDocument? preview = null)
    {
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Message required.", nameof(message));
        if (preview is not null && status != LabelOutputStatus.PreviewReady) throw new ArgumentException("Preview only valid for PreviewReady.", nameof(preview));
        Status=status; Message=message; Pending=pending; Capacity=capacity; CanRetry=canRetry; Preview=preview;
    }
    public LabelOutputStatus Status { get; }
    public string Message { get; }
    public int Pending { get; }
    public int Capacity { get; }
    public int Remaining => Capacity > 0 ? Capacity - Pending : 0;
    public bool CanRetry { get; }
    public LabelPreviewDocument? Preview { get; }
}
