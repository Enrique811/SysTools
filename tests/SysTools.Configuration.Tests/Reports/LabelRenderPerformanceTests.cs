using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Business.Barcodes;
using SysTools.Data.Barcodes;
using SysTools.Data.Reports;
using SysTools.Entities.Labels;
using SysTools.Entities.Products;

namespace SysTools.Configuration.Tests.Reports;

public sealed class LabelRenderPerformanceTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Render_stays_below_two_seconds_and_ten_mibibytes(int capacity)
    {
        var barcodeService = new BarcodeService(new ZxingBarcodeImageEncoder(), NullLogger<BarcodeService>.Instance);
        var context = new LabelRowContext(LabelOutputEnvironment.Preview, capacity, $"label-{capacity}.label.json", "", new string('I', 200));
        var labels = Enumerable.Range(1, capacity).Select(position =>
        {
            var product = new Product(position, $"SKU-{position:0000}", new string('D', 200), "Presentación larga", 123456.78m, "1");
            return new PreparedLabel(position, new LabelData(product, "$123,456.78", context.Information), barcodeService.Generate(product.Barcode));
        });
        var row = new PreparedLabelRow(Guid.NewGuid().ToString("N"), context, new DateTime(2026, 9, 25), labels);
        var template = new LabelTemplateDescriptor(context.TemplateId, "1", capacity, 57, 40, 300, false);
        var renderer = new NativeLabelDocumentRenderer();
        var elapsed = new List<long>();

        for (var iteration = 0; iteration < 10; iteration++)
        {
            var timer = Stopwatch.StartNew();
            var result = await renderer.RenderAsync(row, template);
            timer.Stop();
            Assert.Equal(LabelRenderStatus.Rendered, result.Status);
            Assert.InRange(result.Document!.ByteLength, 1, LabelPreviewDocument.MaximumBytes);
            elapsed.Add(timer.ElapsedMilliseconds);
        }

        var p95 = elapsed.Order().ElementAt((int)Math.Ceiling(elapsed.Count * .95) - 1);
        Assert.InRange(p95, 0, 1999);
    }
}
