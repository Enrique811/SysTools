using SysTools.Presentation.Modules.PriceVerifier.ViewModels;

namespace SysTools.Presentation.Tests.Shell;

public sealed class ShellViewModelTests
{
    [Fact]
    public void Starts_with_expected_catalog_and_price_verifier_content()
    {
        var shell = TestDoubles.CreateShell();
        Assert.Equal(new[] { "price-verifier", "label-printing", "quick-inventory", "new-utility", "settings", "licensing", "system-logs" }, shell.Modules.Select(x => x.Id));
        Assert.Equal("price-verifier", shell.ActiveModule.Id);
        Assert.IsType<PriceVerifierViewModel>(shell.ActiveModuleContent);
    }

    [Fact]
    public void Disabled_module_cannot_replace_active_module()
    {
        var shell = TestDoubles.CreateShell();
        Assert.False(shell.SelectModule(shell.Modules[1]));
        Assert.Equal("price-verifier", shell.ActiveModule.Id);
    }
}
