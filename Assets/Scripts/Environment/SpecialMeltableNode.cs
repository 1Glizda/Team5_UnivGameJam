using UnityEngine;

namespace RW.MonumentValley
{
    /// <summary>
    /// Attach this to any Node that belongs to the group of "meltable" nodes.
    /// If the player steps on this node while in their Special State, it will automatically trigger the linked melter.
    /// </summary>
    public class SpecialMeltableNode : MonoBehaviour
    {
        [Tooltip("The melter to trigger when the player steps on this node in their Special State.")]
        public BlockMelter linkedMelter;
        
        private void Reset()
        {
            // Auto-fill the melter if it's on the same object or parent
            if (linkedMelter == null)
            {
                linkedMelter = GetComponentInChildren<BlockMelter>();
                if (linkedMelter == null) linkedMelter = GetComponentInParent<BlockMelter>();
            }
        }
    }
}
