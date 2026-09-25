using SysTools.Entities.Connection;
using SysTools.Entities.Licensing;
using SysTools.Entities.PriceVerifier;

namespace SysTools.Presentation.Tests.PriceVerifier;

public sealed class PriceVerifierViewModelTests
{
    [Fact]
    public void Starts_neutral_and_unavailable()
    {
        var vm = TestDoubles.CreatePriceVerifier();
        Assert.False(string.IsNullOrWhiteSpace(vm.ModuleTitle));
        Assert.Equal(string.Empty, vm.Barcode);
        Assert.Equal(string.Empty, vm.AdditionalInformation);
        Assert.All(
            new[] { vm.ProductDescription, vm.ProductPresentation, vm.StockDisplay, vm.FinalPriceDisplay },
            value => Assert.Equal("—", value));
        Assert.False(vm.IsBarcodeAvailable || vm.IsAdditionalInformationAvailable
            || vm.IsSearchAvailable || vm.IsPrintAvailable || vm.IsSettingsAvailable);
        Assert.NotNull(vm.GetType().GetProperty(nameof(vm.Barcode))!.SetMethod);
    }

    [Fact]
    public async Task Activation_publishes_pending_then_ready_and_requests_focus()
    {
        var pending = new ControlledOperation<PriceVerifierPreparationResult>();
        var workflow = TestDoubles.ReadyWorkflow();
        workflow.PrepareHandler = _ => pending.Task;
        var vm = TestDoubles.CreatePriceVerifier(workflow);

        var activation = vm.ActivateAsync();
        Assert.True(vm.IsBusy);
        Assert.False(vm.IsBarcodeAvailable);

        pending.Complete(new PriceVerifierPreparationResult(
            PriceVerifierPreparationStatus.Ready,
            "Disponible.",
            ConnectionTestStatus.Success,
            LicenseValidationStatus.Valid,
            "Informacion"));
        await activation;

        Assert.True(vm.IsBarcodeAvailable);
        Assert.Equal("Informacion", vm.AdditionalInformation);
        Assert.Equal(1, vm.FocusRequestVersion);
    }

    [Fact]
    public async Task Retry_repeats_complete_preparation_after_block()
    {
        var workflow = TestDoubles.ReadyWorkflow();
        var calls = 0;
        workflow.PrepareHandler = _ => Task.FromResult(++calls == 1
            ? new PriceVerifierPreparationResult(
                PriceVerifierPreparationStatus.ConnectionUnavailable,
                "Sin conexion.",
                ConnectionTestStatus.Timeout)
            : new PriceVerifierPreparationResult(
                PriceVerifierPreparationStatus.Ready,
                "Disponible.",
                ConnectionTestStatus.Success,
                LicenseValidationStatus.Valid));
        var vm = TestDoubles.CreatePriceVerifier(workflow);

        await vm.ActivateAsync();
        Assert.True(vm.CanRetry);
        await vm.RetryCommand.ExecuteAsync();

        Assert.Equal(2, workflow.PrepareCalls);
        Assert.True(vm.IsBarcodeAvailable);
    }
}
