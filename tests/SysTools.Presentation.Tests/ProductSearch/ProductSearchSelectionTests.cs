using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Entities.PriceVerifier;
using SysTools.Presentation.Modules.PriceVerifier.Search;

namespace SysTools.Presentation.Tests.ProductSearch;

public sealed class ProductSearchSelectionTests
{
    [Fact]
    public void Selection_is_exact_and_completed_once()
    {
        var vm = new ProductSearchViewModel(
            TestDoubles.ReadyWorkflow(),
            NullLogger<ProductSearchViewModel>.Instance);
        var item = new PriceVerifierSearchItem("00017", "Producto", null, "1");
        var results = new List<ProductSearchDialogResult>();
        vm.CloseRequested += (_, result) => results.Add(result);
        vm.Activate();
        vm.SelectedItem = item;

        vm.SelectCommand.Execute(null);
        vm.SelectCommand.Execute(item);
        vm.CancelCommand.Execute(null);

        var result = Assert.Single(results);
        Assert.Equal(ProductSearchDialogOutcome.Selected, result.Outcome);
        Assert.Equal("00017", result.SelectedBarcode);
    }

    [Fact]
    public void Invalid_selection_does_not_close()
    {
        var vm = new ProductSearchViewModel(
            TestDoubles.ReadyWorkflow(),
            NullLogger<ProductSearchViewModel>.Instance);
        var closes = 0;
        vm.CloseRequested += (_, _) => closes++;
        vm.Activate();

        vm.SelectCommand.Execute(null);

        Assert.Equal(0, closes);
    }
}
