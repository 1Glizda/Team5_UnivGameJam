using UnityEngine;

namespace RW.MonumentValley
{
    /// <summary>
    /// Attach this to Nodes to create a "Pancake Zone". When the player is in the Special State here, they squash!
    /// </summary>
    public class PancakeEffect : SpecialZoneEffect
    {
        protected override void OnApply(PlayerController player)
        {
            CatSquasher squasher = player.GetComponent<CatSquasher>();
            if (squasher != null)
            {
                squasher.Squash();
            }
            else
            {
                Debug.LogWarning("[PancakeEffect] Player does not have a CatSquasher component!");
            }
        }

        protected override void OnRevert(PlayerController player)
        {
            CatSquasher squasher = player.GetComponent<CatSquasher>();
            if (squasher != null)
            {
                squasher.Unsquash();
            }
        }
    }
}
