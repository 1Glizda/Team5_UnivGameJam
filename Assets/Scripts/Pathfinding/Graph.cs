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
using System.Linq;

namespace RW.MonumentValley
{
    // management class for all Nodes
    public class Graph : MonoBehaviour
    {
        // all of the Nodes in the current level/maze
        private List<Node> allNodes = new List<Node>();

        // end of level
        [SerializeField] private Node goalNode;
        public Node GoalNode => goalNode;

        private void Awake()
        {
            allNodes = FindObjectsOfType<Node>().ToList();
            InitNodes();
        }

        private void Start()
        {
            InitNeighbors();
        }

        // locate the specific Node at target position within rounding error
        public Node FindNodeAt(Vector3 pos)
        {
            foreach (Node n in allNodes)
            {
                Vector3 diff = n.transform.position - pos;

                if (diff.sqrMagnitude < 0.01f)
                {
                    return n;
                }
            }
            return null;
        }

        // locate the closest Node in screen space, given an array of Nodes
        public Node FindClosestNode(Node[] nodes, Vector3 pos)
        {
            Node closestNode = null;
            float closestDistanceSqr = Mathf.Infinity;

            foreach (Node n in nodes)
            {
                Vector3 diff = n.transform.position - pos;

                Vector3 nodeScreenPosition = Camera.main.WorldToScreenPoint(n.transform.position);
                Vector3 screenPosition = Camera.main.WorldToScreenPoint(pos);
                diff = nodeScreenPosition - screenPosition;

                if (diff.sqrMagnitude < closestDistanceSqr)
                {
                    closestNode = n;
                    closestDistanceSqr = diff.sqrMagnitude;
                }
            }
            return closestNode;
        }

        // find the closest Node in the entire Graph
        public Node FindClosestNode(Vector3 pos)
        {
            return FindClosestNode(allNodes.ToArray(), pos);
        }

        // clear breadcrumb trail
        public void ResetNodes()
        {
            foreach (Node node in allNodes)
            {
                node.PreviousNode = null;
            }
        }

        // set the Graph for each Node
        private void InitNodes()
        {
            foreach (Node n in allNodes)
            {
                if (n != null)
                {
                    n.InitGraph(this);
                }
            }
        }

        // set neighbors for each Node; must run AFTER all Nodes are initialized
        private void InitNeighbors()
        {
            foreach (Node n in allNodes)
            {
                if (n != null)
                {
                    n.FindNeighbors();
                }
            }
        }
    }

}