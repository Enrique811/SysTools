using SysTools.BusinessRules.Tests.TestDoubles;
using SysTools.Entities.Labels;

namespace SysTools.BusinessRules.Tests.Labels;

public sealed class LabelDataTests
{
    [Fact]
    public void Product_is_required()
    {
        Assert.Throws<ArgumentNullException>(() => new LabelData(null!, "$1.00", "info"));
    }

    [Fact]
    public void Product_without_price_is_rejected()
    {
        var product = ProductTestFactory.Create(priceWithTax: null);

        Assert.Throws<ArgumentException>(() => new LabelData(product, "$0.00", "info"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Formatted_price_is_required(string? formattedPrice)
    {
        Assert.Throws<ArgumentException>(() =>
            new LabelData(ProductTestFactory.Create(), formattedPrice!, "info"));
    }

    [Fact]
    public void Null_information_becomes_empty()
    {
        var data = new LabelData(ProductTestFactory.Create(), "$123.45", null);

        Assert.Equal(string.Empty, data.Information);
    }

    [Fact]
    public void Unicode_and_intentional_spaces_are_preserved_exactly()
    {
        const string information = "  Árbol  ñ  50%  ";

        var data = new LabelData(ProductTestFactory.Create(), "$123.45", information);

        Assert.Equal(information, data.Information);
    }

    [Fact]
    public void To_string_does_not_expose_label_payload()
    {
        var product = ProductTestFactory.Create(
            barcode: "SECRET-CODE",
            priceWithTax: 999.99m,
            description: "SECRET-DESCRIPTION");
        var data = new LabelData(product, "$999.99", "SECRET-INFORMATION");

        var text = data.ToString();

        Assert.DoesNotContain("SECRET-CODE", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SECRET-DESCRIPTION", text, StringComparison.Ordinal);
        Assert.DoesNotContain("999.99", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SECRET-INFORMATION", text, StringComparison.Ordinal);
    }
}
