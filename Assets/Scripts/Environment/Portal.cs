using UnityEngine;

namespace RW.MonumentValley
{
    public class Portal : MonoBehaviour
    {
        [Header("Portal Destinations")]
        [Tooltip("The node the player will automatically walk to BEFORE teleporting.")]
        public Node preTeleportWalkTarget;

        [Tooltip("The exact node the player will instantly teleport to.")]
        public Node teleportTarget;
        
        [Tooltip("The node the player will automatically walk to AFTER teleporting.")]
        public Node postTeleportWalkTarget;

        [Header("Portal State")]
        [Tooltip("If false, the portal will ignore the player stepping on it.")]
        public bool isActive = true;

        public void DeactivatePortal()
        {
            isActive = false;
            Debug.Log($"[Portal] {gameObject.name} deactivated.");
        }

        public void EnablePortal()
        {
            isActive = true;
            Debug.Log($"[Portal] {gameObject.name} enabled.");
        }

        public void ActivatePortal()
        {
            if (!isActive) return;

            Debug.Log($"[Portal] ActivatePortal called on {gameObject.name}");
            PlayerController player = FindFirstObjectByType<PlayerController>();
            
            if (player != null)
            {
                player.PortalToNode(preTeleportWalkTarget, teleportTarget, postTeleportWalkTarget);
            }
            else
            {
                Debug.LogError("[Portal] Could not find PlayerController in the scene!");
            }
        }
    }
}
