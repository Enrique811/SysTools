using System.Collections;
using SysTools.BusinessRules.Tests.TestDoubles;
using SysTools.Entities.Labels;

namespace SysTools.BusinessRules.Tests.Labels;

public sealed class LabelQueueEntityTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void Pending_position_must_be_between_one_and_three(int position)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PendingLabel(position, Data("001")));
    }

    [Fact]
    public void Pending_data_is_required()
    {
        Assert.Throws<ArgumentNullException>(() => new PendingLabel(1, null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void Result_capacity_must_be_between_one_and_three(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new LabelQueueResult(capacity, Array.Empty<PendingLabel>(), Array.Empty<LabelData>()));
    }

    [Fact]
    public void Pending_and_completed_are_mutually_exclusive()
    {
        var data = Data("001");

        Assert.Throws<ArgumentException>(() =>
            new LabelQueueResult(2, new[] { new PendingLabel(1, data) }, new[] { data, data }));
    }

    [Fact]
    public void Completed_count_must_equal_capacity()
    {
        Assert.Throws<ArgumentException>(() =>
            new LabelQueueResult(2, Array.Empty<PendingLabel>(), new[] { Data("001") }));
    }

    [Fact]
    public void Pending_positions_must_be_contiguous_and_below_capacity()
    {
        Assert.Throws<ArgumentException>(() =>
            new LabelQueueResult(3, new[] { new PendingLabel(2, Data("001")) }, Array.Empty<LabelData>()));
        Assert.Throws<ArgumentException>(() =>
            new LabelQueueResult(
                2,
                new[] { new PendingLabel(1, Data("001")), new PendingLabel(2, Data("002")) },
                Array.Empty<LabelData>()));
    }

    [Fact]
    public void Snapshots_are_read_only_and_derive_counts()
    {
        var pending = new[] { new PendingLabel(1, Data("001")) };
        var result = new LabelQueueResult(3, pending, Array.Empty<LabelData>());

        pending[0] = new PendingLabel(1, Data("CHANGED"));

        Assert.False(result.IsComplete);
        Assert.Equal(2, result.Remaining);
        Assert.Equal("001", result.Pending[0].Data.Product.Barcode);
        Assert.Throws<NotSupportedException>(() => ((IList)result.Pending).Add(new object()));
        Assert.Throws<NotSupportedException>(() => ((IList)result.Completed).Add(new object()));
    }

    private static LabelData Data(string barcode) =>
        new(ProductTestFactory.Create(barcode: barcode), "$1.00", "info");
}
