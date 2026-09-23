using System.Collections;
using SysTools.Business.Labels;
using SysTools.BusinessRules.Tests.TestDoubles;
using SysTools.Entities.Labels;

namespace SysTools.BusinessRules.Tests.Labels;

public sealed class LabelQueueCaptureTests
{
    [Fact]
    public void One_column_completes_immediately_without_pending_state()
    {
        var service = new LabelQueueService();
        var data = Data("001");

        var result = service.Capture(data, 1);

        Assert.True(result.IsComplete);
        Assert.Empty(result.Pending);
        Assert.Collection(result.Completed, item => Assert.Same(data, item));
        Assert.Empty(service.GetPending());
    }

    [Fact]
    public void Two_columns_complete_on_second_capture_in_order_and_reset()
    {
        var service = new LabelQueueService();
        var first = Data("001");
        var second = Data("002");

        var pending = service.Capture(first, 2);
        var completed = service.Capture(second, 2);

        Assert.False(pending.IsComplete);
        Assert.Equal(1, pending.Remaining);
        Assert.Collection(pending.Pending, item =>
        {
            Assert.Equal(1, item.Position);
            Assert.Same(first, item.Data);
        });
        Assert.True(completed.IsComplete);
        Assert.Collection(completed.Completed, item => Assert.Same(first, item), item => Assert.Same(second, item));
        Assert.Empty(service.GetPending());
    }

    [Fact]
    public void Three_columns_assign_contiguous_positions_and_complete_only_on_third_capture()
    {
        var service = new LabelQueueService();
        var labels = new[] { Data("001"), Data("002"), Data("003") };

        var first = service.Capture(labels[0], 3);
        var second = service.Capture(labels[1], 3);
        var third = service.Capture(labels[2], 3);

        Assert.Equal(new[] { 1 }, first.Pending.Select(item => item.Position));
        Assert.Equal(new[] { 1, 2 }, second.Pending.Select(item => item.Position));
        Assert.False(first.IsComplete);
        Assert.False(second.IsComplete);
        Assert.True(third.IsComplete);
        Assert.Equal(new[] { "001", "002", "003" }, third.Completed.Select(item => item.Product.Barcode));
        Assert.Empty(service.GetPending());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void Invalid_capacity_is_rejected_without_mutating_state(int columns)
    {
        var service = new LabelQueueService();
        service.Capture(Data("001"), 3);

        Assert.Throws<ArgumentOutOfRangeException>(() => service.Capture(Data("002"), columns));
        Assert.Equal(new[] { "001" }, service.GetPending().Select(item => item.Data.Product.Barcode));
    }

    [Fact]
    public void Capacity_change_is_rejected_without_mutating_pending_row()
    {
        var service = new LabelQueueService();
        service.Capture(Data("001"), 3);

        Assert.Throws<InvalidOperationException>(() => service.Capture(Data("002"), 2));

        var pending = service.GetPending();
        Assert.Collection(pending, item =>
        {
            Assert.Equal(1, item.Position);
            Assert.Equal("001", item.Data.Product.Barcode);
        });
    }

    [Fact]
    public void Returned_pending_and_completed_snapshots_cannot_mutate_service_or_result()
    {
        var service = new LabelQueueService();
        var pending = service.Capture(Data("001"), 2);
        var completed = service.Capture(Data("002"), 2);

        Assert.Throws<NotSupportedException>(() => ((IList)pending.Pending).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList)completed.Completed).Clear());
        Assert.Empty(service.GetPending());
        Assert.Equal(new[] { "001", "002" }, completed.Completed.Select(item => item.Product.Barcode));
    }

    private static LabelData Data(string barcode) =>
        new(ProductTestFactory.Create(barcode: barcode), "$1.00", "Árbol  ñ  50%");
}
