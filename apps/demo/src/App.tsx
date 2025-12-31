import { TokenType } from '@maigolabs/needle/common';
import {
  searchInvertedIndex,
  highlightSearchResult,
  type SearchResult,
} from '@maigolabs/needle/searcher';
import { useState, type FunctionComponent } from 'react';

type Tab = 'search' | 'tokenize';

type AppData = typeof import('./data');
export const Layout: FunctionComponent<{ dataPromise: Promise<AppData> }> = ({ dataPromise }) => {
  const [appData, setAppData] = useState<AppData | null>(null);
  const [error, setError] = useState<string | null>(null);
  void dataPromise.then(props => setAppData(props)).catch(error => setError((error instanceof Error ? error.stack : undefined) ?? String(error)));
  return (
    <div className="min-h-screen bg-[#f9f2e0] text-[#8b7355] font-mono selection:bg-[#d4c4b0]/70">
      <div className="max-w-200 mx-auto px-4 pt-8 pb-6">
        <header className="mb-8">
          <h1 className="pb-3 text-2xl text-[#a08060]">MaigoLabs :: needLe</h1>
          <div className="pb-4 text-sm">
            <p>Fuzzy search engine for small text pieces, with Chinese/Japanese pronunciation support</p>
            <p>(Available in TypeScript and C#)</p>
          </div>
          <div className="flex gap-4 text-sm">
            <a href="https://github.com/MaigoLabs/needLe" target="_blank" rel="noopener" className="text-[#b8a890] hover:text-[#8b7355]">[GitHub]</a>
            <a href="https://www.npmjs.com/package/@maigolabs/needle" target="_blank" rel="noopener" className="text-[#b8a890] hover:text-[#8b7355]">[NPM]</a>
            <a href="https://www.nuget.org/packages/MaigoLabs.NeedLe" target="_blank" rel="noopener" className="text-[#b8a890] hover:text-[#8b7355]">[NuGet]</a>
          </div>
        </header>

        {
          appData
            ? <App appData={appData} />
            : error
              ? <div className="text-sm bg-[#efe5d0] px-4 py-3 rounded-lg whitespace-pre-wrap">{error}</div>
              : <div>
                  <div className="flex flex-row items-center gap-2"><div className="i-svg-spinners:ring-resize" /> Loading...</div>
                  <div className="mt-6 text-sm bg-[#efe5d0] px-4 py-3 rounded-lg">
                    <div className="font-bold mb-2">Tips:</div>
                    <div>This demo loads Kuromoji/OpenCC/pinyin-pro for tokenization and index building.</div>
                    <div>However, searching on a prebuilt index doesn't require loading any external library/dictionary.</div>
                  </div>
                </div>
        }
      </div>
    </div>
  );
};

interface AppProps {
  appData: AppData;
}

export const App: FunctionComponent<AppProps> = ({ appData: { kuromoji, createTokenizer, invertedIndex } }) => {
  const [input, setInput] = useState('');
  const [tab, setTab] = useState<Tab>('search');

  const searchResults = tab === 'search' && input.trim()
    ? searchInvertedIndex(invertedIndex, input).slice(0, 50)
    : [];

  const tokenizeResults = tab === 'tokenize' && input.trim()
    ? (() => {
        const tokenizer = createTokenizer({ kuromoji });
        const tokens = tokenizer.tokenize(input);
        const tokenDefs = tokenizer.tokens;
        const codePoints = [...input];
        return tokens.map(t => {
          const def = [...tokenDefs.values()].find(d => d.id === t.id)!;
          const original = codePoints.slice(t.start, t.end).join('');
          return { ...t, type: def.type, text: def.text, original };
        });
      })()
    : [];

  return (
    <>
      <input
        type="text"
        value={input}
        onChange={e => setInput(e.target.value)}
        placeholder={`Type something to ${tab}...`}
        className="w-full bg-[#efe5d0] text-[#6b5a48] px-3 py-2 mb-2 outline-none placeholder-[#b8a890] rounded-lg"
      />

      <div className="flex gap-4 mb-6 text-sm">
        <button
          onClick={() => setTab('search')}
          className={`bg-transparent border-none cursor-pointer ${tab === 'search' ? 'text-[#6b5a48]' : 'text-[#c0b0a0]'}`}
        >
          Search
        </button>
        <button
          onClick={() => setTab('tokenize')}
          className={`bg-transparent border-none cursor-pointer ${tab === 'tokenize' ? 'text-[#6b5a48]' : 'text-[#c0b0a0]'}`}
        >
          Tokenize
        </button>
      </div>

      <div className="space-y-2">
        {tab === 'search' && searchResults.map((result, i) => (
          <SearchResultItem key={i} result={result} input={input} />
        ))}

        {tab === 'tokenize' && tokenizeResults.length > 0 && (
          <div className="grid grid-cols-[repeat(auto-fill,minmax(280px,1fr))] gap-1">
            {tokenizeResults.map((token, i) => (
              <div key={i} className="bg-[#efe5d0] px-3 py-2 text-sm truncate rounded-lg">
                <span className="text-[#a08060]">{TokenType[token.type]}: </span>
                <span className="text-[#6b5a48]">{JSON.stringify(token.text)}</span>
                <span className="text-[#c0b0a0]">{' <- '}</span>
                <span className="text-[#8b7355]">{JSON.stringify(token.original)}</span>
                <span className="text-[#c8bba8]">{` [${token.start}, ${token.end}]`}</span>
              </div>
            ))}
          </div>
        )}

        {input.trim() && tab === 'search' && searchResults.length === 0 && (
          <div className="text-[#b8a890] text-sm">No results.</div>
        )}
      </div>
    </>
  );
};

const SearchResultItem: FunctionComponent<{ result: SearchResult; input: string }> = ({ result, input }) => {
  const highlighted = highlightSearchResult(result);
  const inputCodePoints = [...input];

  const stats = [
    `${result.rangeCount} range(s)`,
    `${Math.round(result.matchRatio * 100)}%`,
    result.prefixMatchCount > 0 ? `${result.prefixMatchCount} prefix` : null,
  ].filter(Boolean).join(', ');

  return (
    <div className="bg-[#efe5d0] px-3 py-2 text-sm rounded-lg">
      <div className="flex gap-2">
        <div className="flex-1 truncate">
          {highlighted.map((part, i) =>
            typeof part === 'string'
              ? <span key={i} className="text-[#b8a890]">{part}</span>
              : <span key={i} className="text-[#5a4a38]">{part.highlight}</span>)}
        </div>
        <div className="text-[#c8bba8] shrink-0">{stats}</div>
      </div>

      <div className="grid grid-cols-[repeat(auto-fill,minmax(200px,1fr))] gap-x-2 mt-1">
        {result.tokens.map((token, i) => {
          const inputText = inputCodePoints.slice(token.inputOffset.start, token.inputOffset.end).join('');
          const docText = result.documentCodePoints.slice(token.documentOffset.start, token.documentOffset.end).join('');
          return (
            <div key={i} className="text-[11px] truncate">
              <span className="text-[#b8a890]">{TokenType[token.definition.type]}: </span>
              <span className="text-[#8b7355]">{JSON.stringify(inputText)}</span>
              <span className="text-[#c8bba8]">{' -> '}</span>
              <span className="text-[#6b5a48]">{JSON.stringify(docText)}</span>
              {token.isTokenPrefixMatching && <span className="text-[#b8a890]">{' (prefix)'}</span>}
            </div>
          );
        })}
      </div>
    </div>
  );
};
