namespace MaigoLabs.NeedLe.Common.Types;

#pragma warning disable IDE1006 // Naming rule violation

// For compatibility with TypeScript, we use camelCase property names here.

public class CompressedInvertedIndex
{
    public required string[]? documents { get; set; }
    public required int[] tokenTypes { get; set; } // Use int values here instead of TokenType enum to avoid JSON serialization issues.
    public required List<int[]>[] tokenReferences { get; set; } // tokenId -> [documentId, start1, end1, start2, end2, ...]
    public required CompressedInvertedIndexTries tries { get; set; }
}

public class CompressedInvertedIndexTries
{
    public required int[] romaji { get; set; }
    public required int[] kana { get; set; }
    public required int[] other { get; set; }
}
