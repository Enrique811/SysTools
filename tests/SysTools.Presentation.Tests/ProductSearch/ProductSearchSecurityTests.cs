using SysTools.Entities.PriceVerifier;
using SysTools.Presentation.Modules.PriceVerifier.Search;

namespace SysTools.Presentation.Tests.ProductSearch;

public sealed class ProductSearchSecurityTests
{
    [Fact]
    public async Task Dialog_logs_exclude_criterion_and_result_payload()
    {
        const string criterion = "SECRET_CRITERION";
        var workflow = TestDoubles.ReadyWorkflow();
        workflow.SearchHandler = (_, _) => Task.FromResult(new PriceVerifierSearchResult(
            PriceVerifierSearchStatus.Success,
            "Encontrado.",
            [new PriceVerifierSearchItem("SECRET_BARCODE", "SECRET_DESCRIPTION", "$1", "1")]));
        var logger = new CollectingLogger<ProductSearchViewModel>();
        var vm = new ProductSearchViewModel(workflow, logger) { SearchText = criterion };
        vm.Activate();

        await vm.SearchCommand.ExecuteAsync();

        var diagnostic = string.Join('|', logger.Entries.SelectMany(entry =>
            entry.Properties.Select(pair => $"{pair.Key}={pair.Value}")));
        Assert.DoesNotContain(criterion, diagnostic, StringComparison.Ordinal);
        Assert.DoesNotContain("SECRET_BARCODE", diagnostic, StringComparison.Ordinal);
        Assert.DoesNotContain("SECRET_DESCRIPTION", diagnostic, StringComparison.Ordinal);
        Assert.All(logger.Entries, entry => Assert.Null(entry.Exception));
    }
}
