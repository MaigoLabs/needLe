namespace MaigoLabs.NeedLe.Common.Types;

public class OffsetSpan
{
    public required int Start { get; init; }
    public required int End { get; init; }

    public int Length => End - Start;
}
