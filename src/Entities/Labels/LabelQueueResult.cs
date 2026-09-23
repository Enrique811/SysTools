namespace SysTools.Entities.Labels;

public sealed class LabelQueueResult
{
    public LabelQueueResult(
        int capacity,
        IEnumerable<PendingLabel> pending,
        IEnumerable<LabelData> completed)
    {
        if (capacity is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        ArgumentNullException.ThrowIfNull(pending);
        ArgumentNullException.ThrowIfNull(completed);
        var pendingArray = pending.ToArray();
        var completedArray = completed.ToArray();

        if (pendingArray.Any(item => item is null))
        {
            throw new ArgumentException("Las etiquetas pendientes no admiten valores nulos.", nameof(pending));
        }

        if (completedArray.Any(item => item is null))
        {
            throw new ArgumentException("La fila completa no admite valores nulos.", nameof(completed));
        }

        if (pendingArray.Length > 0 && completedArray.Length > 0)
        {
            throw new ArgumentException("Un resultado no puede estar pendiente y completo al mismo tiempo.");
        }

        if (pendingArray.Length >= capacity)
        {
            throw new ArgumentException("Una fila llena debe entregarse como completada.", nameof(pending));
        }

        for (var index = 0; index < pendingArray.Length; index++)
        {
            if (pendingArray[index].Position != index + 1)
            {
                throw new ArgumentException("Las posiciones pendientes deben ser contiguas y comenzar en uno.", nameof(pending));
            }
        }

        if (completedArray.Length != 0 && completedArray.Length != capacity)
        {
            throw new ArgumentException("La fila completa debe alcanzar exactamente la capacidad.", nameof(completed));
        }

        Capacity = capacity;
        Pending = Array.AsReadOnly(pendingArray);
        Completed = Array.AsReadOnly(completedArray);
    }

    public int Capacity { get; }

    public IReadOnlyList<PendingLabel> Pending { get; }

    public IReadOnlyList<LabelData> Completed { get; }

    public bool IsComplete => Completed.Count == Capacity;

    public int Remaining => IsComplete ? 0 : Capacity - Pending.Count;

    public override string ToString() =>
        $"{nameof(LabelQueueResult)} {{ Capacity = {Capacity}, Pending = {Pending.Count}, IsComplete = {IsComplete} }}";
}
