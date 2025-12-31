import fs from 'node:fs';
import path from 'node:path';
import url from 'node:url';

import { TokenType } from '@maigolabs/needle/common';
import { buildInvertedIndex, createTokenizer } from '@maigolabs/needle/indexer';
import { loadInvertedIndex, inspectSearchResult, searchInvertedIndex } from '@maigolabs/needle/searcher';
import { TokenizerBuilder } from '@patdx/kuromoji';
import NodeDictionaryLoader from '@patdx/kuromoji/node';
import { Telegraf } from 'telegraf';

const botToken = process.env.TELEGRAM_BOT_TOKEN!;
const targetChatId = parseInt(process.env.TARGET_CHAT_ID!);
if (!botToken || isNaN(targetChatId)) throw new Error('Missing environment variables TELEGRAM_BOT_TOKEN or TARGET_CHAT_ID');

const bot = new Telegraf(botToken);

const escapeHtml = (s: string) => s.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');

const commands = await (async () => {
  const kuromojiDictPath = path.resolve(url.fileURLToPath(import.meta.resolve('@patdx/kuromoji')), '..', '..', 'dict');
  const kuromoji = await new TokenizerBuilder({ loader: new NodeDictionaryLoader({ dic_path: kuromojiDictPath }) }).build();

  const documents = (await fs.promises.readFile('../../example.txt', 'utf-8')).split('\n').filter(line => line.length > 0);
  const startBuildInvertedIndex = performance.now();
  const compressed = buildInvertedIndex(documents, { kuromoji });
  const endBuildInvertedIndex = performance.now();
  console.log(`Built inverted index in ${endBuildInvertedIndex - startBuildInvertedIndex}ms`);

  const startLoadInvertedIndex = performance.now();
  const invertedIndex = loadInvertedIndex(compressed);
  const endLoadInvertedIndex = performance.now();
  console.log(`Loaded inverted index in ${endLoadInvertedIndex - startLoadInvertedIndex}ms`);

  const codify = (text: string) => `<code>${escapeHtml(text)}</code>`;
  return {
    needle: (text: string) => {
      const startSearch = performance.now();
      const results = searchInvertedIndex(invertedIndex, text);
      const endSearch = performance.now();
      const searchDuration = (endSearch - startSearch).toFixed(3);
      const showingResults = results.slice(0, 5);
      return results.length === 0 ? codify(`No results found after ${searchDuration}ms`) : [
        codify(`Search completed in ${searchDuration}ms, showing ${showingResults.length}/${results.length} results:\n`),
        ...showingResults.map(result => inspectSearchResult(result, true)),
      ].join('\n').trimEnd();
    },
    tokenize: (text: string) => {
      const startTokenize = performance.now();
      const tokenizer = createTokenizer({ kuromoji });
      const tokens = tokenizer.tokenize(text);
      const tokenDefinitions = [...tokenizer.tokens.values()];
      const endTokenize = performance.now();
      const tokenizeDuration = (endTokenize - startTokenize).toFixed(3);
      return codify(tokens.length === 0 ? `No tokens emitted after ${tokenizeDuration}ms` : [
        `Tokenization completed in ${tokenizeDuration}ms, emitted ${tokens.length} tokens:`,
        ...tokens
          .map(token => [tokenDefinitions[token.id]!, token, [...text].slice(token.start, token.end).join('')] as const)
          .map(([token, { start, end }, originalPhrase]) => `  ${TokenType[token.type]}: ${JSON.stringify(token.text)} <- ${JSON.stringify(originalPhrase)} [${start}, ${end}]`),
      ].join('\n'));
    },
  };
})();

bot.on('message', async ctx => {
  const text = 'text' in ctx.message ? ctx.message.text :  undefined;
  console.log(`${ctx.chat.id ?? 'N/A'}:${ctx.from!.id} ${JSON.stringify(text)}`);
  if (ctx.chat.id === targetChatId) {
    if (text?.startsWith('/needle ')) {
      await ctx.reply(commands.needle(text.slice('/needle '.length)), { parse_mode: 'HTML' });
    } else if (text?.startsWith('/tokenize ')) {
      await ctx.reply(commands.tokenize(text.slice('/tokenize '.length)), { parse_mode: 'HTML' });
    }
  }
});

await bot.launch();
void bot.telegram.getMe().then(me => console.log(`Bot logged in as ${me.first_name} (@${me.username})`));
