using SysTools.Entities.PriceVerifier;
using SysTools.Entities.Products;

namespace SysTools.Presentation.Tests.PriceVerifier;

public sealed class PriceVerifierSecurityTests
{
    [Fact]
    public async Task Presentation_logs_exclude_capture_and_product_payload()
    {
        var logger = new CollectingLogger<SysTools.Presentation.Modules.PriceVerifier.ViewModels.PriceVerifierViewModel>();
        var workflow = TestDoubles.ReadyWorkflow();
        workflow.LookupHandler = (_, _) => Task.FromResult(new PriceVerifierLookupResult(
            PriceVerifierLookupStatus.Success,
            "Encontrado.",
            new Product(1, "SECRET_BARCODE", "SECRET_DESCRIPTION", "Pieza", 1m, "1"),
            "$1.00"));
        var vm = new SysTools.Presentation.Modules.PriceVerifier.ViewModels.PriceVerifierViewModel(
            workflow,
            new ProductSearchDialogServiceStub(),
            logger)
        {
            Barcode = "SECRET_BARCODE"
        };
        await vm.ActivateAsync();
        await vm.SubmitBarcodeCommand.ExecuteAsync();

        var diagnostic = string.Join('|', logger.Entries.SelectMany(entry =>
            entry.Properties.Select(pair => $"{pair.Key}={pair.Value}")));
        Assert.DoesNotContain("SECRET_BARCODE", diagnostic, StringComparison.Ordinal);
        Assert.DoesNotContain("SECRET_DESCRIPTION", diagnostic, StringComparison.Ordinal);
        Assert.All(logger.Entries, entry => Assert.Null(entry.Exception));
    }
}
