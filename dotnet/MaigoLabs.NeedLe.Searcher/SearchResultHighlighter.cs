using MaigoLabs.NeedLe.Common.Extensions;
using MaigoLabs.NeedLe.Common.Types;

namespace MaigoLabs.NeedLe.Searcher;

public class HighlightedTextPart
{
    public required string Text { get; init; }
    public required bool IsHighlighted { get; init; }
}

public static class SearchResultHighlighter
{
    public static List<HighlightedTextPart> Highlight(SearchResult resultDocument)
    {
        var result = new List<HighlightedTextPart>();
        var previousHighlightEnd = 0;
        foreach (var token in resultDocument.Tokens)
        {
            var notHighlightedText = resultDocument.DocumentCodePoints.Skip(previousHighlightEnd).Take(token.DocumentOffset.Start - previousHighlightEnd).ToUtf32String();
            if (notHighlightedText.Length > 0) result.Add(new HighlightedTextPart { Text = notHighlightedText, IsHighlighted = false });
            var highlightEnd = token.IsTokenPrefixMatching && token.Definition.Type == TokenType.Kana
                ? token.DocumentOffset.Start + Math.Max(
                    1,
                    (int)Math.Round(
                        token.DocumentOffset.Length *
                        Math.Min(1, (double)token.InputOffset.Length / token.Definition.CodePointLength)
                    )
                )
                : token.DocumentOffset.End;
            result.Add(new HighlightedTextPart { Text = resultDocument.DocumentCodePoints.Skip(token.DocumentOffset.Start).Take(highlightEnd - token.DocumentOffset.Start).ToUtf32String(), IsHighlighted = true });
            previousHighlightEnd = highlightEnd;
        }
        if (previousHighlightEnd < resultDocument.DocumentCodePoints.Length) result.Add(new HighlightedTextPart { Text = resultDocument.DocumentCodePoints.Skip(previousHighlightEnd).ToUtf32String(), IsHighlighted = false });
        return result;
    }
}
