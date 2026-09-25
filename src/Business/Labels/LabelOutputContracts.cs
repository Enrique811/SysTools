using SysTools.Entities.Labels;

namespace SysTools.Business.Labels;

public interface ILabelTemplateProvider
{
    Task<LabelTemplateResult> ResolveAsync(string templateId, int capacity, CancellationToken cancellationToken = default);
}
public interface ILabelDocumentRenderer
{
    Task<LabelRenderResult> RenderAsync(PreparedLabelRow row, LabelTemplateDescriptor template, CancellationToken cancellationToken = default);
}
public interface ILabelPrinter
{
    Task<LabelPrintResult> PrintAsync(LabelPreviewDocument document, string exactPrinterId, string operationId, CancellationToken cancellationToken = default);
}
