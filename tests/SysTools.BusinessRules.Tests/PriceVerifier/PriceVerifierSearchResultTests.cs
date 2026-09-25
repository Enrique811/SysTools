using SysTools.Entities.PriceVerifier;
using SysTools.Entities.Products;

namespace SysTools.BusinessRules.Tests.PriceVerifier;

public sealed class PriceVerifierSearchResultTests
{
    [Fact]
    public void Item_normalizes_optional_values_and_preserves_barcode()
    {
        var item = new PriceVerifierSearchItem("001", null, null, " ");

        Assert.Equal("001", item.Barcode);
        Assert.Equal(string.Empty, item.Description);
        Assert.Null(item.FormattedPrice);
        Assert.Equal(Product.StockNotRegistered, item.Stock);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Item_rejects_missing_barcode(string? barcode) =>
        Assert.Throws<ArgumentException>(() => new PriceVerifierSearchItem(barcode!, "x", null, "1"));

    [Fact]
    public void Item_enforces_text_limits()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PriceVerifierSearchItem(new string('1', 51), "x", null, "1"));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PriceVerifierSearchItem("1", new string('x', 256), null, "1"));
    }

    [Fact]
    public void Success_requires_items_and_takes_read_only_snapshot()
    {
        var source = new List<PriceVerifierSearchItem>
        {
            new("01", "Producto", "$1.00", "2")
        };
        var result = new PriceVerifierSearchResult(PriceVerifierSearchStatus.Success, "Ok", source);
        source.Clear();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Items);
        var collection = Assert.IsAssignableFrom<ICollection<PriceVerifierSearchItem>>(result.Items);
        Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => collection.Add(new("02", "Otro", null, "1")));
        Assert.Throws<ArgumentException>(() =>
            new PriceVerifierSearchResult(PriceVerifierSearchStatus.Success, "Ok"));
    }

    [Theory]
    [InlineData(PriceVerifierSearchStatus.NoMatches)]
    [InlineData(PriceVerifierSearchStatus.MissingInput)]
    [InlineData(PriceVerifierSearchStatus.InputTooLong)]
    [InlineData(PriceVerifierSearchStatus.OperationalFailure)]
    public void Non_success_requires_empty_items(PriceVerifierSearchStatus status)
    {
        var item = new PriceVerifierSearchItem("01", "Producto", null, "1");
        var result = new PriceVerifierSearchResult(status, "Mensaje");

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Items);
        Assert.Throws<ArgumentException>(() => new PriceVerifierSearchResult(status, "Mensaje", [item]));
    }
}
