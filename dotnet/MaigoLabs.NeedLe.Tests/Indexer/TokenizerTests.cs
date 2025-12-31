using MaigoLabs.NeedLe.Common.Types;
using MaigoLabs.NeedLe.Indexer;

namespace MaigoLabs.NeedLe.Tests.Indexer;

public sealed class Tokenizer_TokenizesMixedJapaneseTextTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        var tokenizer = new Tokenizer(TokenizerOptions);
        var tokens = tokenizer.Tokenize("僕の和風本当上手");

        var tokenDefs = tokenizer.Tokens.Values.ToList();

        // Should have tokens of various types
        var types = tokenDefs.Select(t => t.Type).ToHashSet();
        Assert.Contains(TokenType.Han, types);
        Assert.Contains(TokenType.Pinyin, types);
        Assert.Contains(TokenType.Kana, types);
        Assert.Contains(TokenType.Romaji, types);

        // Helper to get token texts at a specific position by type
        List<string> GetTokenTextsAt(int pos, TokenType type) => tokens
            .Where(t => t.Start <= pos && t.End > pos)
            .Select(t => tokenDefs.First(d => d.Id == t.Id))
            .Where(d => d.Type == type)
            .Select(d => d.Text)
            .ToList();

        // Position 0: 僕
        Assert.Contains("僕", GetTokenTextsAt(0, TokenType.Han));
        Assert.Contains("pu", GetTokenTextsAt(0, TokenType.Pinyin));
        Assert.Contains("ボク", GetTokenTextsAt(0, TokenType.Kana));
        Assert.Contains("boku", GetTokenTextsAt(0, TokenType.Romaji));

        // Position 1: の (hiragana, no Han/Pinyin)
        Assert.Empty(GetTokenTextsAt(1, TokenType.Han));
        Assert.Empty(GetTokenTextsAt(1, TokenType.Pinyin));
        Assert.Contains("ノ", GetTokenTextsAt(1, TokenType.Kana));
        Assert.Contains("no", GetTokenTextsAt(1, TokenType.Romaji));

        // Position 2: 和
        Assert.Contains("和", GetTokenTextsAt(2, TokenType.Han));
        Assert.Contains("he", GetTokenTextsAt(2, TokenType.Pinyin));
        Assert.Contains("ワ", GetTokenTextsAt(2, TokenType.Kana));
        Assert.Contains("wa", GetTokenTextsAt(2, TokenType.Romaji));

        // Position 3: 風
        Assert.Contains("風", GetTokenTextsAt(3, TokenType.Han));
        Assert.Contains("风", GetTokenTextsAt(3, TokenType.Han)); // simplified variant
        Assert.Contains("feng", GetTokenTextsAt(3, TokenType.Pinyin));
        Assert.Contains("フウ", GetTokenTextsAt(3, TokenType.Kana));
        Assert.Contains("fu", GetTokenTextsAt(3, TokenType.Romaji));

        // Position 4: 本
        Assert.Contains("本", GetTokenTextsAt(4, TokenType.Han));
        Assert.Contains("ben", GetTokenTextsAt(4, TokenType.Pinyin));
        Assert.Contains("ホン", GetTokenTextsAt(4, TokenType.Kana));
        Assert.Contains("hon", GetTokenTextsAt(4, TokenType.Romaji));

        // Position 5: 当
        Assert.Contains("当", GetTokenTextsAt(5, TokenType.Han));
        Assert.Contains("當", GetTokenTextsAt(5, TokenType.Han)); // traditional variant
        Assert.Contains("dang", GetTokenTextsAt(5, TokenType.Pinyin));
        Assert.Contains("トウ", GetTokenTextsAt(5, TokenType.Kana));
        Assert.Contains("to", GetTokenTextsAt(5, TokenType.Romaji)); // normalized: tou -> to

        // Position 6: 上
        Assert.Contains("上", GetTokenTextsAt(6, TokenType.Han));
        Assert.Contains("shang", GetTokenTextsAt(6, TokenType.Pinyin));
        Assert.Contains("ジョウ", GetTokenTextsAt(6, TokenType.Kana));
        Assert.Contains("jo", GetTokenTextsAt(6, TokenType.Romaji)); // normalized: jou -> jo

        // Position 7: 手
        Assert.Contains("手", GetTokenTextsAt(7, TokenType.Han));
        Assert.Contains("shou", GetTokenTextsAt(7, TokenType.Pinyin));
        Assert.Contains("シュ", GetTokenTextsAt(7, TokenType.Kana));
        Assert.Contains("shu", GetTokenTextsAt(7, TokenType.Romaji));
    }
}

