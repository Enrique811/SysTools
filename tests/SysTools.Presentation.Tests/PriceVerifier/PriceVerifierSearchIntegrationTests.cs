using SysTools.Entities.PriceVerifier;
using SysTools.Entities.Products;
using SysTools.Presentation.Modules.PriceVerifier.Search;

namespace SysTools.Presentation.Tests.PriceVerifier;

public sealed class PriceVerifierSearchIntegrationTests
{
    [Fact]
    public async Task Selected_barcode_reuses_exact_lookup_once()
    {
        var workflow = TestDoubles.ReadyWorkflow();
        string? lookedUp = null;
        workflow.LookupHandler = (barcode, _) =>
        {
            lookedUp = barcode;
            return Task.FromResult(new PriceVerifierLookupResult(
                PriceVerifierLookupStatus.Success,
                "Encontrado.",
                new Product(1, barcode!, "Producto", null, 1m, "1"),
                "$1.00"));
        };
        var dialog = new ProductSearchDialogServiceStub
        {
            Handler = _ => ProductSearchDialogResult.Selected("00042")
        };
        var vm = TestDoubles.CreatePriceVerifier(workflow, dialog);
        await vm.ActivateAsync();

        await vm.OpenSearchCommand.ExecuteAsync();

        Assert.Equal("00042", vm.Barcode);
        Assert.Equal("00042", lookedUp);
        Assert.Equal(1, workflow.LookupCalls);
        Assert.Equal("Producto", vm.ProductDescription);
    }

    [Fact]
    public async Task Cancel_preserves_product_and_returns_focus()
    {
        var workflow = TestDoubles.ReadyWorkflow();
        workflow.LookupHandler = (_, _) => Task.FromResult(new PriceVerifierLookupResult(
            PriceVerifierLookupStatus.Success,
            "Encontrado.",
            new Product(1, "1", "Anterior", null, 1m, "1"),
            "$1.00"));
        var vm = TestDoubles.CreatePriceVerifier(workflow);
        await vm.ActivateAsync();
        vm.Barcode = "1";
        await vm.SubmitBarcodeCommand.ExecuteAsync();
        var focus = vm.FocusRequestVersion;

        await vm.OpenSearchCommand.ExecuteAsync();

        Assert.Equal("Anterior", vm.ProductDescription);
        Assert.Equal(focus + 1, vm.FocusRequestVersion);
        Assert.Equal(1, workflow.LookupCalls);
    }

    [Fact]
    public async Task Search_operational_failure_blocks_until_retry()
    {
        var dialog = new ProductSearchDialogServiceStub
        {
            Handler = _ => ProductSearchDialogResult.OperationalFailure()
        };
        var vm = TestDoubles.CreatePriceVerifier(searchDialog: dialog);
        await vm.ActivateAsync();

        await vm.OpenSearchCommand.ExecuteAsync();

        Assert.False(vm.IsAvailable);
        Assert.True(vm.CanRetry);
        Assert.False(vm.IsSearchAvailable);
    }
}
