using SysTools.Entities.Labels;

namespace SysTools.Business.Labels;

public interface ILabelQueueService
{
    LabelQueueResult Capture(LabelData data, int columns);

    int Cancel();

    IReadOnlyList<PendingLabel> GetPending();
}
