
/*
 * Copyright (c) 2020 Razeware LLC
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * Notwithstanding the foregoing, you may not use, copy, modify, merge, publish, 
 * distribute, sublicense, create a derivative work, and/or sell copies of the 
 * Software in any work that is designed, intended, or marketed for pedagogical or 
 * instructional purposes related to programming, coding, application development, 
 * or information technology.  Permission for such use, copying, modification,
 * merger, publication, distribution, sublicensing, creation of derivative works, 
 * or sale is expressly withheld.
 *    
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace RW.MonumentValley
{
    public class Node : MonoBehaviour
    {
        // gizmo colors
        [SerializeField] private float gizmoRadius = 0.1f;
        [SerializeField] private Color defaultGizmoColor = Color.black;
        [SerializeField] private Color selectedGizmoColor = Color.blue;
        [SerializeField] private Color inactiveGizmoColor = Color.gray;

        // neighboring nodes + active state
        [SerializeField] private List<Edge> edges = new List<Edge>();

        // Nodes specifically excluded from Edges
        [SerializeField] private List<Node> excludedNodes;

        // reference to the graph
        private Graph graph;

        // previous Node that forms a "breadcrumb" trail back to the start
        private Node previousNode;

        // invoked when Player enters this node
        public UnityEvent gameEvent;

        [Header("Strict Connections")]
        [Tooltip("If true, this node will ONLY connect to nodes in the strictNeighbours list, completely ignoring the automatic distance check. Other automatic nodes will also ignore this node unless they are in this list.")]
        public bool hasStrictNeighbours = false;
        public List<Node> strictNeighbours = new List<Node>();

        // properties
        
        public Node PreviousNode { get { return previousNode; } set { previousNode = value; } }
        public List<Edge> Edges => edges;

        // 3d compass directions to check for horizontal neighbors automatically(east/west/north/south)
        public static Vector3[] neighborDirections =
        {
            new Vector3(1f, 0f, 0f),
            new Vector3(-1f, 0f, 0f),
            new Vector3(0f, 0f, 1f),
            new Vector3(0f, 0f, -1f),

            // One block up (straight)
            new Vector3(1f, 1f, 0f),
            new Vector3(-1f, 1f, 0f),
            new Vector3(0f, 1f, 1f),
            new Vector3(0f, 1f, -1f),

            // One block down (straight)
            new Vector3(1f, -1f, 0f),
            new Vector3(-1f, -1f, 0f),
            new Vector3(0f, -1f, 1f),
            new Vector3(0f, -1f, -1f),
        };
         
        private void Start()
        {
            // automatic connect Edges with horizontal Nodes
            if (graph != null)
            {
                FindNeighbors();
            }
        }

        // draws a sphere gizmo
        private void OnDrawGizmos()
        {
            Gizmos.color = defaultGizmoColor;
            Gizmos.DrawSphere(transform.position, gizmoRadius);
        }

        // draws a sphere gizmo in a different color when selected
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = selectedGizmoColor;
            Gizmos.DrawSphere(transform.position, gizmoRadius);

            // draws a line to each neighbor
            foreach (Edge e in edges)
            {
                if (e.neighbor != null)
                {
                    Gizmos.color = (e.isActive) ? selectedGizmoColor : inactiveGizmoColor;
                    Gizmos.DrawLine(transform.position, e.neighbor.transform.position);
                }
            }
        }

        // Checks if this Node is physically covered by another block on top of it
        public bool IsCovered()
        {
            int levelLayerIndex = LayerMask.NameToLayer("Level");

            // We check a sphere slightly above the surface (y + 0.5f) to detect both full-height blocks AND custom FBX boxes!
            Collider[] colliders = Physics.OverlapSphere(transform.position + Vector3.up * 0.5f, 0.4f);
            foreach (Collider col in colliders)
            {
                // Only consider blocks that are on the "Level" layer
                if (levelLayerIndex != -1 && col.gameObject.layer != levelLayerIndex) continue;

                // Ignore our own colliders or any child visual meshes
                if (col.transform != this.transform && !col.transform.IsChildOf(this.transform))
                {
                    // If it's an acid puddle, it's a flat hazard, NOT a solid blocking block! We can traverse over it.
                    if (col.GetComponent<AcidPuddle>() != null || col.GetComponentInParent<AcidPuddle>() != null) continue;
                    
                    // Same for SpecialZoneEffects, they are triggers, not solid blocks
                    if (col.GetComponent<SpecialZoneEffect>() != null) continue;

                    return true; // We found a distinct solid block sitting immediately above us!
                }
            }
            return false;
        }

        // fill out edge connections to neighboring nodes automatically
        public void FindNeighbors()
        {
            // If this node is physically covered by a block, it cannot be walked on. Do not establish connections from it.
            if (IsCovered()) return;

            // If this node uses strict neighbours, we ONLY connect to the ones in the manual list!
            if (hasStrictNeighbours)
            {
                foreach (Node neighbor in strictNeighbours)
                {
                    if (neighbor != null && !neighbor.IsCovered() && !excludedNodes.Contains(neighbor))
                    {
                        if (!HasNeighbor(neighbor))
                        {
                            edges.Add(new Edge { neighbor = neighbor, isActive = true });
                        }
                    }
                }
                return; // SKIP the automatic distance check completely for this node!
            }

            // Freeform Distance Check: Instead of strict 1.0 unit grid vectors, we check all nodes in the graph
            // and automatically connect to any node that is within a traversable distance (e.g. 2.5 units).
            if (graph != null)
            {
                foreach (Node otherNode in graph.GetAllNodes())
                {
                    if (otherNode == this || otherNode == null) continue;

                    // IMPORTANT: If the OTHER node has strict neighbours, and it DOES NOT include THIS node in its manual list,
                    // we MUST NOT automatically connect to it. This prevents automatic nodes from bypassing strict rules!
                    if (otherNode.hasStrictNeighbours && !otherNode.strictNeighbours.Contains(this))
                    {
                        continue;
                    }

                    float distSqr = (transform.position - otherNode.transform.position).sqrMagnitude;
                    
                    // If the node is within 2.5 units (sqrMagnitude 6.25), consider it a neighbor!
                    if (distSqr <= 4.25f)
                    {
                        if (!HasNeighbor(otherNode) && !excludedNodes.Contains(otherNode) && !otherNode.IsCovered())
                        {
                            Edge newEdge = new Edge { neighbor = otherNode, isActive = true };
                            edges.Add(newEdge);
                        }
                    }
                }
            }
        }

        // is a Node already in the Edges List?
        private bool HasNeighbor(Node node)
        {
            foreach (Edge e in edges)
            {
                if (e.neighbor != null && e.neighbor.Equals(node))
                {
                    return true;
                }
            }
            return false;
        }

        // given a specific neighbor, sets active state
        public void EnableEdge(Node neighborNode, bool state)
        {
            foreach (Edge e in edges)
            {
                if (e.neighbor.Equals(neighborNode))
                {
                    e.isActive = state;
                }
            }
        }

        // Returns true if the connection to the neighbor exists AND is active
        public bool IsEdgeActive(Node neighborNode)
        {
            foreach (Edge e in edges)
            {
                if (e.neighbor.Equals(neighborNode))
                {
                    return e.isActive;
                }
            }
            return false;
        }

        public void InitGraph(Graph graphToInit)
        {
            this.graph = graphToInit;
        }
    }
}