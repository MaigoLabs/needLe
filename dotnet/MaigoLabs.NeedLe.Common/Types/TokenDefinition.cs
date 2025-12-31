namespace MaigoLabs.NeedLe.Common.Types;

public class TokenDefinition
{
    public required int Id { get; set; }
    public required TokenType Type { get; set; }
    public required string Text { get; set; }
    public required int CodePointLength { get; set; }
}
