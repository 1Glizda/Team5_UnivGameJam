using RW.MonumentValley;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f; // Units per second (Higher is FASTER)
    [SerializeField] private Node[] patrolNodes;
    [SerializeField] private bool[] teleportToNode;

    private Pathfinder pathfinder;
    private Graph graph;

    private Node currentNode;
    private int patrolIndex = 0;

    private bool isMoving;
    private PlayerAnimation enemyAnimation;

    // The rotation we are aiming for
    private Quaternion targetRotation;

    public void TeleportToNode(Node targetNode)
    {
        if (targetNode == null) return;

        isMoving = false;
        transform.position = targetNode.transform.position;
        transform.parent = targetNode.transform;
        currentNode = targetNode;
        
        if (enemyAnimation != null) enemyAnimation.ToggleAnimation(false);
    }

    private void Awake()
    {
        pathfinder = FindFirstObjectByType<Pathfinder>();
        graph = pathfinder.GetComponent<Graph>();
        enemyAnimation = GetComponent<PlayerAnimation>();
    }

    private void Start()
    {
        SnapToNearestNode();
        StartCoroutine(PatrolRoutine());
    }

    private void SnapToNearestNode()
    {
        Node nearestNode = graph.FindClosestNode(transform.position);
        if (nearestNode != null)
        {
            currentNode = nearestNode;
            transform.position = nearestNode.transform.position;
        }
    }

    private IEnumerator PatrolRoutine()
    {
        while (true)
        {
            if (patrolNodes.Length == 0)
                yield break;

            Node targetNode = patrolNodes[patrolIndex];

            // Determine if we should teleport to this node
            bool shouldTeleport = false;
            if (teleportToNode != null && patrolIndex < teleportToNode.Length)
            {
                shouldTeleport = teleportToNode[patrolIndex];
            }

            if (shouldTeleport)
            {
                Debug.Log($"[EnemyController] Teleporting to {targetNode.name}");
                TeleportToNode(targetNode);
            }
            else
            {
                List<Node> path = pathfinder.FindPath(currentNode, targetNode);

                if (path != null && path.Count > 1)
                {
                    yield return StartCoroutine(FollowPath(path));
                }
                else
                {
                    Debug.LogWarning($"[EnemyController] Blocked! No path found from {currentNode?.name} to {targetNode.name}. Check if nodes are connected or if a melter is blocking the way.");
                }
            }

            patrolIndex = (patrolIndex + 1) % patrolNodes.Length;

            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator FollowPath(List<Node> path)
    {
        isMoving = true;

        enemyAnimation.ToggleAnimation(isMoving);

        for (int i = 1; i < path.Count; i++)
        {
            Node nextNode = path[i];

            yield return StartCoroutine(MoveToNode(nextNode));
        }

        isMoving = false;

        enemyAnimation.ToggleAnimation(isMoving);
    }

    private IEnumerator MoveToNode(Node targetNode)
    {
        Vector3 startPosition = transform.position;
        Vector3 targetPos = targetNode.transform.position;
        float elapsedTime = 0f;

        // Calculate exact duration: Time = Distance / Speed
        float distance = Vector3.Distance(startPosition, targetPos);
        
        // Prevent speed being 0
        if (moveSpeed <= 0.1f) moveSpeed = 0.1f;
        
        float actualDuration = distance / moveSpeed;
        if (actualDuration <= 0.001f) actualDuration = 0.001f;

        // Calculate the target rotation based on the direction we're heading
        targetRotation = GetTargetRotation(startPosition, targetNode);

        while (elapsedTime < actualDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / actualDuration);

            // Smooth linear movement
            transform.position = Vector3.Lerp(startPosition, targetPos, t);

            // Smooth Slerp rotation (matches the player's feel)
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);

            if (t > 0.51f)
            {
                transform.parent = targetNode.transform;
                currentNode = targetNode;
            }

            yield return null;
        }
        
        // Final snap to position and rotation
        transform.position = targetPos;
        transform.rotation = targetRotation;
    }

    private Quaternion GetTargetRotation(Vector3 startPosition, Node targetNode)
    {
        if (targetNode == null) return transform.rotation;
        
        Vector3 direction = targetNode.transform.position - startPosition;
        
        // If the enemy is on a wall, we should use the node's up vector (like the player)
        Vector3 upVector = Vector3.up;
        if (targetNode.GetComponent<WallWalkEffect>() != null)
        {
            upVector = targetNode.transform.up;
        }
        else if (currentNode != null && currentNode.GetComponent<WallWalkEffect>() != null)
        {
            upVector = currentNode.transform.up;
        }

        if (direction != Vector3.zero)
        {
            return Quaternion.LookRotation(direction, upVector);
        }
        
        return transform.rotation;
    }


}