using SysTools.Entities.PriceVerifier;
using SysTools.Entities.Products;
using SysTools.Presentation.Modules.PriceVerifier.Search;

namespace SysTools.Presentation.Tests.ProductSearch;

public sealed class ProductSearchReliabilityTests
{
    [Fact]
    public async Task One_hundred_sequential_selections_issue_one_exact_lookup_each()
    {
        var workflow = TestDoubles.ReadyWorkflow();
        workflow.LookupHandler = (barcode, _) => Task.FromResult(new PriceVerifierLookupResult(
            PriceVerifierLookupStatus.Success,
            "Ok",
            new Product(int.Parse(barcode!), barcode!, $"Producto {barcode}", null, 1m, "1"),
            "$1"));
        var next = 0;
        var dialog = new ProductSearchDialogServiceStub
        {
            Handler = _ => ProductSearchDialogResult.Selected((++next).ToString("D3"))
        };
        var vm = TestDoubles.CreatePriceVerifier(workflow, dialog);
        await vm.ActivateAsync();

        for (var index = 0; index < 100; index++)
        {
            await vm.OpenSearchCommand.ExecuteAsync();
        }

        Assert.Equal(100, dialog.ShowCalls);
        Assert.Equal(100, workflow.LookupCalls);
        Assert.Equal("100", vm.Barcode);
        Assert.Equal("Producto 100", vm.ProductDescription);
    }
}
