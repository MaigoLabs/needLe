# `@maigolabs/needle`

Fuzzy search engine for small text pieces, with Chinese/Japanese pronunciation support.

See also [in-browser demo](https://needle.maigo.dev).

## Install

Dictionaries are installed as dependencies of the package, but if you don't use the indexer, they could be tree-shaken when bundling.

```bash
pnpm install @maigolabs/needle
```

## Usage

### Indexing

NeedLe uses Kuromoji for Japanese tokenization, which loads dictionaries dynamically. You need to create a Kuromoji `TokenizerBuilder` first:

```ts
// In Node.js you can just load the dictionary from the file system.

import { TokenizerBuilder } from '@patdx/kuromoji';
import NodeDictionaryLoader from '@patdx/kuromoji/node';

const kuromojiDictPath = path.resolve(url.fileURLToPath(import.meta.resolve('@patdx/kuromoji')), '..', '..', 'dict');
const kuromoji = await new TokenizerBuilder({ loader: new NodeDictionaryLoader({ dic_path: kuromojiDictPath }) }).build();

// In browser you need to provide a custom loader to load the dictionary files with fetch().

import { TokenizerBuilder } from '@patdx/kuromoji';

// You can load dict files from CDN (See also the README of https://github.com/patdx/kuromoji.js)
const kuromoji = await new TokenizerBuilder({
  loader: {
    loadArrayBuffer: async (url: string) => {
      url = `https://cdn.jsdelivr.net/npm/@aiktb/kuromoji@1.0.2/dict/${url.replace('.gz', '')}`;
      const res = await fetch(url);
      if (!res.ok) throw new Error(`Failed to fetch ${url}`);
      return await res.arrayBuffer();
    },
  },
}).build();
```

After creating the Kuromoji instance, you can build the inverted index:

```ts
import { buildInvertedIndex } from '@maigolabs/needle/indexer';

const documents = ['你好世界', 'こんにちは'];
const compressedIndex = buildInvertedIndex(documents, { kuromoji });

// The built index could be stored for later use.
const json = JSON.stringify(compressedIndex);
```

### Searching

If you only import the searcher in your frontend code, indexer and dictionary-related dependencies will be tree-shaken.

```ts
import { loadInvertedIndex, searchInvertedIndex } from '@maigolabs/needle/searcher';

const loadedIndex = loadInvertedIndex(compressedIndex);
const results = searchInvertedIndex(loadedIndex, 'sekai');
for (const result of results) console.log(`${result.documentText} (${(result.matchRatio * 100).toFixed(0)}%)`);
// → 你好世界 (50%)
```

To highlight the search result, see also `highlightSearchResult`.
