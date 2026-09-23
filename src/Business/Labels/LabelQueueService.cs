using SysTools.Entities.Labels;

namespace SysTools.Business.Labels;

public sealed class LabelQueueService : ILabelQueueService
{
    private readonly object _syncRoot = new();
    private readonly List<PendingLabel> _pending = [];
    private int? _activeCapacity;

    public LabelQueueResult Capture(LabelData data, int columns)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (columns is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(columns));
        }

        lock (_syncRoot)
        {
            if (_pending.Count > 0 && _activeCapacity != columns)
            {
                throw new InvalidOperationException(
                    "Complete o cancele la fila pendiente antes de cambiar las columnas.");
            }

            if (columns == 1)
            {
                return new LabelQueueResult(
                    columns,
                    Array.Empty<PendingLabel>(),
                    new[] { data });
            }

            _activeCapacity ??= columns;
            _pending.Add(new PendingLabel(_pending.Count + 1, data));
            if (_pending.Count < columns)
            {
                return new LabelQueueResult(
                    columns,
                    _pending.ToArray(),
                    Array.Empty<LabelData>());
            }

            var completed = _pending.Select(item => item.Data).ToArray();
            _pending.Clear();
            _activeCapacity = null;
            return new LabelQueueResult(
                columns,
                Array.Empty<PendingLabel>(),
                completed);
        }
    }

    public int Cancel()
    {
        lock (_syncRoot)
        {
            var discarded = _pending.Count;
            _pending.Clear();
            _activeCapacity = null;
            return discarded;
        }
    }

    public IReadOnlyList<PendingLabel> GetPending()
    {
        lock (_syncRoot)
        {
            return Array.AsReadOnly(_pending.ToArray());
        }
    }
}
