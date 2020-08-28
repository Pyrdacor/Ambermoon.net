using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy.Compression
{
    /// <summary>
    /// This is a modified trie. It only stores 1 byte literal (key)
    /// in each branch node but the leaf nodes may contain a whole
    /// byte array (theoretically up to 17 bytes). Each node also contains a
    /// last match offset which is updated if a node is visit again.
    /// 
    /// An additional dictionary keeps track of nodes that are no longer
    /// valid due to leaving the match window. Those nodes are removed
    /// after each adding so that they won't be considered any longer.
    /// </summary>
    public class MatchTrie
    {
        abstract class Node
        {
            public BranchNode Parent { get; set; }
            public byte Key { get; set; }
            public int LastMatchOffset { get; set; }
            public abstract Node GetChild(byte symbol);
        }

        class BranchNode : Node
        {
            public SortedDictionary<byte, Node> Children { get; } = new SortedDictionary<byte, Node>();
            public override Node GetChild(byte symbol) => Children.TryGetValue(symbol, out Node child) ? child : null;
        }

        class LeafNode : Node
        {
            public int Offset { get; set; }
            public int Length { get; set; }
            public override Node GetChild(byte symbol) => null;
            public int GetLeafMatchLength(byte[] sequence, int offset, int length)
            {
                int i;
                int compareLength = Math.Min(length, Length);

                for (i = 0; i < compareLength; ++i)
                {
                    if (sequence[offset + i] != sequence[Offset + i])
                        break;
                }

                return i;
            }
            public void Truncate(byte[] sequence, int length)
            {
                Key = sequence[Offset + length];
                Offset += length + 1;
                Length -= length + 1;
            }
        }

        readonly BranchNode rootNode = new BranchNode();
        readonly SortedDictionary<int, Node> matchNodes = new SortedDictionary<int, Node>();
        readonly int maxMatchOffset = (1 << 12) - 1;

        public MatchTrie()
        {

        }

        public MatchTrie(int maxMatchOffset)
        {
            this.maxMatchOffset = maxMatchOffset;
        }

        /// <summary>
        /// Add a new sequence (mostly 18 bytes as this is the max match length for LOB).
        /// It can be shorter (down to 3) at the end of a stream.
        /// 
        /// Note: Length should never exceed 18 (and never be smaller than 3) but we won't
        /// check it here each time to keep performance higher.
        /// </summary>
        public void Add(byte[] sequence, int offset, int length)
        {
            Node node = rootNode;

            for (int i = 0; i < length; ++i)
            {
                var child = node.GetChild(sequence[offset + i]);

                if (child != null)
                {
                    // found a child, proceed inside the trie
                    if (child is BranchNode)
                        child.LastMatchOffset = offset;
                    node = child;
                }
                else
                {
                    // no child found

                    if (node is LeafNode leaf)
                    {
                        // split the leaf
                        int matchLength = leaf.GetLeafMatchLength(sequence, offset + i, length - i);

                        if (matchLength == length - i) // full match, update last match offset only
                        {
                            leaf.LastMatchOffset = offset;
                        }
                        else
                        {
                            var parent = leaf.Parent;
                            int n;

                            // transform the leaf node into a branch node and create two child leaf nodes
                            var newBranchNode = new BranchNode
                            {
                                Parent = parent,
                                Key = leaf.Key,
                                LastMatchOffset = offset
                            };

                            parent.Children[newBranchNode.Key] = newBranchNode;
                            parent = newBranchNode;

                            for (n = 0; n < matchLength; ++n)
                            {
                                newBranchNode = new BranchNode
                                {
                                    Parent = parent,
                                    Key = sequence[leaf.Offset + n],
                                    LastMatchOffset = offset
                                };

                                parent.Children.Add(newBranchNode.Key, newBranchNode);
                                parent = newBranchNode;
                            }

                            var newLeaf = new LeafNode
                            {
                                Parent = parent,
                                Key = sequence[offset + i + n],
                                Offset = offset + i + n + 1,
                                Length = length - i - n - 1,
                                LastMatchOffset = offset
                            };

                            parent.Children.Add(newLeaf.Key, newLeaf);

                            leaf.Truncate(sequence, matchLength);
                            leaf.Parent = parent;

                            parent.Children[leaf.Key] = leaf;

                            node = newLeaf;
                        }
                    }
                    else if (node is BranchNode branch) // branch node, but the key is not there yet
                    {
                        // add new leaf
                        var newLeaf = new LeafNode
                        {
                            Parent = branch,
                            Key = sequence[offset + i],
                            Offset = offset + i + 1,
                            Length = length - i - 1,
                            LastMatchOffset = offset
                        };

                        branch.Children.Add(newLeaf.Key, newLeaf);
                        node = newLeaf;
                    }

                    break;
                }
            }

            matchNodes.Add(offset, node);

            // Remove nodes that are too far away
            int firstOffset = offset - maxMatchOffset + 1; // we check match before adding new sequences so add 1 to the first offset here
            foreach (var matchNode in matchNodes.ToList())
            {
                if (matchNode.Key < firstOffset)
                {
                    Remove(matchNode.Value, firstOffset);
                    matchNodes.Remove(matchNode.Key);
                }
                else
                    break;
            }
        }

        void Remove(Node node, int firstOffset)
        {
            do
            {
                if (node.LastMatchOffset < firstOffset)
                    node.Parent.Children.Remove(node.Key);
                else
                    break;

                node = node.Parent;
            } while (node != rootNode);
        }

        /// <summary>
        /// Gets the longest match.
        /// Key = Offset
        /// Value = Length
        /// </summary>
        public KeyValuePair<int, int> GetLongestMatch(byte[] sequence, int searchOffset, int maxLength)
        {
            Node node = rootNode;
            int i;

            for (i = 0; i < maxLength; ++i)
            {
                var child = node.GetChild(sequence[searchOffset + i]);

                if (child == null)
                    break;

                node = child;
            }

            if (node == rootNode)
                return new KeyValuePair<int, int>(-1, 0);

            // node now contains the node with the longest match
            if (node is LeafNode leaf)
            {
                int parentMatchLength = i;

                return new KeyValuePair<int, int>(leaf.LastMatchOffset,
                    Math.Min(maxLength, parentMatchLength + leaf.GetLeafMatchLength(sequence, searchOffset + parentMatchLength, maxLength - parentMatchLength)));
            }
            else
            {
                return new KeyValuePair<int, int>(node.LastMatchOffset, i);
            }
        }
    }
}
