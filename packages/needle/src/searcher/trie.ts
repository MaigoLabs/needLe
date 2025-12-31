import type { TrieNode } from '../common';

export const deserializeTrie = (data: number[]) => {
  const nodes: TrieNode[] = [];
  const getNode = (id: number) => nodes[id - 1] ??= { parent: undefined, children: new Map(), tokenIds: [], subTreeTokenIds: [] };
  let currentId = 0;
  for (let i = 0; i < data.length;) {
    const node = getNode(++currentId);
    const parentId = data[i++]!;
    node.parent = parentId !== 0 ? getNode(parentId) : undefined;

    let endOfChildren = i;
    while (endOfChildren < data.length && data[endOfChildren]! > 0) endOfChildren++;
    const numberOfChildren = (endOfChildren - i) / 2;
    for (let j = i; j < i + numberOfChildren; j++) {
      const codePoint = data[j]!;
      const child = getNode(data[j + numberOfChildren]!);
      node.children.set(codePoint, child);
    }
    i = endOfChildren;

    if (data[i] === 0) i++; // No token IDs
    else while (i < data.length && data[i]! < 0) node.tokenIds.push(-data[i++]! - 1);
  }
  const root = nodes[0]!;

  // DFS to construct code point paths for each token
  const tokenCodePoints = new Map<number, string[]>();
  const currentCodePoints: string[] = [];
  const dfsCodePoints = (node: TrieNode) => {
    for (const tokenId of node.tokenIds) tokenCodePoints.set(tokenId, [...currentCodePoints]);
    for (const [codePoint, child] of node.children.entries()) {
      if (child.parent !== node) continue; // Skip grafted paths as these are not the canonical representation of the tokens
      currentCodePoints.push(String.fromCodePoint(codePoint));
      dfsCodePoints(child);
      currentCodePoints.pop();
    }
  };
  dfsCodePoints(root);

  // DFS to construct subTreeTokenIds for each node
  const visitedNodes = new Set<TrieNode>();
  const dfsSubTreeTokenIds = (node: TrieNode) => {
    if (visitedNodes.has(node)) return node.subTreeTokenIds;
    visitedNodes.add(node);
    node.subTreeTokenIds = [...node.tokenIds, ...new Set([...node.children.values()].flatMap(child => dfsSubTreeTokenIds(child)))];
    return node.subTreeTokenIds;
  };
  dfsSubTreeTokenIds(root);

  return {
    root,
    tokenCodePoints,
  };
};

export const getTrieNodeTokenIds = (node: TrieNode | undefined, includeSubTree: boolean) =>
  (includeSubTree ? node?.subTreeTokenIds : node?.tokenIds) ?? [];
