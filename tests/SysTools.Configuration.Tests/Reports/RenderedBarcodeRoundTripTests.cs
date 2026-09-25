using System.Drawing;
using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Business.Barcodes;
using SysTools.Data.Barcodes;
using SysTools.Data.Reports;
using SysTools.Entities.Labels;
using SysTools.Entities.Products;
using ZXing;
using ZXing.Common;

namespace SysTools.Configuration.Tests.Reports;

public sealed class RenderedBarcodeRoundTripTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Every_barcode_decodes_from_its_final_rendered_slot(int capacity)
    {
        var barcodeService = new BarcodeService(new ZxingBarcodeImageEncoder(), NullLogger<BarcodeService>.Instance);
        var context = new LabelRowContext(LabelOutputEnvironment.Preview, capacity, $"label-{capacity}.label.json", "", "Información");
        var labels = Enumerable.Range(1, capacity).Select(position =>
        {
            var value = $"SKU-{position:0000}";
            var product = new Product(position, value, $"Producto {position}", "Pieza", 10m, "1");
            return new PreparedLabel(position, new LabelData(product, "$10.00", context.Information), barcodeService.Generate(value));
        }).ToArray();
        var row = new PreparedLabelRow(Guid.NewGuid().ToString("N"), context, new DateTime(2026, 9, 25), labels);
        var template = new LabelTemplateDescriptor(context.TemplateId, "1", capacity, 57, 40, 300, false);

        var result = await new NativeLabelDocumentRenderer().RenderAsync(row, template);
        using var stream = new MemoryStream(result.Document!.GetPngBytes());
        using var page = new Bitmap(stream);
        var cellWidth = page.Width / capacity;
        var reader = new ZXing.Windows.Compatibility.BarcodeReader
        {
            Options = new DecodingOptions { PossibleFormats = [BarcodeFormat.CODE_128], TryHarder = true }
        };

        for (var index = 0; index < capacity; index++)
        {
            var area = new Rectangle(index * cellWidth, (int)(page.Height * .53), cellWidth, (int)(page.Height * .31));
            using var crop = page.Clone(area, page.PixelFormat);
            var decoded = reader.Decode(crop);
            Assert.NotNull(decoded);
            Assert.Equal(labels[index].BarcodeValue, decoded.Text);
        }
    }
}
