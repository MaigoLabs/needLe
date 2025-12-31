export interface TrieNode {
  parent: TrieNode | undefined;
  children: Map<number, TrieNode>; // Unicode code point -> child node
  tokenIds: number[];
  subTreeTokenIds: number[]; // Empty on root. Will Uint16Array be faster?
}

export const traverseTrieStep = (node: TrieNode | undefined, codePoint: string, ignorableCodePoints?: RegExp) =>
  node?.children.get(codePoint.codePointAt(0)!) ?? (ignorableCodePoints?.test(codePoint) ? node : undefined);
export const traverseTrie = (node: TrieNode | undefined, text: string, ignorableCodePoints?: RegExp) => {
  if (!node) return;
  for (const codePoint of text) {
    node = traverseTrieStep(node, codePoint, ignorableCodePoints);
    if (!node) return;
  }
  return node;
};
