using UnityEngine;

namespace RW.MonumentValley
{
    /// <summary>
    /// Base class for all "Special Zone" effects (like WallWalking or Pancake).
    /// </summary>
    public abstract class SpecialZoneEffect : MonoBehaviour
    {
        // Tracks if the effect is currently actively applied to the player
        protected bool isApplied = false;

        public void Apply(PlayerController player)
        {
            if (!isApplied)
            {
                isApplied = true;
                OnApply(player);
                Debug.Log($"[SpecialZoneEffect] Applied {this.GetType().Name}");
            }
        }

        public void Revert(PlayerController player)
        {
            if (isApplied)
            {
                isApplied = false;
                OnRevert(player);
                Debug.Log($"[SpecialZoneEffect] Reverted {this.GetType().Name}");
            }
        }

        // To be implemented by specific effects
        protected abstract void OnApply(PlayerController player);
        protected abstract void OnRevert(PlayerController player);
    }
}
