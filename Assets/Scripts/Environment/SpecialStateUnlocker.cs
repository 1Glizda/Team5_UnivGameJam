using UnityEngine;

namespace RW.MonumentValley
{
    /// <summary>
    /// Attach this to a Node and hook it up to the Node's Game Event.
    /// When the player steps on the node, it unlocks their ability to Right-Click into the special state.
    /// </summary>
    public class SpecialStateUnlocker : MonoBehaviour
    {
        public void UnlockSpecialState()
        {
#if UNITY_2023_1_OR_NEWER
            PlayerController player = FindFirstObjectByType<PlayerController>();
#else
            PlayerController player = FindObjectOfType<PlayerController>();
#endif
            if (player != null)
            {
                player.specialStateUnlocked = true;
                Debug.Log("[SpecialStateUnlocker] Special State UNLOCKED! Player can now right-click.");
            }
        }
    }
}
