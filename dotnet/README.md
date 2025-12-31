# `MaigoLabs.NeedLe`

Fuzzy search engine for small text pieces, with Chinese/Japanese pronunciation support.

See also [in-browser demo](https://needle.maigo.dev) (TypeScript version, but the same features as in C#).

## Install

```bash
dotnet add package MaigoLabs.NeedLe
```

Or install sub-packages separately:

```bash
dotnet add package MaigoLabs.NeedLe.Indexer  # For building indexes
dotnet add package MaigoLabs.NeedLe.Searcher # For searching only
```

## Usage

### Indexing

Indexing requires dictionaries. These are installed as dependencies of the `MaigoLabs.NeedLe.Indexer` package:

* MeCab.DotNet
* OpenccNetLib
* hyjiacan.pinyin4net

```csharp
using MaigoLabs.NeedLe.Indexer;

var documents = new[] { "你好世界", "こんにちは" };
var compressedIndex = InvertedIndexBuilder.BuildInvertedIndex(documents);
// To customize dictionary paths, pass the second argument `TokenizerOptions` to `BuildInvertedIndex`.

// The built index could be stored for later use, or sent to frontend to load with TypeScript package `@maigolabs/needle`.
// For compatibility with .NET Standard, we don't provide JSON related methods. You can use any JSON library to serialize/deserialize the index in the way you prefer.
var json = JsonSerializer.Serialize(compressedIndex);
```

### Searching

Searching requires a prebuilt index but doesn't require dictionaries. Searcher is a lightweight package without dependencies.

```csharp
using MaigoLabs.NeedLe.Searcher;

// Index returned by `BuildInvertedIndex`.
var index = InvertedIndexLoader.Load(compressedIndex);

var results = InvertedIndexSearcher.Search(index, "sekai");
foreach (var result in results) Console.WriteLine($"{result.DocumentText} ({result.MatchRatio:P0})")
// → 你好世界 (50%)
```

To highlight the search result, see also `SearchResultHighlighter`.
