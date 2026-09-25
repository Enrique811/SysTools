using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using SysTools.Business.Labels;
using SysTools.Entities.Labels;

namespace SysTools.Data.Reports;

public sealed class NativeLabelDocumentRenderer : ILabelDocumentRenderer
{
    public Task<LabelRenderResult> RenderAsync(PreparedLabelRow row, LabelTemplateDescriptor template, CancellationToken cancellationToken = default) =>
        Task.Run(() => Render(row, template, cancellationToken), cancellationToken);

    private static LabelRenderResult Render(PreparedLabelRow row, LabelTemplateDescriptor template, CancellationToken token)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            if (row.Context.Capacity != template.Capacity) return new(LabelRenderStatus.Failed);
            var cellWidth = MillimetersToPixels(template.CellWidthMm, template.Dpi);
            var height = MillimetersToPixels(template.PageHeightMm, template.Dpi);
            var width = checked(cellWidth * template.Capacity);
            using var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            bitmap.SetResolution(template.Dpi, template.Dpi);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.White);
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            for (var index = 0; index < row.Labels.Count; index++)
            {
                token.ThrowIfCancellationRequested();
                DrawCell(graphics, row.Labels[index], row.ServerDate, index * cellWidth, cellWidth, height);
            }
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            var document = new LabelPreviewDocument(row.OperationId, width, height,
                template.CellWidthMm * template.Capacity, template.PageHeightMm, stream.ToArray());
            return new(LabelRenderStatus.Rendered, document);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch { return new(LabelRenderStatus.Failed); }
    }

    private static void DrawCell(Graphics g, PreparedLabel label, DateTime date, int x, int width, int height)
    {
        var pad = Math.Max(8, width / 45);
        using var border = new Pen(Color.LightGray, 1);
        using var normal = new Font("Arial", Math.Max(7, height / 45f), FontStyle.Regular, GraphicsUnit.Pixel);
        using var small = new Font("Arial", Math.Max(6, height / 55f), FontStyle.Regular, GraphicsUnit.Pixel);
        using var bold = new Font("Arial", Math.Max(11, height / 20f), FontStyle.Bold, GraphicsUnit.Pixel);
        using var centered = new StringFormat { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        using var left = new StringFormat { Trimming = StringTrimming.EllipsisCharacter };
        g.DrawRectangle(border, x, 0, width - 1, height - 1);
        var product = label.Data.Product;
        g.DrawString(product.Description, normal, Brushes.Black, new RectangleF(x + pad, pad, width - pad * 2, height * .17f), left);
        g.DrawString(product.Presentation ?? string.Empty, small, Brushes.Black, new RectangleF(x + pad, height * .19f, width - pad * 2, height * .08f), left);
        g.DrawString(label.Data.FormattedPrice, bold, Brushes.Black, new RectangleF(x + pad, height * .25f, width - pad * 2, height * .22f), centered);
        g.DrawString(label.Data.Information, small, Brushes.Black, new RectangleF(x + pad, height * .46f, width - pad * 2, height * .08f), centered);
        using var barcodeStream = new MemoryStream(label.GetBarcodeBytes(), writable: false);
        using var barcode = Image.FromStream(barcodeStream);
        var barcodeRect = new RectangleF(x + width * .08f, height * .56f, width * .84f, height * .24f);
        g.DrawImage(barcode, barcodeRect);
        g.DrawString(label.BarcodeValue, small, Brushes.Black, new RectangleF(x + pad, height * .81f, width - pad * 2, height * .08f), centered);
        g.DrawString(date.ToString("yyyy-MM-dd"), small, Brushes.Black, new RectangleF(x + pad, height * .9f, width - pad * 2, height * .07f), centered);
    }

    private static int MillimetersToPixels(decimal millimeters, int dpi) =>
        checked((int)Math.Round(millimeters * dpi / 25.4m, MidpointRounding.AwayFromZero));
}
