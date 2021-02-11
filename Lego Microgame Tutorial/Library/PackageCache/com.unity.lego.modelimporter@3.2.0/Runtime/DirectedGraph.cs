// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace LEGOModelImporter
{
    public class DirectedGraph<T>
    {
        public class Node
        {
            public T data;
        }

        public class Edge
        {
            public Node src;
            public Node dst;
        }

        private HashSet<Node> nodes = new HashSet<Node>();
        private HashSet<Edge> edges = new HashSet<Edge>();

        public void AddNode(Node node)
        {
            nodes.Add(node);
        }

        public void AddEdge(Node src, Node dst)
        {
            var edge = new Edge() { src = src, dst = dst };
            edges.Add(edge);
        }

        public List<Node> TopologicalSort()
        {
            var result = new List<Node>();
            var visited = new HashSet<Node>();
            var worklist = new Queue<Node>();

            // Find roots.
            foreach (var node in nodes)
            {
                if (!edges.Any(edge => edge.dst == node))
                {
                    worklist.Enqueue(node);
                }
            }

            // Depth-first traversal of graph.
            while (worklist.Count > 0)
            {
                var currentNode = worklist.Dequeue();
                if (!visited.Contains(currentNode))
                {
                    visited.Add(currentNode);
                    result.Add(currentNode);

                    foreach (var edge in edges)
                    {
                        if (edge.src == currentNode)
                        {
                            worklist.Enqueue(edge.dst);
                        }
                    }
                }
            }

            return result;
        }
    }
}
