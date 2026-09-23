namespace SysTools.Entities.Labels;

public sealed class PendingLabel
{
    public PendingLabel(int position, LabelData data)
    {
        if (position is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        ArgumentNullException.ThrowIfNull(data);
        Position = position;
        Data = data;
    }

    public int Position { get; }

    public LabelData Data { get; }

    public override string ToString() => $"{nameof(PendingLabel)} {{ Position = {Position} }}";
}
