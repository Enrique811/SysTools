using SysTools.Entities.PriceVerifier;
using SysTools.Entities.Products;

namespace SysTools.Presentation.Tests.PriceVerifier;

public sealed class PriceVerifierReliabilityTests
{
    [Fact]
    public async Task One_hundred_sequential_queries_have_no_duplicates_or_crossed_results()
    {
        var workflow = TestDoubles.ReadyWorkflow();
        workflow.LookupHandler = (barcode, _) => Task.FromResult(new PriceVerifierLookupResult(
            PriceVerifierLookupStatus.Success,
            "Encontrado.",
            new Product(int.Parse(barcode!) + 1, barcode!, $"Producto {barcode}", "Pieza", 1m, "1"),
            "$1.00"));
        var vm = TestDoubles.CreatePriceVerifier(workflow);
        await vm.ActivateAsync();

        for (var index = 0; index < 100; index++)
        {
            vm.Barcode = index.ToString("D3");
            await vm.SubmitBarcodeCommand.ExecuteAsync();
            Assert.Equal($"Producto {index:D3}", vm.ProductDescription);
        }

        Assert.Equal(100, workflow.LookupCalls);
        Assert.Equal("099", vm.Barcode);
        Assert.Equal("Producto 099", vm.ProductDescription);
    }
}
