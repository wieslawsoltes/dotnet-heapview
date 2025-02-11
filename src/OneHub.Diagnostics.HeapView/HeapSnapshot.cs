﻿using Graphs;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace OneHub.Diagnostics.HeapView;

public class HeapSnapshot
{
    private GCHeapDump heapDump;
    private RefGraph refGraph;

    private int[] postOrderIndex2NodeIndex;
    //private int[]? nodeIndex2PostOrderIndex;
    //private int[] dominatorsTree;
    private ulong[] retainedSizes;

    public MemoryGraph MemoryGraph => heapDump.MemoryGraph;
    public RefGraph RefGraph => refGraph;
    public ulong GetRetainedSize(NodeIndex nodeIndex) => retainedSizes[(int)nodeIndex];

    public HeapSnapshot(GCHeapDump heapDump)
    {
        this.heapDump = heapDump;
        this.refGraph = new RefGraph(heapDump.MemoryGraph);
        BuildPostOrderIndex();
        CalculateRetainedSizes();
        Debug.Assert(postOrderIndex2NodeIndex != null);
        Debug.Assert(retainedSizes != null);
    }


    private void BuildPostOrderIndex()
    {
        var graph = this.heapDump.MemoryGraph;
        postOrderIndex2NodeIndex = new int[(int)graph.NodeIndexLimit];
        //nodeIndex2PostOrderIndex = new int[(int)graph.NodeIndexLimit];
        var visited = new BitArray((int)graph.NodeIndexLimit);
        var nodeStack = new Stack<Node>();
        int postOrderIndex = 0;

        var rootNode = graph.GetNode(graph.RootIndex, graph.AllocNodeStorage());
        rootNode.ResetChildrenEnumeration();
        nodeStack.Push(rootNode);

        while (nodeStack.Count > 0)
        {
            var currentNode = nodeStack.Peek();
            NodeIndex nextChild = currentNode.GetNextChildIndex();
            if (nextChild != NodeIndex.Invalid)
            {
                if (visited.Get((int)nextChild))
                    continue;
                var childNode = graph.GetNode(nextChild, graph.AllocNodeStorage());
                childNode.ResetChildrenEnumeration();
                nodeStack.Push(childNode);
                visited.Set((int)nextChild, true);
            }
            else
            {
                //nodeIndex2PostOrderIndex[(int)currentNode.Index] = postOrderIndex;
                postOrderIndex2NodeIndex[postOrderIndex] = (int)currentNode.Index;
                postOrderIndex++;
                nodeStack.Pop();
            }
        }
    }

    private void CalculateRetainedSizes()
    {
        retainedSizes = new ulong[(int)heapDump.MemoryGraph.NodeIndexLimit];

        var nodeStorage = heapDump.MemoryGraph.AllocNodeStorage();
        for (NodeIndex nodeIndex = 0; nodeIndex < heapDump.MemoryGraph.NodeIndexLimit; nodeIndex++)
        {
            Node node = heapDump.MemoryGraph.GetNode(nodeIndex, nodeStorage);
            retainedSizes[(int)nodeIndex] = (ulong)node.Size;
        }

        var spanningTree = new SpanningTree(heapDump.MemoryGraph, TextWriter.Null);
        spanningTree.ForEach(null!);

        // Propagate retained sizes for each node excluding root.
        int nodeCount = (int)heapDump.MemoryGraph.NodeIndexLimit;
        for (int postOrderIndex = 0; postOrderIndex < nodeCount - 1; ++postOrderIndex)
        {
            int nodeIndex = postOrderIndex2NodeIndex[postOrderIndex];
            int dominatorOrdinal = (int)spanningTree.Parent((NodeIndex)nodeIndex);
            if (dominatorOrdinal >= 0)
                retainedSizes[dominatorOrdinal] += retainedSizes[nodeIndex];
        }
    }
}

