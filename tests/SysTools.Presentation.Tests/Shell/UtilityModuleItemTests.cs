using SysTools.Presentation.Modules.PriceVerifier.ViewModels;
using SysTools.Presentation.Shell.Models;

namespace SysTools.Presentation.Tests.Shell;

public sealed class UtilityModuleItemTests
{
    [Theory]
    [InlineData("")]
    [InlineData("PriceVerifier")]
    [InlineData("price_verifier")]
    public void Id_must_be_nonempty_kebab_case(string id) =>
        Assert.ThrowsAny<ArgumentException>(() => new UtilityModuleItem(id, "Módulo", ModuleSection.Utilities, true, new PriceVerifierViewModel()));

    [Fact]
    public void Enabled_module_requires_content() => Assert.Throws<ArgumentException>(() => new UtilityModuleItem("valid-id", "Módulo", ModuleSection.Utilities, true));

    [Fact]
    public void Initial_catalog_has_unique_ids_and_one_selection()
    {
        var shell = TestDoubles.CreateShell();
        Assert.Equal(shell.Modules.Count, shell.Modules.Select(x => x.Id).Distinct().Count());
        Assert.Single(shell.Modules, x => x.IsSelected);
        Assert.Single(shell.Modules, x => x.IsEnabled);
    }
}
