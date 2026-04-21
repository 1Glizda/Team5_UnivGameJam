using RW.MonumentValley;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private float moveTime = 0.5f;
    [SerializeField] private Node[] patrolNodes;

    private Pathfinder pathfinder;
    private Graph graph;

    private Node currentNode;
    private int patrolIndex = 0;

    private bool isMoving;
    private PlayerAnimation enemyAnimation;

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

            List<Node> path = pathfinder.FindPath(currentNode, targetNode);

            if (path.Count > 1)
            {
                yield return StartCoroutine(FollowPath(path));
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

            FaceNextNode(transform.position, nextNode.transform.position);

            yield return StartCoroutine(MoveToNode(nextNode));
        }

        isMoving = false;

        enemyAnimation.ToggleAnimation(isMoving);
    }

    private IEnumerator MoveToNode(Node targetNode)
    {
        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < moveTime)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / moveTime);

            transform.position = Vector3.Lerp(startPosition, targetNode.transform.position, t);

            if (t > 0.5f)
            {
                transform.parent = targetNode.transform;
                currentNode = targetNode;
            }

            yield return null;
        }
    }

    private void FaceNextNode(Vector3 currentPosition, Vector3 nextPosition)
    {
        Vector3 direction = nextPosition - currentPosition;

        direction.y = 0;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}