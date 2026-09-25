using SysTools.Entities.Connection;
using SysTools.Entities.Licensing;
using SysTools.Entities.PriceVerifier;
using SysTools.Entities.Products;

namespace SysTools.Presentation.Tests.PriceVerifier;

public sealed class PriceVerifierLifecycleTests
{
    [Fact]
    public async Task Deactivate_suppresses_late_lookup_publication()
    {
        var pending = new ControlledOperation<PriceVerifierLookupResult>();
        var workflow = TestDoubles.ReadyWorkflow();
        workflow.LookupHandler = (_, _) => pending.Task;
        var vm = TestDoubles.CreatePriceVerifier(workflow);
        await vm.ActivateAsync();
        vm.Barcode = "001";
        var lookup = vm.SubmitBarcodeCommand.ExecuteAsync();

        vm.Deactivate();
        pending.Complete(new PriceVerifierLookupResult(
            PriceVerifierLookupStatus.Success,
            "Encontrado.",
            new Product(1, "001", "Tardío", "Pieza", 1m, "1"),
            "$1.00"));
        await lookup;

        Assert.Equal("—", vm.ProductDescription);
        Assert.False(vm.IsBarcodeAvailable);
        Assert.Equal(1, workflow.InvalidateCalls);
    }

    [Fact]
    public async Task Older_noncooperative_activation_cannot_overwrite_newer_state()
    {
        var first = new ControlledOperation<PriceVerifierPreparationResult>();
        var second = new ControlledOperation<PriceVerifierPreparationResult>();
        var calls = 0;
        var workflow = TestDoubles.ReadyWorkflow();
        workflow.PrepareHandler = _ => ++calls == 1 ? first.Task : second.Task;
        var vm = TestDoubles.CreatePriceVerifier(workflow);

        var oldActivation = vm.ActivateAsync();
        var newActivation = vm.ActivateAsync();
        second.Complete(Ready("Nueva"));
        await newActivation;
        first.Complete(Ready("Antigua"));
        await oldActivation;

        Assert.Equal("Nueva", vm.AdditionalInformation);
        Assert.True(vm.IsBarcodeAvailable);
    }

    private static PriceVerifierPreparationResult Ready(string information) => new(
        PriceVerifierPreparationStatus.Ready,
        "Disponible.",
        ConnectionTestStatus.Success,
        LicenseValidationStatus.Valid,
        information);
}
