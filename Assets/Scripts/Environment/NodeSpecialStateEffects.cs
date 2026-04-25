using UnityEngine;
using UnityEngine.Events;

namespace RW.MonumentValley
{
    /// <summary>
    /// Attach this to specific Nodes to trigger unique effects when the player is in the Special State while standing on them!
    /// </summary>
    public class NodeSpecialStateEffects : MonoBehaviour
    {
        [Tooltip("Triggered when the Special State is turned ON while standing here, or when walking onto this node while Special State is already ON.")]
        public UnityEvent onSpecialStateActive;

        [Tooltip("Triggered when the Special State is turned OFF while standing here, or when walking away from this node.")]
        public UnityEvent onSpecialStateInactive;

        public void TurnOn()
        {
            if (onSpecialStateActive != null) onSpecialStateActive.Invoke();
        }

        public void TurnOff()
        {
            if (onSpecialStateInactive != null) onSpecialStateInactive.Invoke();
        }
    }
}
