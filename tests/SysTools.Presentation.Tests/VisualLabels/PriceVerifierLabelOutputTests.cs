using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Business.Labels;
using SysTools.Entities.Labels;
using SysTools.Entities.PriceVerifier;
using SysTools.Entities.Products;
using SysTools.Presentation.Modules.Labels;
using SysTools.Presentation.Modules.PriceVerifier.ViewModels;

namespace SysTools.Presentation.Tests.VisualLabels;

public sealed class PriceVerifierLabelOutputTests
{
    [Fact]
    public async Task Capture_uses_private_lookup_product_shows_progress_and_cancel_resets_row()
    {
        var product = new Product(1, "SKU-01", "Product", "Piece", 10m, "1");
        var workflow = TestDoubles.ReadyWorkflow();
        workflow.LookupHandler = (_, _) => Task.FromResult(new PriceVerifierLookupResult(PriceVerifierLookupStatus.Success, "ok", product, "$10.00"));
        var labels = new LabelWorkflowStub { CaptureResult = new(LabelOutputStatus.Pending, "pending", 1, 2) };
        var vm = Create(workflow, labels, new PreviewStub());
        await vm.ActivateAsync();
        vm.Barcode = "SKU-01";
        await vm.SubmitBarcodeCommand.ExecuteAsync();

        await vm.CaptureLabelCommand.ExecuteAsync();

        Assert.Same(product, labels.LastProduct);
        Assert.Equal("$10.00", labels.LastPrice);
        Assert.Equal("Fila de etiquetas: 1/2; faltan 1.", vm.LabelProgressText);
        Assert.True(vm.CanCancelPendingLabels);
        vm.CancelPendingLabelsCommand.Execute(null);
        Assert.Equal(1, labels.CancelCalls);
        Assert.False(vm.CanCancelPendingLabels);
    }

    [Fact]
    public async Task Preview_is_opened_once_and_deactivation_blocks_late_publication()
    {
        var product = new Product(1, "SKU-01", "Product", "Piece", 10m, "1");
        var workflow = TestDoubles.ReadyWorkflow();
        workflow.LookupHandler = (_, _) => Task.FromResult(new PriceVerifierLookupResult(PriceVerifierLookupStatus.Success, "ok", product, "$10.00"));
        var completion = new TaskCompletionSource<LabelOutputResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var labels = new LabelWorkflowStub { CaptureHandler = (_, _, _) => completion.Task };
        var preview = new PreviewStub();
        var vm = Create(workflow, labels, preview);
        await vm.ActivateAsync();
        vm.Barcode = "SKU-01";
        await vm.SubmitBarcodeCommand.ExecuteAsync();
        var capture = vm.CaptureLabelCommand.ExecuteAsync();
        vm.Deactivate();
        completion.TrySetResult(new(LabelOutputStatus.PreviewReady, "ready", preview: Document()));
        await capture;
        Assert.Equal(0, preview.ShowCalls);
        Assert.Equal(1, labels.InvalidateCalls);
        Assert.Equal("Sin etiquetas pendientes.", vm.LabelProgressText);
    }

    private static PriceVerifierViewModel Create(PriceVerifierWorkflowStub workflow, LabelWorkflowStub labels, PreviewStub preview) =>
        new(workflow, new ProductSearchDialogServiceStub(), NullLogger<PriceVerifierViewModel>.Instance, labelOutput: labels, labelPreview: preview);
    private static LabelPreviewDocument Document() => new(Guid.NewGuid().ToString("N"), 1, 1, 57, 40, [1]);

    private sealed class LabelWorkflowStub : ILabelOutputWorkflow
    {
        internal LabelOutputResult CaptureResult { get; set; } = new(LabelOutputStatus.Printed, "printed");
        internal Func<Product, string, CancellationToken, Task<LabelOutputResult>>? CaptureHandler { get; set; }
        internal Product? LastProduct { get; private set; }
        internal string? LastPrice { get; private set; }
        internal int CancelCalls { get; private set; }
        internal int InvalidateCalls { get; private set; }
        public Task<LabelOutputResult> CaptureAsync(Product product, string price, CancellationToken token=default) { LastProduct=product; LastPrice=price; return CaptureHandler?.Invoke(product, price, token) ?? Task.FromResult(CaptureResult); }
        public Task<LabelOutputResult> RetryAsync(CancellationToken token=default) => Task.FromResult(CaptureResult);
        public LabelCancelResult Cancel() { CancelCalls++; return new(1, false); }
        public LabelWorkflowSnapshot GetState() => new(0, 0, false, false, false);
        public void Invalidate() => InvalidateCalls++;
    }

    private sealed class PreviewStub : ILabelPreviewDialogService
    {
        internal int ShowCalls { get; private set; }
        public Task ShowAsync(LabelPreviewDocument document, CancellationToken token=default) { ShowCalls++; return Task.CompletedTask; }
        public void CloseActive() { }
    }
}
