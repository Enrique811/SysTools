using System.Diagnostics;
using SysTools.Business.Labels;
using SysTools.BusinessRules.Tests.TestDoubles;
using SysTools.Entities.Labels;
using Xunit.Abstractions;

namespace SysTools.BusinessRules.Tests.Labels;

public sealed class LabelQueueReliabilityTests
{
    private readonly ITestOutputHelper _output;

    public LabelQueueReliabilityTests(ITestOutputHelper output) => _output = output;

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void One_hundred_rows_preserve_every_label_once_and_meet_local_p95(int columns)
    {
        var service = new LabelQueueService();
        var durations = new List<double>(100 * columns);

        for (var row = 0; row < 100; row++)
        {
            LabelQueueResult? completed = null;
            var expected = new List<string>(columns);
            for (var position = 0; position < columns; position++)
            {
                var barcode = $"{row:D3}-{position + 1}";
                expected.Add(barcode);
                var stopwatch = Stopwatch.StartNew();
                var result = service.Capture(Data(barcode), columns);
                stopwatch.Stop();
                durations.Add(stopwatch.Elapsed.TotalMilliseconds);
                if (result.IsComplete)
                {
                    completed = result;
                }
            }

            Assert.NotNull(completed);
            Assert.Equal(expected, completed.Completed.Select(item => item.Product.Barcode));
            Assert.Empty(service.GetPending());
        }

        durations.Sort();
        var percentileIndex = (int)Math.Ceiling(durations.Count * 0.95) - 1;
        _output.WriteLine("columns={0}; operations={1}; p95={2:F6} ms", columns, durations.Count, durations[percentileIndex]);
        Assert.True(durations[percentileIndex] < 5, $"p95 was {durations[percentileIndex]:F3} ms");
    }

    private static LabelData Data(string barcode) =>
        new(ProductTestFactory.Create(barcode: barcode), "$1.00", "info");
}
