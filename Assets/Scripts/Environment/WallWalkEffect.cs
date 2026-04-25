using UnityEngine;

namespace RW.MonumentValley
{
    /// <summary>
    /// Attach this to Nodes to create a "Wall Walking Zone". 
    /// As requested, this effect will persist until the player leaves the zone or the timer runs out!
    /// </summary>
    public class WallWalkEffect : SpecialZoneEffect
    {
        protected override void OnApply(PlayerController player)
        {
            player.isWallWalking = true;
            Debug.Log("[WallWalkEffect] Player entered gravity zone. Wall-Walking ENABLED.");
        }

        protected override void OnRevert(PlayerController player)
        {
            player.isWallWalking = false;
            Debug.Log("[WallWalkEffect] Player left gravity zone. Wall-Walking DISABLED.");
        }
    }
}
