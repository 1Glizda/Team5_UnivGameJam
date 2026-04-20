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


using UnityEngine;

namespace RW.MonumentValley
{
    // class to activate/deactivate special Edges between Nodes based on rotation
    [System.Serializable]
    public class RotationLink
    {
        // transform to check
        public Transform linkedTransform;

        // euler angle needed to activate link
        public Vector3 activeEulerAngle;
        [Header("Nodes to activate")]
        public Node nodeA;
        public Node nodeB;
    }

    // activates or deactivates special Edges between Nodes
    public class Linker : MonoBehaviour
    {
        [SerializeField] public RotationLink[] rotationLinks;

        // toggle active state of Edge between neighbor Nodes
        public void EnableLink(Node nodeA, Node nodeB, bool state)
        {
            if (nodeA == null || nodeB == null)
                return;

            nodeA.EnableEdge(nodeB, state);
            nodeB.EnableEdge(nodeA, state);
        }

        // enable/disable based on transform's euler angles
        public void UpdateRotationLinks()
        {
            foreach (RotationLink l in rotationLinks)
            {
                if (l.linkedTransform == null || l.nodeA == null || l.nodeB == null)
                    continue;

                // check difference between desired and current angle
                Quaternion targetAngle = Quaternion.Euler(l.activeEulerAngle);
                float angleDiff = Quaternion.Angle(l.linkedTransform.rotation, targetAngle);

                // enable the linked Edges if the angle matches; otherwise disable
                if (Mathf.Abs(angleDiff) < 0.01f)
                {
                    EnableLink(l.nodeA, l.nodeB, true);
                }
                else
                {
                    EnableLink(l.nodeA, l.nodeB, false);
                }
            }
        }

        // update links when we begin
        private void Start()
        {
            UpdateRotationLinks();
        }
    }
}