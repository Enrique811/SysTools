using SysTools.Data.Repositories;
using SysTools.Entities.Products;
using SysTools.Repositories.Tests.TestDoubles;

namespace SysTools.Repositories.Tests.Data;

public sealed class ProductDataReaderMapperTests
{
    [Fact]
    public void Map_preserves_typed_values_accents_and_leading_zeroes()
    {
        const string description = "Árbol, piñata y café";
        using var reader = RepositoryRows.Products(
            [12, "000045", description, "P", 123.45m, -1.5d]).CreateDataReader();
        Assert.True(reader.Read());

        var product = ProductDataReaderMapper.Map(reader);

        Assert.Equal(12, product.Id);
        Assert.Equal("000045", product.Barcode);
        Assert.Equal(description, product.Description);
        Assert.Equal("P", product.Presentation);
        Assert.Equal(123.45m, product.PriceWithTax);
        Assert.Equal("-1.5", product.Stock);
    }

    [Fact]
    public void Map_normalizes_null_description_and_stock_without_inventing_optional_values()
    {
        using var reader = RepositoryRows.Products(
            [8, "8", null, null, null, null]).CreateDataReader();
        Assert.True(reader.Read());

        var product = ProductDataReaderMapper.Map(reader);

        Assert.Equal(string.Empty, product.Description);
        Assert.Null(product.Presentation);
        Assert.Null(product.PriceWithTax);
        Assert.Equal(Product.StockNotRegistered, product.Stock);
    }

    [Fact]
    public void Map_keeps_a_two_hundred_fifty_five_character_description()
    {
        var description = string.Concat(Enumerable.Repeat("á", 255));
        using var reader = RepositoryRows.Products(
            [1, "1", description, "U", 1m, 0d]).CreateDataReader();
        Assert.True(reader.Read());

        var product = ProductDataReaderMapper.Map(reader);

        Assert.Equal(255, product.Description.Length);
        Assert.Equal(description, product.Description);
    }
}
