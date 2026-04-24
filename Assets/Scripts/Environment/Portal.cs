using UnityEngine;

namespace RW.MonumentValley
{
    public class Portal : MonoBehaviour
    {
        [Header("Portal Destinations")]
        [Tooltip("The exact node the player will instantly teleport to.")]
        public Node teleportTarget;
        
        [Tooltip("The node the player will automatically walk to after teleporting (e.g. to walk 'into' the new room).")]
        public Node walkTarget;

        public void ActivatePortal()
        {
            Debug.Log($"[Portal] ActivatePortal called on {gameObject.name}");
            PlayerController player = FindFirstObjectByType<PlayerController>();
            
            if (player != null)
            {
                player.PortalToNode(teleportTarget, walkTarget);
            }
            else
            {
                Debug.LogError("[Portal] Could not find PlayerController in the scene!");
            }
        }
    }
}
