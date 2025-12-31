using MaigoLabs.NeedLe.Common;
using MaigoLabs.NeedLe.Common.Extensions;
using MaigoLabs.NeedLe.Common.Types;
using MaigoLabs.NeedLe.Indexer.Han;
using MaigoLabs.NeedLe.Indexer.Japanese;

namespace MaigoLabs.NeedLe.Indexer;

public class TokenizerOptions
{
    public HanVariantProvider? HanVariantProvider { get; set; }
    public TranscriptionProvider? TranscriptionProvider { get; set; }
}

public class Tokenizer(TokenizerOptions? options = null)
{
    public HanVariantProvider HanVariantProvider { get; set; } = options?.HanVariantProvider ?? new HanVariantProvider();
    public TranscriptionProvider TranscriptionProvider { get; set; } = options?.TranscriptionProvider ?? new TranscriptionProvider();

    public class Token
    {
        public required int Id { get; set; }
        public required int Start { get; set; }
        public required int End { get; set; }
    }

    public Dictionary<(TokenType Type, string Text), TokenDefinition> Tokens { get; } = [];
    private TokenDefinition EnsureToken(TokenType type, string text)
    {
        var key = (type, text);
        if (Tokens.TryGetValue(key, out var tokenDefinition)) return tokenDefinition;
        tokenDefinition = new TokenDefinition { Id = Tokens.Count, Type = type, Text = text, CodePointLength = text.ToCodePoints().Count() };
        Tokens.Add(key, tokenDefinition);
        return tokenDefinition;
    }

    public List<Token> Tokenize(string text)
    {
        var codePoints = text.ToCodePoints().Select(CommonNormalization.NormalizeCodePoint).ToArray();
        var results = new List<Token>();
        Action<TokenType /* tokenType */, string /* text */> Emitter(int start, int end) =>
            (tokenType, codePoints) => results.Add(new Token { Id = EnsureToken(tokenType, codePoints).Id, Start = start, End = end });

        void EmitMaybeJapanese(ReadOnlyMemory<int> codePoints, int offset)
        {
            foreach (var combination in TranscriptionProvider.EnumerateKanaTranscriptions(codePoints))
            {
                var emit = Emitter(offset + combination.Start, offset + combination.Start + combination.Length);
                foreach (var transcription in combination.Transcriptions) emit(TokenType.Kana, transcription);
            }
            foreach (var combination in TranscriptionProvider.EnumerateRomajiTranscriptions(codePoints))
            {
                var emit = Emitter(offset + combination.Start, offset + combination.Start + combination.Length);
                foreach (var transcription in combination.Transcriptions) emit(TokenType.Romaji, transcription);
            }
            for (int i = 0; i < codePoints.Length; i++)
            {
                // Single character may have not only kana readings, but also Chinese pronunciations or Simplified/Traditional/Japanese variants.
                var hanAlternates = HanVariantProvider.GetHanVariants(codePoints.Span[i]); // All possible variant characters (Simplified/Traditional/Japanese)
                var pinyinAlternates = hanAlternates.SelectMany(PinyinHelper.GetPinyinCandidates).Distinct();
                var emit = Emitter(offset + i, offset + i + 1);
                foreach (var han in hanAlternates) emit(TokenType.Han, char.ConvertFromUtf32(han));
                foreach (var pinyin in pinyinAlternates) emit(TokenType.Pinyin, pinyin);
            }
        }

        var consequentCharsets = new (Func<int, bool> Is, Action<ReadOnlyMemory<int>, int> Emit)[]
        {
            (Is: JapaneseUtils.IsMaybeJapanese, Emit: EmitMaybeJapanese),
        };

        void EmitRaw(int codePoint, int offset) => Emitter(offset, offset + 1)(TokenType.Raw, char.ConvertFromUtf32(codePoint));

        for (int start = 0; start < codePoints.Length; )
        {
            var codePoint = codePoints[start];
            var emitted = false;
            foreach (var (Is, Emit) in consequentCharsets)
            {
                var length = 0;
                while (start + length < codePoints.Length && Is(codePoints[start + length])) length++;
                if (length > 0)
                {
                    Emit(new Memory<int>(codePoints, start, length), start);
                    start += length;
                    emitted = true;
                    break;
                }
            }
            if (emitted) continue;

            // Skip whitespaces
            if (CommonUtils.IsWhitespace(codePoint))
            {
                start++;
                continue;
            }

            EmitRaw(codePoint, start);
            start++;
        }
        return results;
    }
}
