
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RW.MonumentValley
{
    // handles Player input and movement
    [RequireComponent(typeof(PlayerAnimation))]
    public class PlayerController : MonoBehaviour
    {

        //  time to move one unit
        [Range(0.25f, 2f)]
        [SerializeField] private float moveTime = 0.5f;

        // dynamic zoomies mechanic
        private float currentZoomieMultiplier = 0.5f;

        // click indicator
        [SerializeField] Cursor cursor;

        // cursor AnimationController
        private Animator cursorAnimController;

        // pathfinding fields
        private Clickable[] clickables;
        private Pathfinder pathfinder;
        private Graph graph;
        private Node currentNode;
        private Node nextNode;

        // flags
        private bool isMoving;
        private bool isControlEnabled;
        private PlayerAnimation playerAnimation;

        private void Awake()
        {
            //  initialize fields
            clickables = FindObjectsOfType<Clickable>();
            pathfinder = FindObjectOfType<Pathfinder>();
            playerAnimation = GetComponent<PlayerAnimation>();

            if (pathfinder != null)
            {
                graph = pathfinder.GetComponent<Graph>();
            }

            isMoving = false;
            isControlEnabled = true;
        }

        private void Start()
        {
            // always start on a Node
            SnapToNearestNode();

            // automatically set the Graph's StartNode 
            if (pathfinder != null && !pathfinder.SearchOnStart)
            {
                pathfinder.SetStartNode(transform.position);
            }

            //listen to all clickEvents
            foreach (Clickable c in clickables)
            {
                c.clickAction += OnClick;
            }

            StartCoroutine(RandomZoomiesRoutine());
        }

        private void OnDisable()
        {
            // unsubscribe from clickEvents when disabled
            foreach (Clickable c in clickables)
            {
                c.clickAction -= OnClick;
            }
        }

        private void OnClick(Clickable clickable, Vector3 position)
        {
            if (!isControlEnabled || clickable == null || pathfinder == null)
            {
                return;
            }

            // find the best path to the any Nodes under the Clickable; gives the user some flexibility
            List<Node> newPath = pathfinder.FindBestPath(currentNode, clickable.ChildNodes);

            // if we are already moving and we click again, stop all previous Animation/motion
            if (isMoving)
            {
                StopAllCoroutines();
            }

            // show a marker for the mouse click
            if (cursor != null)
            {
                cursor.ShowCursor(position);
            }

            // if we have a valid path, follow it
            if (newPath.Count > 1)
            {
                StartCoroutine(FollowPathRoutine(newPath));
            }
            else
            {
                // otherwise, invalid path, stop movement
                isMoving = false;
                UpdateAnimation();
            }
        }



        private IEnumerator FollowPathRoutine(List<Node> path)
        {
            // start moving
            isMoving = true;

            if (path == null || path.Count <= 1)
            {
                Debug.Log("PLAYERCONTROLLER FollowPathRoutine: invalid path");
            }
            else
            {
                UpdateAnimation();

                // loop through all Nodes
                for (int i = 0; i < path.Count; i++)
                {
                    // Check for corner cutting (skipping a node)
                    if (i + 1 < path.Count)
                    {
                        Node cornerNode = path[i];
                        Node targetNode = path[i + 1];

                        // Determine if skipping cornerNode forms a diagonal jump from our current position
                        bool isDiagonal = Mathf.Abs(targetNode.transform.position.x - transform.position.x) > 0.1f && 
                                          Mathf.Abs(targetNode.transform.position.z - transform.position.z) > 0.1f;

                        if (isDiagonal)
                        {
                            // We can skip the corner node if it's tagged "skipable"
                            // (If the corner node doesn't exist, it wouldn't be in the path anyway, so this covers the user's manual path setups)
                            if (cornerNode.CompareTag("Skipable"))
                            {
                                i++; // Skip the corner!
                            }
                        }
                    }

                    // use the current Node as the next waypoint
                    nextNode = path[i];

                    // aim at the Node after that to minimize flipping
                    int nextAimIndex = Mathf.Clamp(i + 1, 0, path.Count - 1);
                    Node aimNode = path[nextAimIndex];
                    FaceNextPosition(transform.position, aimNode.transform.position);

                    // move to the next Node
                    yield return StartCoroutine(MoveToNodeRoutine(transform.position, nextNode));
                }
            }

            isMoving = false;
            UpdateAnimation();

        }

        //  lerp to another Node from current position
        private IEnumerator MoveToNodeRoutine(Vector3 startPosition, Node targetNode)
        {

            float elapsedTime = 0;

            // validate move time
            moveTime = Mathf.Clamp(moveTime, 0.1f, 5f);
            
            // Cat-like jump mechanics logic
            Vector3 targetPos = targetNode.transform.position;
            
            // Determine if the move is a jump:
            // 1. Vertical difference (1 block up or down)
            bool isElevationJump = Mathf.Abs(targetPos.y - startPosition.y) > 0.1f;
            // 2. Diagonal horizontal movement (corner cutting)
            bool isDiagonalJump = Mathf.Abs(targetPos.x - startPosition.x) > 0.1f && Mathf.Abs(targetPos.z - startPosition.z) > 0.1f;
            
            bool isJumping = isElevationJump || isDiagonalJump;
            
            // Configure cat-like bouncy jump height - adjust as needed
            float jumpHeight = isElevationJump ? 1.25f : 0.75f;

            while (elapsedTime < moveTime && targetNode != null && !HasReachedNode(targetNode))
            {

                // dynamically speed up elapsed time tracking using the multiplier
                elapsedTime += Time.deltaTime / currentZoomieMultiplier;
                float lerpValue = Mathf.Clamp(elapsedTime / moveTime, 0f, 1f);

                // Start with linear interpolation
                Vector3 currentPos = Vector3.Lerp(startPosition, targetPos, lerpValue);
                
                // Add the jump arc if applicable
                if (isJumping)
                {
                    float jumpOffset = Mathf.Sin(lerpValue * Mathf.PI) * jumpHeight;
                    currentPos.y += jumpOffset;
                }

                transform.position = currentPos;

                // if over halfway, change parent to next node
                if (lerpValue > 0.51f)
                {
                    transform.parent = targetNode.transform;
                    currentNode = targetNode;

                    // invoke UnityEvent associated with next Node
                    targetNode.gameEvent.Invoke();
                    //Debug.Log("invoked GameEvent from targetNode: " + targetNode.name);
                }

                // wait one frame
                yield return null;
            }
        }

        // snap the Player to the nearest Node in Game view
        public void SnapToNearestNode()
        {
            Node nearestNode = graph?.FindClosestNode(transform.position);
            if (nearestNode != null)
            {
                currentNode = nearestNode;
                transform.position = nearestNode.transform.position;
            }
        }

        // turn face the next Node, always projected on a plane at the Player's feet
        public void FaceNextPosition(Vector3 startPosition, Vector3 nextPosition)
        {
            if (Camera.main == null)
            {
                return;
            }

            // convert next Node world space to screen space
            Vector3 nextPositionScreen = Camera.main.WorldToScreenPoint(nextPosition);

            // convert next Node screen point to Ray
            Ray rayToNextPosition = Camera.main.ScreenPointToRay(nextPositionScreen);

            // plane at player's feet
            Plane plane = new Plane(Vector3.up, startPosition);

            // distance from camera (used for projecting point onto plane)
            float cameraDistance = 0f;

            // project the nextNode onto the plane and face toward projected point
            if (plane.Raycast(rayToNextPosition, out cameraDistance))
            {
                Vector3 nextPositionOnPlane = rayToNextPosition.GetPoint(cameraDistance);
                Vector3 directionToNextNode = nextPositionOnPlane - startPosition;
                if (directionToNextNode != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(directionToNextNode);
                }
            }
        }

        // toggle between Idle and Walk animations
        private void UpdateAnimation()
        {
            if (playerAnimation != null)
            {
                playerAnimation.ToggleAnimation(isMoving);
            }
        }

        // have we reached a specific Node?
        public bool HasReachedNode(Node node)
        {
            if (pathfinder == null || graph == null || node == null)
            {
                return false;
            }

            float distanceSqr = (node.transform.position - transform.position).sqrMagnitude;

            return (distanceSqr < 0.01f);
        }

        // have we reached the end of the graph?
        public bool HasReachedGoal()
        {
            if (graph == null)
            {
                return false;
            }
            return HasReachedNode(graph.GoalNode);
        }

        //  enable/disable controls
        public void EnableControls(bool state)
        {
            isControlEnabled = state;
        }

        // background routine to casually check for randomly triggering the zoomies
        private IEnumerator RandomZoomiesRoutine()
        {
            while (true)
            {
                // Only roll for zoomies while currently moving and not already zooming
                if (isMoving && currentZoomieMultiplier == 1.0f)
                {
                    // 15% chance to start zoomies
                    if (Random.value < 0.15f) 
                    {
                        yield return StartCoroutine(ExecuteZoomiesBurst());
                    }
                }
                yield return new WaitForSeconds(0.5f); // Check twice a second
            }
        }

        // eases in and out of a 5x speed boost
        private IEnumerator ExecuteZoomiesBurst()
        {
            float easeTime = 0.5f; // half a second to accelerate
            float burstDuration = Random.Range(1f, 2.5f); // 1-2.5 seconds of pure sprint
            
            // Ease In (Accelerate)
            float t = 0;
            while (t < easeTime)
            {
                t += Time.deltaTime;
                // smoothstep from 1.0 down to 0.2 (which means 5x faster since we divide by it)
                currentZoomieMultiplier = Mathf.SmoothStep(1.0f, 0.2f, t / easeTime);
                yield return null;
            }

            // Sprint
            currentZoomieMultiplier = 0.2f;
            yield return new WaitForSeconds(burstDuration);

            // Ease Out (Decelerate)
            t = 0;
            while (t < easeTime)
            {
                t += Time.deltaTime;
                currentZoomieMultiplier = Mathf.SmoothStep(0.2f, 1.0f, t / easeTime);
                yield return null;
            }
            
            currentZoomieMultiplier = 1.0f;
        }
    }
}