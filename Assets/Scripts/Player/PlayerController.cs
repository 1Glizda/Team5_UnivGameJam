
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

        // dynamic zoomies mechanic removed

        // click indicator
        [SerializeField] Cursor cursor;

        // cursor AnimationController
        private Animator cursorAnimController;
        [SerializeField] private float stepDistance = 0.6f;
        private float distanceSinceLastStep = 0f;

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

        [Header("Special State")]
        public bool specialStateUnlocked = false;
        public bool isInSpecialState = false;
        public float specialStateDuration = 10f;
        private float specialStateTimer = 0f;
        
        [HideInInspector]
        public bool isWallWalking = false;

        public UnityEngine.Events.UnityEvent<bool> onSpecialStateToggled;

        [Header("Skyboxes")]
        public Material normalSkybox;
        public Material trippySkybox;
        public Material skyboxBlendMaterial;

        // The Call Stack for dynamic zone effects
        private List<SpecialZoneEffect> activeEffects = new List<SpecialZoneEffect>();

        private void Update()
        {
            if (specialStateUnlocked && Input.GetMouseButtonDown(1))
            {
                // Toggle ON only. Manual deactivation is disabled as requested.
                if (!isInSpecialState) ActivateSpecialState();
            }

            if (isInSpecialState)
            {
                // If we are currently wall-walking, the special state is "locked" on and the timer pauses!
                if (!isWallWalking)
                {
                    specialStateTimer -= Time.deltaTime;
                    if (specialStateTimer <= 0)
                    {
                        DeactivateSpecialState();
                    }
                }
            }
        }

        public void ActivateSpecialState()
        {
            if (isInSpecialState) return;
            isInSpecialState = true;
            specialStateTimer = specialStateDuration;
            
            if (onSpecialStateToggled != null) onSpecialStateToggled.Invoke(true);
            SoundManager.PlaySound(SoundType.SMOKE);

            StartCoroutine(SkyboxTransition(trippySkybox));

            if (currentNode != null)
            {
                NodeSpecialStateEffects fx = currentNode.GetComponent<NodeSpecialStateEffects>();
                if (fx != null) fx.TurnOn();

                SpecialZoneEffect[] zoneFx = currentNode.GetComponents<SpecialZoneEffect>();
                foreach (var z in zoneFx) PushEffect(z);
            }
        }

        private void DeactivateSpecialState()
        {
            if (!isInSpecialState) return;
            isInSpecialState = false;

            // Force revert all active effects!
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                activeEffects[i].Revert(this);
            }
            activeEffects.Clear();

            if (onSpecialStateToggled != null) onSpecialStateToggled.Invoke(false);

            StartCoroutine(SkyboxTransition(normalSkybox));

            if (currentNode != null)
            {
                NodeSpecialStateEffects fx = currentNode.GetComponent<NodeSpecialStateEffects>();
                if (fx != null) fx.TurnOff();
            }
        }

        public void PushEffect(SpecialZoneEffect effect)
        {
            if (!activeEffects.Contains(effect))
            {
                activeEffects.Add(effect);
                effect.Apply(this);
            }
        }

        public void RemoveEffect(SpecialZoneEffect effect)
        {
            if (activeEffects.Contains(effect))
            {
                effect.Revert(this);
                activeEffects.Remove(effect);
            }
        }

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
            if (!isControlEnabled)
            {
                Debug.Log("[PlayerController] Click ignored: Controls are DISABLED.");
                return;
            }

            if (clickable == null) return;

            if (pathfinder == null)
            {
                Debug.LogError("[PlayerController] Pathfinder is MISSING from the scene!");
                return;
            }

            // For custom continuous meshes with many nodes, find the specific node closest to where the user clicked!
            Node targetNode = graph.FindClosestNode(clickable.ChildNodes, position);
            
            if (targetNode == null)
            {
                Debug.LogWarning("[PlayerController] Clicked on an object with no valid Nodes!");
                return;
            }

            if (currentNode == null)
            {
                Debug.LogWarning("[PlayerController] Player is not standing on any Node! Attempting to snap...");
                SnapToNearestNode();
                if (currentNode == null) return;
            }

            List<Node> newPath = pathfinder.FindPath(currentNode, targetNode);
            Debug.Log($"[PlayerController] Clicked on {targetNode.name}. Path found: {(newPath != null && newPath.Count > 1 ? "YES" : "NO")} (Nodes: {newPath?.Count})");


            // if we are already moving and we click again, stop all previous Animation/motion
            if (isMoving)
            {
                StopAllCoroutines();
                isMoving = false;
                EnableControls(true); // Safety reset
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
                    // Check if we should skip the current node in the path
                    if (i + 1 < path.Count)
                    {
                        Node nextNodeToCheck = path[i];

                        // The user requested to skip ONLY the nodes that have the tag "Skipable"
                        if (nextNodeToCheck.CompareTag("Skipable"))
                        {
                            i++; // Skip this node and aim for the one after it!
                        }
                    }

                    // use the current Node as the next waypoint
                    nextNode = path[i];

                    // move to the next Node
                    yield return StartCoroutine(MoveToNodeRoutine(transform.position, nextNode));
                    
                    // WebGL Safety: Ensure we yield at least once per node to prevent 
                    // a 'Maximum call stack size exceeded' error if many nodes are skipped synchronously
                    yield return null;
                }
            }

            isMoving = false;
            UpdateAnimation();

        }

        // Calculate the correct rotation required to face the next node based on the camera plane
        private Quaternion GetTargetRotation(Vector3 startPosition, Node targetNode)
        {
            if (Camera.main == null || targetNode == null) return transform.rotation;

            Vector3 nextPosition = targetNode.transform.position;
            Vector3 nextPositionScreen = Camera.main.WorldToScreenPoint(nextPosition);
            Ray rayToNextPosition = Camera.main.ScreenPointToRay(nextPositionScreen);
            
            // PREDICTIVE GRAVITY:
            // Use the UP vector of the node we are MOVING TOWARDS.
            // This ensures we rotate to the wall's orientation as we approach it!
            bool targetIsWall = targetNode.GetComponent<WallWalkEffect>() != null;
            Vector3 upVector = targetIsWall ? targetNode.transform.up : Vector3.up;
            
            // Safety: if target isn't a wall, but WE are currently on one, stay sideways until we land on the ground
            if (!targetIsWall && isWallWalking && currentNode != null)
            {
                upVector = currentNode.transform.up;
            }

            Plane plane = new Plane(upVector, startPosition);

            if (plane.Raycast(rayToNextPosition, out float cameraDistance))
            {
                Vector3 nextPositionOnPlane = rayToNextPosition.GetPoint(cameraDistance);
                Vector3 directionToNextNode = nextPositionOnPlane - startPosition;
                
                if (directionToNextNode != Vector3.zero)
                {
                    return Quaternion.LookRotation(directionToNextNode, upVector);
                }
            }
            return transform.rotation;
        }

        //  lerp to another Node from current position
        private IEnumerator MoveToNodeRoutine(Vector3 startPosition, Node targetNode)
        {

            float elapsedTime = 0;

            // validate move time
            moveTime = Mathf.Clamp(moveTime, 0.1f, 5f);
            
            Vector3 targetPos = targetNode.transform.position;
            
            // Calculate the exact distance to the next node
            float distance = Vector3.Distance(startPosition, targetPos);
            
            // If the moveTime is "time per unit", then actual duration is distance * moveTime
            // This guarantees a perfectly smooth, constant speed regardless of how far apart the nodes are!
            float actualDuration = distance * moveTime;
            
            // The user requested to jump if the target node has the tag "Jumpable", 
            // OR if the node we are launching from (currentNode) is "Jumpable"!
            bool isJumping = targetNode.CompareTag("Jumpable") || (currentNode != null && currentNode.CompareTag("Jumpable"));
            
            // Configure cat-like bouncy jump height - adjust as needed
            float jumpHeight = isJumping ? 1.25f : 0.0f;

            // Prevent division by zero if nodes are exactly on top of each other
            if (actualDuration <= 0.001f) actualDuration = 0.001f;

            // Determine what direction we should be facing for this segment
            Quaternion targetRotation = GetTargetRotation(startPosition, targetNode);

            Vector3 previousPosition = transform.position;

            while (elapsedTime < actualDuration && targetNode != null && !HasReachedNode(targetNode))
            {
                // elapsed time tracking
                elapsedTime += Time.deltaTime;
                float lerpValue = Mathf.Clamp(elapsedTime / actualDuration, 0f, 1f);

                // Start with linear interpolation
                Vector3 currentPos = Vector3.Lerp(startPosition, targetPos, lerpValue);
                
                // Add the jump arc if applicable
                if (isJumping)
                {
                    float jumpOffset = Mathf.Sin(lerpValue * Mathf.PI) * jumpHeight;
                    currentPos.y += jumpOffset;
                }

                transform.position = currentPos; 
                
                float movedDistance = Vector3.Distance(transform.position, previousPosition);
                distanceSinceLastStep += movedDistance;

                // Play step sound only when grounded (not jumping)
                if (!isJumping && distanceSinceLastStep >= stepDistance)
                {
                    SoundManager.PlaySound(SoundType.STEPS);
                    distanceSinceLastStep = 0f;
                }

                previousPosition = transform.position;

                // Seamlessly rotate towards the target direction while moving!
                // A lerp speed of 15f means it snaps quickly but smoothly within the first few frames of movement.
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);

                // if over halfway, change parent to next node
                if (lerpValue > 0.51f && currentNode != targetNode)
                {
                    // Exit the OLD node's simple UnityEvent effects
                    if (isInSpecialState && currentNode != null)
                    {
                        NodeSpecialStateEffects oldFx = currentNode.GetComponent<NodeSpecialStateEffects>();
                        if (oldFx != null) oldFx.TurnOff();
                    }

                    transform.parent = targetNode.transform;
                    currentNode = targetNode;

                    // EXIT EFFECTS
                    if (isInSpecialState && currentNode != null)
                    {
                        NodeSpecialStateEffects oldFx = currentNode.GetComponent<NodeSpecialStateEffects>();
                        if (oldFx != null) oldFx.TurnOff();
                    }

                    // ENTER EFFECTS
                    // Check for WallWalkEffect to force-activate special state if needed!
                    WallWalkEffect w = currentNode.GetComponent<WallWalkEffect>();
                    if (w != null && !isInSpecialState)
                    {
                        ActivateSpecialState();
                    }

                    if (isInSpecialState)
                    {
                        // Manage the Dynamic Call Stack for contiguous Zones
                        List<SpecialZoneEffect> toRemove = new List<SpecialZoneEffect>();
                        foreach (var active in activeEffects)
                        {
                            if (currentNode.GetComponent(active.GetType()) == null)
                            {
                                toRemove.Add(active);
                            }
                        }
                        foreach (var r in toRemove) RemoveEffect(r);

                        // Apply new effects
                        SpecialZoneEffect[] newZoneFx = currentNode.GetComponents<SpecialZoneEffect>();
                        foreach (var z in newZoneFx)
                        {
                            bool alreadyHasType = false;
                            foreach (var active in activeEffects)
                            {
                                if (active.GetType() == z.GetType()) alreadyHasType = true;
                            }
                            if (!alreadyHasType) PushEffect(z);
                        }
                    }

                    // invoke UnityEvent associated with next Node ONCE
                    if (targetNode.gameEvent != null)
                    {
                        targetNode.gameEvent.Invoke();
                    }

                    // Automatic Special State Melting
                    if (isInSpecialState)
                    {
                        SpecialMeltableNode meltable = targetNode.GetComponent<SpecialMeltableNode>();
                        if (meltable != null && meltable.linkedMelter != null)
                        {
                            meltable.linkedMelter.TriggerMelt();
                        }
                    }
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



        public void TeleportToNode(Node targetNode)
        {
            if (targetNode == null)
                return;

            StopAllCoroutines();

            isMoving = false;

            transform.position = targetNode.transform.position;
            transform.parent = targetNode.transform;

            currentNode = targetNode;

            UpdateAnimation();
        }

        // Instantly teleports the player to a target node, optionally walking to nodes before and after.
        public void PortalToNode(Node preWalkTarget, Node teleportTarget, Node postWalkTarget)
        {
            Debug.Log($"[PlayerController] PortalToNode called! Pre-Walk: {preWalkTarget?.name}, Teleport: {teleportTarget?.name}, Post-Walk: {postWalkTarget?.name}");
            
            // Safety: If we were already in a portal routine or moving, ensure controls are reset if we interrupt!
            StopAllCoroutines();
            isMoving = false;
            EnableControls(true); 

            StartCoroutine(PortalRoutine(preWalkTarget, teleportTarget, postWalkTarget));
        }

        private IEnumerator PortalRoutine(Node preWalkTarget, Node teleportTarget, Node postWalkTarget)
        {
            bool startedOnWall = isWallWalking;

            if (teleportTarget == null)
            {
                Debug.LogError("[PlayerController] Teleport target is NULL! Aborting teleport.");
                yield break;
            }

            // 1. Disable controls
            EnableControls(false);

            // 2. Pre-Teleport Walk (if provided)
            if (preWalkTarget != null && preWalkTarget != currentNode)
            {
                Debug.Log($"[PlayerController] Forcing pre-walk to {preWalkTarget.name}");
                List<Node> newPath = pathfinder.FindPath(currentNode, preWalkTarget);
                if (newPath != null && newPath.Count > 1)
                {
                    yield return StartCoroutine(FollowPathRoutine(newPath));
                }
            }

            // 3. Instantly Teleport
            isMoving = false;
            transform.position = teleportTarget.transform.position;
            transform.parent = teleportTarget.transform;
            currentNode = teleportTarget;
            UpdateAnimation();
            
            // NEW: Always check for WallWalkEffect to force-activate special state if needed!
            WallWalkEffect landingWall = currentNode.GetComponent<WallWalkEffect>();

            // Immediately orient the player to the wall's UP vector
            // This prevents them from being "stuck upwards" while on a wall.
            if (landingWall != null)
            {
                // We use our current forward but the wall's UP
                // If forward is too close to up, we use a fallback
                Vector3 wallUp = teleportTarget.transform.up;
                if (Mathf.Abs(Vector3.Dot(transform.forward, wallUp)) > 0.99f)
                {
                    transform.rotation = Quaternion.LookRotation(Vector3.Cross(transform.right, wallUp), wallUp);
                }
                else
                {
                    transform.rotation = Quaternion.LookRotation(transform.forward, wallUp);
                }
            }

            // TRIGGER SPECIAL STATE EFFECTS UPON LANDING
            if (landingWall != null && !isInSpecialState)
            {
                ActivateSpecialState();
            }
            else if (landingWall == null && startedOnWall && isInSpecialState)
            {
                // REVERT TO NORMAL MODE WHEN COMING OUT OF THE PORTAL AFTER ANTIGRAVITY
                DeactivateSpecialState();
            }

            if (isInSpecialState)
            {
                // Manage the Dynamic Call Stack for contiguous Zones
                List<SpecialZoneEffect> toRemove = new List<SpecialZoneEffect>();
                foreach (var active in activeEffects)
                {
                    // If the new node doesn't have an effect of the exact same TYPE, we have left the zone!
                    if (currentNode.GetComponent(active.GetType()) == null)
                    {
                        toRemove.Add(active);
                    }
                }

                foreach (var r in toRemove) RemoveEffect(r);

                // Apply new effects from the destination node
                SpecialZoneEffect[] newZoneFx = currentNode.GetComponents<SpecialZoneEffect>();
                foreach (var z in newZoneFx)
                {
                    bool alreadyHasType = false;
                    foreach (var active in activeEffects)
                    {
                        if (active.GetType() == z.GetType()) alreadyHasType = true;
                    }
                    if (!alreadyHasType) PushEffect(z);
                }
            }

            Debug.Log($"[PlayerController] Teleported to {teleportTarget.name}");

            // Wait a frame just in case Cinemachine or other physics need to catch up
            yield return null;

            // 4. Post-Teleport Walk (if provided)
            if (postWalkTarget != null && postWalkTarget != currentNode)
            {
                Debug.Log($"[PlayerController] Forcing post-walk to {postWalkTarget.name}");
                List<Node> newPath = pathfinder.FindPath(currentNode, postWalkTarget);
                if (newPath != null && newPath.Count > 1)
                {
                    yield return StartCoroutine(FollowPathRoutine(newPath));
                }
            }

            // 5. Re-enable controls
            EnableControls(true);
            Debug.Log("[PlayerController] Portal routine finished. Controls restored.");
        }

        private IEnumerator SkyboxTransition(Material targetSkybox)
        {
            if (targetSkybox == null || skyboxBlendMaterial == null)
            {
                RenderSettings.skybox = targetSkybox;
                DynamicGI.UpdateEnvironment();
                yield break;
            }

            Material startSkybox = RenderSettings.skybox;
            RenderSettings.skybox = skyboxBlendMaterial;
            
            // Standard 6-Sided skybox properties
            string[] sides = { "_FrontTex", "_BackTex", "_LeftTex", "_RightTex", "_UpTex", "_DownTex" };

            foreach (string side in sides)
            {
                // Copy from current skybox to Slot 1
                if (startSkybox.HasProperty(side)) 
                    skyboxBlendMaterial.SetTexture(side + "1", startSkybox.GetTexture(side));
                
                // Copy from target skybox to Slot 2
                if (targetSkybox.HasProperty(side)) 
                    skyboxBlendMaterial.SetTexture(side + "2", targetSkybox.GetTexture(side));
            }

            // Sync rotation so there is no visual snap during transition
            if (startSkybox.HasProperty("_Rotation"))
                skyboxBlendMaterial.SetFloat("_Rotation", startSkybox.GetFloat("_Rotation"));

            float elapsed = 0f;
            float duration = 1.0f; 
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                skyboxBlendMaterial.SetFloat("_Blend", t);
                yield return null;
            }

            RenderSettings.skybox = targetSkybox;
            DynamicGI.UpdateEnvironment();
        }
    }
}