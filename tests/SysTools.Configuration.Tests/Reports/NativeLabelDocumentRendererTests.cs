using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Business.Barcodes;
using SysTools.Data.Barcodes;
using SysTools.Data.Reports;
using SysTools.Entities.Labels;
using SysTools.Entities.Products;

namespace SysTools.Configuration.Tests.Reports;

public sealed class NativeLabelDocumentRendererTests
{
    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public async Task Renders_valid_png_with_exact_physical_pixel_dimensions(int capacity)
    {
        var barcodeService = new BarcodeService(new ZxingBarcodeImageEncoder(), NullLogger<BarcodeService>.Instance);
        var context = new LabelRowContext(LabelOutputEnvironment.Preview, capacity, $"label-{capacity}.label.json", string.Empty, "Información ñ");
        var labels = Enumerable.Range(1, capacity).Select(position =>
        {
            var product = new Product(position, $"75012345678{position}", $"Descripción {position}", "Pieza", 12.5m, "1");
            return new PreparedLabel(position, new LabelData(product, "$12.50", context.Information), barcodeService.Generate(product.Barcode));
        });
        var row = new PreparedLabelRow(Guid.NewGuid().ToString("N"), context, new DateTime(2026,9,25), labels);
        var template = new LabelTemplateDescriptor(context.TemplateId, "1", capacity, 57, 40, 300, false);

        var result = await new NativeLabelDocumentRenderer().RenderAsync(row, template);

        Assert.Equal(LabelRenderStatus.Rendered, result.Status);
        Assert.NotNull(result.Document);
        Assert.Equal((int)Math.Round(57m * 300m / 25.4m) * capacity, result.Document.WidthPixels);
        Assert.Equal((int)Math.Round(40m * 300m / 25.4m), result.Document.HeightPixels);
        var bytes = result.Document.GetPngBytes();
        Assert.Equal(new byte[] {137,80,78,71,13,10,26,10}, bytes.Take(8));
        Assert.InRange(bytes.Length, 1, LabelPreviewDocument.MaximumBytes);
    }
}
