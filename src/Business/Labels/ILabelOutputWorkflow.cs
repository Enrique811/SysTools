using SysTools.Entities.Labels;
using SysTools.Entities.Products;

namespace SysTools.Business.Labels;

public interface ILabelOutputWorkflow
{
    Task<LabelOutputResult> CaptureAsync(Product product, string formattedPrice, CancellationToken cancellationToken = default);
    Task<LabelOutputResult> RetryAsync(CancellationToken cancellationToken = default);
    LabelCancelResult Cancel();
    LabelWorkflowSnapshot GetState();
    void Invalidate();
}
