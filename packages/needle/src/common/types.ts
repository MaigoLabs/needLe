export enum TokenType {
  Raw,
  Kana,
  Romaji,
  Han,
  Pinyin,
}

export interface TokenDefinition {
  id: number;
  type: TokenType;
  text: string;
  codePointLength: number;
}

// [start, end)
export interface OffsetSpan {
  start: number;
  end: number;
}

export type CompressedInvertedIndex = {
  documents?: string[];
  tokenTypes: TokenType[];
  tokenReferences: number[][][]; // tokenId -> [documentId, start1, end1, start2, end2, ...][]
  tries: {
    romaji: number[];
    kana: number[];
    other: number[];
  };
};
