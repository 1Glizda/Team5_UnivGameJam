using RW.MonumentValley;
using UnityEngine;

public class EnemyInteraction : MonoBehaviour
{
    [SerializeField] private Node teleportNode;
    [SerializeField] private PlayerController playerController;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (playerController != null && teleportNode != null)
        {
            playerController.TeleportToNode(teleportNode);
        }
    }
}