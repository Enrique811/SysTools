using SysTools.Entities.Connection;
using SysTools.Entities.Licensing;
using SysTools.Entities.PriceVerifier;
using SysTools.Presentation.Shell.Models;

namespace SysTools.Presentation.Tests.Shell;

public sealed class ShellStatusTests
{
    [Fact]
    public async Task Shell_proxies_module_status_without_divergent_copies()
    {
        var workflow = TestDoubles.ReadyWorkflow();
        var vm = TestDoubles.CreatePriceVerifier(workflow);
        var shell = TestDoubles.CreateShell(priceVerifier: vm);

        await vm.ActivateAsync();

        Assert.Equal(AvailabilityStatus.Available, shell.ConnectionStatus);
        Assert.Equal(AvailabilityStatus.Available, shell.LicenseStatus);
        Assert.Equal(vm.StatusMessage, shell.StatusMessage);

        workflow.PrepareHandler = _ => Task.FromResult(new PriceVerifierPreparationResult(
            PriceVerifierPreparationStatus.LicenseUnavailable,
            "Licencia no disponible.",
            ConnectionTestStatus.Success,
            LicenseValidationStatus.Expired));
        vm.Deactivate();
        await vm.RetryCommand.ExecuteAsync();

        Assert.Equal(AvailabilityStatus.Available, shell.ConnectionStatus);
        Assert.Equal(AvailabilityStatus.Error, shell.LicenseStatus);
        Assert.Same(vm.StatusMessage, shell.StatusMessage);
    }
}
