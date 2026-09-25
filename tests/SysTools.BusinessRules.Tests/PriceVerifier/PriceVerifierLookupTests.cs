using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Business.PriceVerifier;
using SysTools.BusinessRules.Tests.TestDoubles;
using SysTools.Entities.PriceVerifier;
using SysTools.Entities.Products;

namespace SysTools.BusinessRules.Tests.PriceVerifier;

public sealed class PriceVerifierLookupTests
{
    [Fact]
    public async Task Lookup_normalizes_outer_spaces_and_preserves_text_identity()
    {
        var productService = new ProductServiceStub();
        productService.GetHandler = (_, barcode, _) => Task.FromResult<Product?>(
            new Product(1, barcode!, "Café", "Pieza", 12.5m, "3"));
        var formatter = new PriceFormatterServiceStub();
        var workflow = Create(productService, formatter);
        await workflow.PrepareAsync();

        var result = await workflow.LookupAsync("  001 23  ");

        Assert.Equal(PriceVerifierLookupStatus.Success, result.Status);
        Assert.Equal("001 23", productService.LastBarcode);
        Assert.Equal("001 23", result.Product!.Barcode);
        Assert.Equal("MX", formatter.LastFormat);
        Assert.Equal(1, productService.GetCalls);
        Assert.Equal(1, formatter.Calls);
    }

    [Theory]
    [InlineData(null, PriceVerifierLookupStatus.MissingInput)]
    [InlineData("   ", PriceVerifierLookupStatus.MissingInput)]
    [InlineData("123456789012345678901234567890123456789012345678901", PriceVerifierLookupStatus.InputTooLong)]
    public async Task Invalid_input_does_not_query_catalog(string? barcode, PriceVerifierLookupStatus expected)
    {
        var products = new ProductServiceStub();
        var workflow = Create(products, new PriceFormatterServiceStub());
        await workflow.PrepareAsync();

        var result = await workflow.LookupAsync(barcode);

        Assert.Equal(expected, result.Status);
        Assert.Equal(0, products.GetCalls);
    }

    [Fact]
    public async Task Fifty_characters_are_accepted_and_not_found_remains_nonblocking()
    {
        var products = new ProductServiceStub();
        var workflow = Create(products, new PriceFormatterServiceStub());
        await workflow.PrepareAsync();

        var result = await workflow.LookupAsync(new string('0', 50));
        var second = await workflow.LookupAsync("next");

        Assert.Equal(PriceVerifierLookupStatus.NotFound, result.Status);
        Assert.Equal(PriceVerifierLookupStatus.NotFound, second.Status);
        Assert.Equal(2, products.GetCalls);
    }

    [Fact]
    public async Task Product_without_price_does_not_call_formatter()
    {
        var products = new ProductServiceStub
        {
            GetHandler = (_, barcode, _) => Task.FromResult<Product?>(new Product(1, barcode!, "Producto", null, null, null))
        };
        var formatter = new PriceFormatterServiceStub();
        var workflow = Create(products, formatter);
        await workflow.PrepareAsync();

        var result = await workflow.LookupAsync("01");

        Assert.True(result.IsSuccess);
        Assert.Null(result.FormattedPrice);
        Assert.Equal(0, formatter.Calls);
    }

    internal static PriceVerifierWorkflow Create(
        ProductServiceStub products,
        PriceFormatterServiceStub formatter,
        CollectingLogger<PriceVerifierWorkflow>? logger = null,
        ConfigurationServiceStub? configuration = null) => new(
            configuration ?? ReadyConfiguration(),
            Connected(),
            Licensed(),
            products,
            formatter,
            logger ?? new CollectingLogger<PriceVerifierWorkflow>());

    internal static ConfigurationServiceStub ReadyConfiguration()
    {
        var service = new ConfigurationServiceStub();
        service.LoadHandler = _ => Task.FromResult(PriceVerifierTestData.Loaded());
        return service;
    }

    private static ConnectionTestServiceStub Connected()
    {
        var service = new ConnectionTestServiceStub();
        service.Handler = (_, _) => Task.FromResult(PriceVerifierTestData.Connected());
        return service;
    }

    private static LicenseServiceStub Licensed()
    {
        var service = new LicenseServiceStub();
        service.Handler = (_, _, _) => Task.FromResult(PriceVerifierTestData.ValidLicense());
        return service;
    }
}
