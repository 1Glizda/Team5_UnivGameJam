using RW.MonumentValley;
using UnityEngine;

public class EnemyInteraction : MonoBehaviour
{
    [SerializeField] private float detectionDistance = 0.3f;
    [SerializeField] private Node teleportNode;

    private Transform player;
    private PlayerController playerController;
    private Graph graph;

    private bool hasInteracted = false;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        playerController = player?.GetComponent<PlayerController>();
        graph = FindFirstObjectByType<Graph>();
    }

    private void Update()
    {
        if (player == null || hasInteracted)
            return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist < detectionDistance)
        {
            hasInteracted = true;
            OnPlayerDetected();
        }
    }

    private void OnPlayerDetected()
    {
        Debug.Log("Interacted with Player");

        if (playerController != null && teleportNode != null)
        {
            playerController.TeleportToNode(teleportNode);
        }
    }
}