public sealed class Tokenizer_NoDuplicateTokensTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        var tokenizer = new Tokenizer(TokenizerOptions);

        // Tokenize multiple music names that share some characters
        tokenizer.Tokenize("僕の和風本当上手");
        tokenizer.Tokenize("僕");
        tokenizer.Tokenize("和風");

        // Check that there are no duplicate tokens
        var tokenDefs = tokenizer.Tokens.Values.ToList();
        var tokenKeys = tokenDefs.Select(t => $"{t.Type}:{t.Text}").ToList();
        var uniqueKeys = tokenKeys.ToHashSet();

        Assert.Equal(uniqueKeys.Count, tokenKeys.Count);

        // Also check that IDs are unique
        var ids = tokenDefs.Select(t => t.Id).ToList();
        var uniqueIds = ids.ToHashSet();
        Assert.Equal(uniqueIds.Count, ids.Count);
    }
}

public sealed class Tokenizer_HandlesRawTokensForNonCjkTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        var tokenizer = new Tokenizer(TokenizerOptions);
        tokenizer.Tokenize("a-b");

        var tokenDefs = tokenizer.Tokens.Values.ToList();
        var rawTokenTexts = tokenDefs.Where(t => t.Type == TokenType.Raw).Select(t => t.Text).ToList();

        Assert.Contains("a", rawTokenTexts);
        Assert.Contains("-", rawTokenTexts);
        Assert.Contains("b", rawTokenTexts);
    }
}

public sealed class Tokenizer_TokenizesCompoundWordKyouTest : NeedleTestBase
{
    [Fact]
    public void Execute()
    {
        var tokenizer = new Tokenizer(TokenizerOptions);
        var tokens = tokenizer.Tokenize("今日");
        var tokenDefs = tokenizer.Tokens.Values.ToList();

        // Helper to get tokens with specific type and span
        List<string> GetTokensWithSpan(TokenType type, int start, int end) => tokens
            .Where(t => t.Start == start && t.End == end)
            .Select(t => tokenDefs.First(d => d.Id == t.Id))
            .Where(d => d.Type == type)
            .Select(d => d.Text)
            .ToList();

        // Individual character readings at position 0: 今
        Assert.Contains("今", GetTokensWithSpan(TokenType.Han, 0, 1));
        Assert.Contains("jin", GetTokensWithSpan(TokenType.Pinyin, 0, 1));
        Assert.Contains("コン", GetTokensWithSpan(TokenType.Kana, 0, 1));
        Assert.Contains("イマ", GetTokensWithSpan(TokenType.Kana, 0, 1));
        Assert.Contains("kon", GetTokensWithSpan(TokenType.Romaji, 0, 1));
        Assert.Contains("ima", GetTokensWithSpan(TokenType.Romaji, 0, 1));

        // Individual character readings at position 1: 日
        Assert.Contains("日", GetTokensWithSpan(TokenType.Han, 1, 2));
        Assert.Contains("ri", GetTokensWithSpan(TokenType.Pinyin, 1, 2));
        Assert.Contains("ニチ", GetTokensWithSpan(TokenType.Kana, 1, 2));
        Assert.Contains("ヒ", GetTokensWithSpan(TokenType.Kana, 1, 2));
        Assert.Contains("niti", GetTokensWithSpan(TokenType.Romaji, 1, 2));
        Assert.Contains("hi", GetTokensWithSpan(TokenType.Romaji, 1, 2));

        // Combined reading for "今日" [0, 2] - this is an indivisible compound word
        Assert.Contains("キョウ", GetTokensWithSpan(TokenType.Kana, 0, 2));
        Assert.Contains("kyo", GetTokensWithSpan(TokenType.Romaji, 0, 2)); // normalized: kyou -> kyo
    }
}


