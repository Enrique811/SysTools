using SysTools.Business.Labels;
using SysTools.BusinessRules.Tests.TestDoubles;
using SysTools.Entities.Labels;

namespace SysTools.BusinessRules.Tests.Labels;

public sealed class LabelQueueCancellationTests
{
    [Fact]
    public void Cancelling_empty_queue_is_idempotent()
    {
        var service = new LabelQueueService();

        Assert.Equal(0, service.Cancel());
        Assert.Equal(0, service.Cancel());
        Assert.Empty(service.GetPending());
    }

    [Fact]
    public void Cancel_returns_discarded_count_and_next_capture_restarts_at_one_with_new_capacity()
    {
        var service = new LabelQueueService();
        service.Capture(Data("001"), 3);
        service.Capture(Data("002"), 3);

        var discarded = service.Cancel();
        var next = service.Capture(Data("003"), 2);

        Assert.Equal(2, discarded);
        Assert.Collection(next.Pending, item =>
        {
            Assert.Equal(1, item.Position);
            Assert.Equal("003", item.Data.Product.Barcode);
        });
    }

    [Fact]
    public void Cancel_after_completion_does_not_change_returned_row()
    {
        var service = new LabelQueueService();
        var completed = service.Capture(Data("001"), 1);

        var discarded = service.Cancel();

        Assert.Equal(0, discarded);
        Assert.True(completed.IsComplete);
        Assert.Collection(completed.Completed, item => Assert.Equal("001", item.Product.Barcode));
    }

    private static LabelData Data(string barcode) =>
        new(ProductTestFactory.Create(barcode: barcode), "$1.00", "info");
}
