using SysTools.Business.Barcodes;
using SysTools.Entities.Barcodes;

namespace SysTools.Barcodes.Tests.Business;

public sealed class BarcodeClassifierTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_input_falls_back_to_code128(string? input) =>
        Assert.Equal(BarcodeType.Code128, new BarcodeClassifier().DetectType(input));

    [Fact]
    public void Legacy_normalization_removes_only_characters_up_to_u0020_at_edges()
    {
        var normalized = BarcodeClassifier.Normalize("\0\t\n  96385074 \r\n");
        Assert.Equal("96385074", normalized);
    }

    [Fact]
    public void Legacy_normalization_preserves_interior_controls_and_non_breaking_outer_space()
    {
        Assert.Equal("A\t B", BarcodeClassifier.Normalize(" A\t B "));
        Assert.Equal("\u00A0A\u00A0", BarcodeClassifier.Normalize("\u00A0A\u00A0"));
    }

    [Theory]
    [InlineData("１２３４５６７８")]
    [InlineData("1234-5678")]
    [InlineData("1234567")]
    [InlineData("12 345678")]
    public void Non_ascii_or_non_matching_numeric_values_use_code128(string value) =>
        Assert.Equal(BarcodeType.Code128, new BarcodeClassifier().DetectType(value));

    [Fact]
    public void Leading_zeroes_are_preserved_during_normalization() =>
        Assert.Equal("00000000", BarcodeClassifier.Normalize(" 00000000 "));
}
