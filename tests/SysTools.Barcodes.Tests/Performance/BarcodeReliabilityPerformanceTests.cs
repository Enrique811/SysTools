using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Business.Barcodes;
using SysTools.Data.Barcodes;
using SysTools.Entities.Barcodes;

namespace SysTools.Barcodes.Tests.Performance;

public sealed class BarcodeReliabilityPerformanceTests
{
    [Fact]
    public void One_thousand_generations_are_stable_and_meet_local_p95_goal()
    {
        var service = new BarcodeService(
            new ZxingBarcodeImageEncoder(),
            NullLogger<BarcodeService>.Instance);
        var cases = new[]
        {
            (Value: "96385074", Type: BarcodeType.Ean8, Width: 340, Height: 56),
            (Value: "4006381333931", Type: BarcodeType.Ean13, Width: 340, Height: 56),
            (Value: "036000291452", Type: BarcodeType.UpcA, Width: 340, Height: 56),
            (Value: "SKU-000123", Type: BarcodeType.Code128, Width: 500, Height: 60)
        };
        var elapsed = new double[1000];

        for (var index = 0; index < elapsed.Length; index++)
        {
            var item = cases[index % cases.Length];
            var stopwatch = Stopwatch.StartNew();
            var result = service.Generate(item.Value);
            stopwatch.Stop();
            elapsed[index] = stopwatch.Elapsed.TotalMilliseconds;

            Assert.Equal(BarcodeGenerationStatus.Success, result.Status);
            Assert.Equal(item.Type, result.Type);
            Assert.Equal(item.Width, result.Width);
            Assert.Equal(item.Height, result.Height);
            Assert.True(PngOutputValidator.IsComplete(
                result.GetImageBytes(),
                item.Width,
                item.Height));
            var first = result.GetImageBytes();
            var second = result.GetImageBytes();
            Assert.NotSame(first, second);
            Assert.Equal(first, second);
        }

        Array.Sort(elapsed);
        var p95 = elapsed[(int)Math.Ceiling(elapsed.Length * 0.95) - 1];
        Assert.True(p95 < 100, $"Expected p95 < 100 ms but measured {p95:F2} ms.");
    }
}
