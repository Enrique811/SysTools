using SysTools.Entities.Labels;

namespace SysTools.Presentation.Modules.Labels;

public interface ILabelPreviewDialogService
{
    Task ShowAsync(LabelPreviewDocument document, CancellationToken cancellationToken = default);
    void CloseActive();
}
