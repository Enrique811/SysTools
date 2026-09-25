using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Entities.PriceVerifier;
using SysTools.Presentation.Modules.PriceVerifier.Search;

namespace SysTools.Presentation.Tests.ProductSearch;

public sealed class ProductSearchViewModelTests
{
    [Fact]
    public async Task Search_publishes_snapshot_selects_first_and_requests_results_focus()
    {
        var workflow = TestDoubles.ReadyWorkflow();
        workflow.SearchHandler = (_, _) => Task.FromResult(new PriceVerifierSearchResult(
            PriceVerifierSearchStatus.Success,
            "Encontrados.",
            [new PriceVerifierSearchItem("001", "Cafe", "$1", "2") ]));
        var vm = Create(workflow);
        vm.Activate();
        vm.SearchText = "ca";

        await vm.SearchCommand.ExecuteAsync();

        Assert.Single(vm.Items);
        Assert.Equal("001", vm.SelectedItem!.Barcode);
        Assert.True(vm.ResultsFocusRequestVersion > 0);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Missing_or_no_matches_returns_focus_to_input()
    {
        var vm = Create(TestDoubles.ReadyWorkflow());
        vm.Activate();
        var initial = vm.SearchFocusRequestVersion;

        await vm.SearchCommand.ExecuteAsync();

        Assert.True(vm.SearchFocusRequestVersion > initial);
        Assert.Empty(vm.Items);
    }

    [Fact]
    public async Task Operational_failure_requests_close_without_exposing_payload()
    {
        var workflow = TestDoubles.ReadyWorkflow();
        workflow.SearchHandler = (_, _) => Task.FromResult(new PriceVerifierSearchResult(
            PriceVerifierSearchStatus.OperationalFailure,
            "No disponible."));
        var vm = Create(workflow);
        ProductSearchDialogResult? result = null;
        vm.CloseRequested += (_, value) => result = value;
        vm.Activate();

        await vm.SearchCommand.ExecuteAsync();

        Assert.Equal(ProductSearchDialogOutcome.OperationalFailure, result!.Outcome);
    }

    private static ProductSearchViewModel Create(PriceVerifierWorkflowStub workflow) =>
        new(workflow, NullLogger<ProductSearchViewModel>.Instance);
}
