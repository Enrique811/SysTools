using System.Drawing;
using System.Drawing.Printing;
using SysTools.Business.Labels;
using SysTools.Entities.Labels;

namespace SysTools.Data.Reports;

public interface IWindowsLabelPrintBackend
{
    IReadOnlyCollection<string> InstalledPrinters { get; }
    LabelPrintStatus Submit(LabelPreviewDocument document, string exactPrinterId, string operationId, CancellationToken cancellationToken);
}

public sealed class WindowsLabelPrinter(IWindowsLabelPrintBackend? backend = null) : ILabelPrinter
{
    private readonly IWindowsLabelPrintBackend _backend = backend ?? new SystemDrawingLabelPrintBackend();

    public Task<LabelPrintResult> PrintAsync(LabelPreviewDocument document, string exactPrinterId, string operationId, CancellationToken cancellationToken = default) =>
        Task.Run(() => Print(document, exactPrinterId, operationId, cancellationToken), cancellationToken);

    private LabelPrintResult Print(LabelPreviewDocument document, string printer, string operationId, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var exact = _backend.InstalledPrinters.FirstOrDefault(candidate => string.Equals(candidate, printer, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(printer) || exact is null)
            return new(LabelPrintStatus.PrinterUnavailable);
        return new(_backend.Submit(document, exact, operationId, token));
    }
}

public sealed class SystemDrawingLabelPrintBackend : IWindowsLabelPrintBackend
{
    public IReadOnlyCollection<string> InstalledPrinters => PrinterSettings.InstalledPrinters.Cast<string>().ToArray();

    public LabelPrintStatus Submit(LabelPreviewDocument document, string exactPrinterId, string operationId, CancellationToken token)
    {
        var submitted = false;
        try
        {
            using var bytes = new MemoryStream(document.GetPngBytes(), writable: false);
            using var image = Image.FromStream(bytes);
            using var print = new PrintDocument
            {
                DocumentName = $"SysTools-{operationId}",
                PrintController = new StandardPrintController(),
                PrinterSettings = new PrinterSettings { PrinterName = exactPrinterId }
            };
            print.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
            print.DefaultPageSettings.PaperSize = new PaperSize("SysTools Label", MmToHundredths(document.WidthMm), MmToHundredths(document.HeightMm));
            print.PrintPage += (_, args) => { args.Graphics?.DrawImage(image, args.PageBounds); args.HasMorePages = false; };
            token.ThrowIfCancellationRequested();
            submitted = true;
            print.Print();
            return LabelPrintStatus.Accepted;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested && !submitted) { return LabelPrintStatus.Canceled; }
        catch (InvalidPrinterException) { return LabelPrintStatus.PrinterUnavailable; }
        catch { return submitted ? LabelPrintStatus.OutcomeUnknown : LabelPrintStatus.Rejected; }
    }

    private static int MmToHundredths(decimal mm) => checked((int)Math.Round(mm / 25.4m * 100m, MidpointRounding.AwayFromZero));
}
