using SysTools.Entities.PriceVerifier;
using SysTools.Entities.Products;

namespace SysTools.Presentation.Tests.PriceVerifier;

public sealed class PriceVerifierQueryTests
{
    [Fact]
    public async Task Submit_publishes_product_atomically_retains_code_and_requests_focus()
    {
        var workflow = TestDoubles.ReadyWorkflow();
        workflow.LookupHandler = (barcode, _) => Task.FromResult(new PriceVerifierLookupResult(
            PriceVerifierLookupStatus.Success,
            "Producto encontrado.",
            new Product(1, barcode!, "Café molido", "Bolsa", 25.50m, "7"),
            "$25.50"));
        var vm = TestDoubles.CreatePriceVerifier(workflow);
        await vm.ActivateAsync();
        var focusBefore = vm.FocusRequestVersion;
        vm.Barcode = "001";

        await vm.SubmitBarcodeCommand.ExecuteAsync();

        Assert.Equal("001", vm.Barcode);
        Assert.Equal("Café molido", vm.ProductDescription);
        Assert.Equal("Bolsa", vm.ProductPresentation);
        Assert.Equal("7", vm.StockDisplay);
        Assert.Equal("$25.50", vm.FinalPriceDisplay);
        Assert.Equal(focusBefore + 1, vm.FocusRequestVersion);
        Assert.Equal(1, workflow.LookupCalls);
    }

    [Fact]
    public async Task Confirmation_is_single_flight_and_disables_capture_while_busy()
    {
        var pending = new ControlledOperation<PriceVerifierLookupResult>();
        var workflow = TestDoubles.ReadyWorkflow();
        workflow.LookupHandler = (_, _) => pending.Task;
        var vm = TestDoubles.CreatePriceVerifier(workflow);
        await vm.ActivateAsync();
        vm.Barcode = "001";

        var first = vm.SubmitBarcodeCommand.ExecuteAsync();
        var second = vm.SubmitBarcodeCommand.ExecuteAsync();

        Assert.True(vm.IsBusy);
        Assert.False(vm.IsBarcodeAvailable);
        Assert.Equal(1, workflow.LookupCalls);
        Assert.True(second.IsCompletedSuccessfully);
        pending.Complete(new PriceVerifierLookupResult(PriceVerifierLookupStatus.NotFound, "No encontrado."));
        await first;
        Assert.True(vm.IsBarcodeAvailable);
    }

    [Theory]
    [InlineData(PriceVerifierLookupStatus.MissingInput)]
    [InlineData(PriceVerifierLookupStatus.InputTooLong)]
    [InlineData(PriceVerifierLookupStatus.NotFound)]
    public async Task Nonblocking_results_leave_module_ready(PriceVerifierLookupStatus status)
    {
        var workflow = TestDoubles.ReadyWorkflow();
        workflow.LookupHandler = (_, _) => Task.FromResult(new PriceVerifierLookupResult(status, "Intente nuevamente."));
        var vm = TestDoubles.CreatePriceVerifier(workflow);
        await vm.ActivateAsync();

        await vm.SubmitBarcodeCommand.ExecuteAsync();

        Assert.True(vm.IsBarcodeAvailable);
        Assert.False(vm.CanRetry);
        Assert.Equal("—", vm.ProductDescription);
    }
}
