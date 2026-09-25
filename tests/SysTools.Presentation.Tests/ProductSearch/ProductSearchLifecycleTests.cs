using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Entities.PriceVerifier;
using SysTools.Presentation.Modules.PriceVerifier.Search;

namespace SysTools.Presentation.Tests.ProductSearch;

public sealed class ProductSearchLifecycleTests
{
    [Fact]
    public async Task Deactivate_suppresses_a_late_noncooperative_response_and_allows_reopen()
    {
        var pending = new ControlledOperation<PriceVerifierSearchResult>();
        var workflow = TestDoubles.ReadyWorkflow();
        workflow.SearchHandler = (_, _) => pending.Task;
        var vm = new ProductSearchViewModel(workflow, NullLogger<ProductSearchViewModel>.Instance)
        {
            SearchText = "old"
        };
        vm.Activate();
        var search = vm.SearchCommand.ExecuteAsync();

        vm.Deactivate();
        pending.Complete(new PriceVerifierSearchResult(
            PriceVerifierSearchStatus.Success,
            "Old",
            [new PriceVerifierSearchItem("OLD", "Old", null, "1")]));
        await search;

        Assert.Empty(vm.Items);
        vm.Activate();
        Assert.True(vm.SearchFocusRequestVersion >= 2);
    }
}
