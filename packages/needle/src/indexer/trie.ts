import { traverseTrie, type TrieNode } from '../common';

const newNode = (parent?: TrieNode): TrieNode => ({ parent, children: new Map(), tokenIds: [], subTreeTokenIds: [] });

// Assume tokens are unique.
export const buildTrie = (tokens: [id: number, text: string][]) => {
  const root = newNode(undefined);
  for (const [id, text] of tokens) {
    let node = root;
    for (const char of text) {
      const codePoint = char.codePointAt(0)!;
      let childNode = node.children.get(codePoint);
      if (!childNode) {
        childNode = newNode(node);
        node.children.set(codePoint, childNode);
      }
      node = childNode;
      node.subTreeTokenIds.push(id);
    }
    node.tokenIds.push(id);
  }
  return root;
};

export const graftTriePaths = (root: TrieNode, rules: Record<string, string>) => {
  for (const [inputPhrase, graftTo] of Object.entries(rules)) if ([...graftTo].length > [...inputPhrase].length) throw new Error(`Graft rule ${inputPhrase} -> ${graftTo} maps to longer string and may cause infinite loop`);
  const visitedNodes = new Set<TrieNode>();
  const graftFromNode = (node: TrieNode, recursiveChildren: boolean) => {
    if (visitedNodes.has(node)) return;
    visitedNodes.add(node);
    if (recursiveChildren) for (const [, childNode] of node.children) graftFromNode(childNode, true);
    while (true) {
      const nodesWithNewGraftedChildren = new Map<TrieNode, /* depth from initial node */ number>();
      for (const [inputPhrase, graftTo] of Object.entries(rules)) {
        const targetNode = traverseTrie(node, graftTo);
        if (!targetNode) continue;
        const codePoints = [...inputPhrase];
        const graftedPath = Array.from<TrieNode>({ length: codePoints.length - 1 });
        let isGrafted = false;
        let currentNode = node;
        for (let i = 0; i < codePoints.length; i++) {
          const codePoint = codePoints[i]!.codePointAt(0)!;
          let childNode = currentNode.children.get(codePoint);
          if (i === codePoints.length - 1) {
            if (childNode) {
              if (childNode !== targetNode) throw new Error(`Grafted path ${inputPhrase} conflicts with existing path`);
              // Already grafted
            } else {
              currentNode.children.set(codePoint, childNode = targetNode);
              isGrafted = true;
            }
          } else {
            if (!childNode) {
              childNode = newNode(currentNode);
              childNode.subTreeTokenIds = targetNode.subTreeTokenIds;
              currentNode.children.set(codePoint, childNode);
            } else {
              // Part of another grafted path?
              childNode.subTreeTokenIds = Array.from(new Set([...childNode.subTreeTokenIds, ...targetNode.subTreeTokenIds]));
            }
            graftedPath[i] = currentNode = childNode;
          }
        }
        if (isGrafted) for (const [i, nodeToAdd] of graftedPath.entries()) nodesWithNewGraftedChildren.set(nodeToAdd, i + 1);
      }

      if (nodesWithNewGraftedChildren.size > 0) {
        // Re-check graft rules on the newly grafted path
        // 1. No need to recursive other children (not on this path) since their children are not affected
        // 2. No need to consider ancestors of this node since they're handled later (we run in DFS order)
        const sortedNodes = [...nodesWithNewGraftedChildren.entries()].sort((a, b) => b[1] - a[1]);
        for (const [changedNode] of sortedNodes) graftFromNode(changedNode, false);
      } else {
        // No new grafts applied
        break;
      }
    }
  };
  graftFromNode(root, true);
};

export const serializeTrie = (root: TrieNode) => {
  const nodeEntries = new Map<TrieNode, {
    id: number;
    visited: boolean;
    data?: number[];
  }>();
  let currentId = 0;
  const getNodeEntry = (node: TrieNode) => {
    let entry = nodeEntries.get(node);
    if (!entry) {
      entry = { id: ++currentId, visited: false };
      nodeEntries.set(node, entry);
    }
    return entry;
  };
  const serializeNode = (node: TrieNode) => {
    const entry = getNodeEntry(node);
    if (entry.visited) return entry.id;
    entry.visited = true;
    const children = [...node.children.entries()].map(([codePoint, childNode]) => [codePoint, serializeNode(childNode)] as const);
    entry.data = [
      node.parent ? getNodeEntry(node.parent).id : 0,
      ...children.map(child => child[0]), // code points
      ...children.map(child => child[1]), // child node ids
      // End of children list (<= 0 are not valid code points nor node IDs)
      ...node.tokenIds.length > 0
        ? node.tokenIds.map(tokenId => -(tokenId + 1)) // Use the negative value of (tokenId + 1)
        : [0], // End of children list, no token IDs (token IDs are encoded to negative values)
    ];
    return entry.id;
  };
  serializeNode(root);
  return [...nodeEntries.values()].sort((a, b) => a.id - b.id).flatMap(node => node.data ?? []);
};
