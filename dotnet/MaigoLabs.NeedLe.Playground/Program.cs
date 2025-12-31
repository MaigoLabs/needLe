using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using MaigoLabs.NeedLe.Common.Extensions;
using MaigoLabs.NeedLe.Indexer;
using MaigoLabs.NeedLe.Searcher;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MaigoLabs.NeedLe.Playground;

public class Program
{
    private static LoadedInvertedIndex _invertedIndex = null!;
    private static long _targetChatId;

    public static async Task Main(string[] args)
    {
        var botToken = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN")
            ?? throw new InvalidOperationException("Missing environment variable TELEGRAM_BOT_TOKEN");
        var targetChatIdStr = Environment.GetEnvironmentVariable("TARGET_CHAT_ID")
            ?? throw new InvalidOperationException("Missing environment variable TARGET_CHAT_ID");
        _targetChatId = long.Parse(targetChatIdStr);

        // Build inverted index
        var exampleDocuments = File.ReadAllLines("../../example.txt").Where(line => line.Length > 0).ToArray();

        var startBuild = Stopwatch.GetTimestamp();
        var compressed = InvertedIndexBuilder.BuildInvertedIndex(exampleDocuments);
        var endBuild = Stopwatch.GetTimestamp();
        Console.WriteLine($"Built inverted index in {Stopwatch.GetElapsedTime(startBuild, endBuild).TotalMilliseconds}ms");

        var startLoad = Stopwatch.GetTimestamp();
        _invertedIndex = InvertedIndexLoader.Load(compressed);
        var endLoad = Stopwatch.GetTimestamp();
        Console.WriteLine($"Loaded inverted index in {Stopwatch.GetElapsedTime(startLoad, endLoad).TotalMilliseconds}ms");

        // Start bot
        var bot = new TelegramBotClient(botToken);
        var me = await bot.GetMe();
        Console.WriteLine($"Bot logged in as {me.FirstName} (@{me.Username})");

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        bot.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: new ReceiverOptions { AllowedUpdates = [UpdateType.Message] },
            cancellationToken: cts.Token
        );
        await Task.Delay(-1, cts.Token).ContinueWith(_ => { });
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Message is not { Text: { } text, Chat.Id: var chatId, From: { } from }) return;

        Console.WriteLine($"{chatId}:{from.Id} {JsonSerializer.Serialize(text, JsonSerializerOptions)}");

        if (chatId != _targetChatId) return;

        if (text.StartsWith("/needle "))
        {
            var query = text["/needle ".Length..];
            var response = HandleNeedleCommand(query);
            await bot.SendMessage(chatId, response, parseMode: ParseMode.Html, cancellationToken: ct);
        }
        else if (text.StartsWith("/tokenize "))
        {
            var query = text["/tokenize ".Length..];
            var response = HandleTokenizeCommand(query);
            await bot.SendMessage(chatId, response, parseMode: ParseMode.Html, cancellationToken: ct);
        }
    }

    private static Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, HandleErrorSource source, CancellationToken ct)
    {
        Console.WriteLine($"Error: {exception.Message}");
        return Task.CompletedTask;
    }

    private static string HandleNeedleCommand(string query)
    {
        var startSearch = Stopwatch.GetTimestamp();
        var results = InvertedIndexSearcher.Search(_invertedIndex, query);
        var endSearch = Stopwatch.GetTimestamp();
        var searchDuration = Stopwatch.GetElapsedTime(startSearch, endSearch).TotalMilliseconds.ToString("F3");

        if (results.Length == 0)
            return Codify($"No results found after {searchDuration}ms");

        var showingResults = results.Take(5).ToArray();
        return string.Join('\n',
        [
            Codify($"Search completed in {searchDuration}ms, showing {showingResults.Length}/{results.Length} results:\n"),
            .. showingResults.Select(result => InspectSearchResult(result, true))
        ]).TrimEnd();
    }

    private static string HandleTokenizeCommand(string query)
    {
        var tokenizer = new Tokenizer();
        var startTokenize = Stopwatch.GetTimestamp();
        var tokens = tokenizer.Tokenize(query);
        var tokenDefinitions = tokenizer.Tokens.Values.ToArray();
        var endTokenize = Stopwatch.GetTimestamp();
        var tokenizeDuration = Stopwatch.GetElapsedTime(startTokenize, endTokenize).TotalMilliseconds.ToString("F3");
        if (tokens.Count == 0) return Codify($"No tokens emitted after {tokenizeDuration}ms");

        var codePoints = query.ToCodePoints().ToArray();
        var lines = new List<string>
        {
            $"Tokenization completed in {tokenizeDuration}ms, emitted {tokens.Count} tokens:"
        };
        foreach (var token in tokens)
        {
            var tokenDef = tokenDefinitions[token.Id];
            var originalPhrase = codePoints.Skip(token.Start).Take(token.End - token.Start).ToUtf32String();
            lines.Add($"  {tokenDef.Type}: {JsonSerializer.Serialize(tokenDef.Text, JsonSerializerOptions)} <- {JsonSerializer.Serialize(originalPhrase, JsonSerializerOptions)} [{token.Start}, {token.End}]");
        }
        return Codify(string.Join('\n', lines));
    }

    private static string InspectSearchResult(SearchResult result, bool htmlHighlight)
    {
        var documentText = result.DocumentText;
        var documentCodePoints = result.DocumentCodePoints;
        var tokens = result.Tokens;
        var rangeCount = result.RangeCount;
        var matchRatio = result.MatchRatio;
        var matchRatioLevel = result.MatchRatioLevel;

        var resultText = htmlHighlight
            ? string.Join("", SearchResultHighlighter.Highlight(result).Select(part => !part.IsHighlighted ? EscapeHtml(part.Text) : $"<u><b>{EscapeHtml(part.Text)}</b></u>"))
            : documentText;
        var description = $" ({rangeCount} ranges, {Math.Round(matchRatio * 10000) / 10000} => L{matchRatioLevel})";
        return string.Join('\n',
        [
            resultText + (htmlHighlight ? $"<code>{description}</code>" : description),
            .. tokens.Select(token =>
            {
                var escapedTokenText = JsonSerializer.Serialize(token.Definition.Text, JsonSerializerOptions);
                var escapedDocumentText = JsonSerializer.Serialize(documentCodePoints.Skip(token.DocumentOffset.Start).Take(token.DocumentOffset.Length).ToUtf32String(), JsonSerializerOptions);
                if (htmlHighlight)
                {
                    escapedTokenText = EscapeHtml(escapedTokenText);
                    escapedDocumentText = EscapeHtml(escapedDocumentText);
                }
                var line = $"    {token.Definition.Type}: {escapedTokenText} -> {escapedDocumentText}" + (token.IsTokenPrefixMatching ? " (prefix match)" : "");
                return htmlHighlight ? $"<code>{line}</code>" : line;
            }),
            "",
        ]);
    }

    private static string Codify(string text) => $"<code>{EscapeHtml(text)}</code>";
    private static JsonSerializerOptions JsonSerializerOptions => new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private static string EscapeHtml(string text) => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
