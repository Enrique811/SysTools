using SysTools.Entities.Products;

namespace SysTools.Repositories.Tests.Entities;

public sealed class ProductTests
{
    [Fact]
    public void Constructor_preserves_typed_values_and_normalizes_nullable_text()
    {
        var product = new Product(7, "000123", null, null, 18.75m, null);

        Assert.Equal(7, product.Id);
        Assert.Equal("000123", product.Barcode);
        Assert.Equal(string.Empty, product.Description);
        Assert.Null(product.Presentation);
        Assert.Equal(18.75m, product.PriceWithTax);
        Assert.Equal(Product.StockNotRegistered, product.Stock);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_rejects_non_positive_id(int id) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Product(id, "123", "Producto", "P", 1m, "1"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_rejects_missing_barcode(string? barcode) =>
        Assert.ThrowsAny<ArgumentException>(() =>
            new Product(1, barcode!, "Producto", "P", 1m, "1"));

    [Fact]
    public void Constructor_rejects_barcode_longer_than_fifty_characters() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Product(1, new string('1', 51), "Producto", "P", 1m, "1"));

    [Fact]
    public void Constructor_rejects_description_longer_than_two_hundred_fifty_five_characters() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Product(1, "123", new string('a', 256), "P", 1m, "1"));

    [Fact]
    public void Constructor_normalizes_blank_stock() =>
        Assert.Equal(
            Product.StockNotRegistered,
            new Product(1, "123", "Producto", "P", null, "  ").Stock);

    [Fact]
    public void ToString_does_not_expose_product_payload()
    {
        var product = new Product(91, "Systools-Repo-Barcode-Secret", "Descripcion privada", "P", 99m, "7");

        var text = product.ToString();

        Assert.Contains("91", text, StringComparison.Ordinal);
        Assert.DoesNotContain(product.Barcode, text, StringComparison.Ordinal);
        Assert.DoesNotContain(product.Description, text, StringComparison.Ordinal);
        Assert.DoesNotContain("99", text, StringComparison.Ordinal);
        Assert.DoesNotContain("7", text, StringComparison.Ordinal);
    }
}
