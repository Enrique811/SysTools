using SysTools.Entities.Barcodes;
using SysTools.Entities.Labels;
using SysTools.Entities.Products;

namespace SysTools.BusinessRules.Tests.Labels;

public sealed class LabelOutputEntityTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void Row_context_rejects_capacity_outside_supported_range(int capacity) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new LabelRowContext(LabelOutputEnvironment.Preview, capacity, "label-1.label.json", "", ""));

    [Fact]
    public void Production_context_requires_exact_printer_identifier() =>
        Assert.Throws<ArgumentException>(() =>
            new LabelRowContext(LabelOutputEnvironment.Production, 1, "label-1.label.json", " ", ""));

    [Theory]
    [InlineData("../label-1.label.json")]
    [InlineData("label-1.frx")]
    public void Template_rejects_unsafe_or_unsupported_identifier(string id) =>
        Assert.Throws<ArgumentException>(() => new LabelTemplateDescriptor(id, "1", 1, 57, 40, 300, false));

    [Fact]
    public void Prepared_row_requires_all_contiguous_positions()
    {
        var context = new LabelRowContext(LabelOutputEnvironment.Preview, 2, "label-2.label.json", "", "");
        var label = Prepared(1);

        Assert.Throws<ArgumentException>(() =>
            new PreparedLabelRow(Guid.NewGuid().ToString("N"), context, DateTime.Today, [label]));
    }

    [Fact]
    public void Preview_document_has_defensive_byte_copies()
    {
        var source = new byte[] { 1, 2, 3 };
        var document = new LabelPreviewDocument(Guid.NewGuid().ToString("N"), 10, 10, 57, 40, source);
        source[0] = 9;
        var firstRead = document.GetPngBytes();
        firstRead[1] = 9;

        Assert.Equal(new byte[] { 1, 2, 3 }, document.GetPngBytes());
    }

    [Fact]
    public void Preview_document_rejects_payload_over_ten_mibibytes() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new LabelPreviewDocument(Guid.NewGuid().ToString("N"), 10, 10, 57, 40,
                new byte[LabelPreviewDocument.MaximumBytes + 1]));

    [Fact]
    public void Output_result_only_allows_document_for_preview_ready()
    {
        var document = new LabelPreviewDocument(Guid.NewGuid().ToString("N"), 10, 10, 57, 40, [1]);
        Assert.Throws<ArgumentException>(() => new LabelOutputResult(LabelOutputStatus.Printed, "ok", preview: document));
    }

    [Fact]
    public void Workflow_snapshot_enforces_completed_and_retry_invariants()
    {
        Assert.Throws<ArgumentException>(() => new LabelWorkflowSnapshot(1, 2, true, false, false));
        Assert.Throws<ArgumentException>(() => new LabelWorkflowSnapshot(1, 2, false, true, false));
        var snapshot = new LabelWorkflowSnapshot(2, 2, true, true, false);
        Assert.Equal(0, snapshot.Remaining);
    }

    private static PreparedLabel Prepared(int position)
    {
        var product = new Product(1, "12345678", "Product", "Piece", 10m, "1");
        var data = new LabelData(product, "$10.00", "Info");
        var barcode = BarcodeGenerationResult.Succeeded("12345678", BarcodeType.Code128, 1, 1, [1]);
        return new PreparedLabel(position, data, barcode);
    }
}
