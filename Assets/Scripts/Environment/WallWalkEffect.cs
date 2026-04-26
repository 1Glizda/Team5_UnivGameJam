using UnityEngine;

namespace RW.MonumentValley
{
    public class WallWalkEffect : SpecialZoneEffect
    {
        protected override void OnApply(PlayerController player)
        {
            player.isWallWalking = true;
            Debug.Log("[WallWalkEffect] Wall-Walking ENABLED.");
        }

        protected override void OnRevert(PlayerController player)
        {
            player.isWallWalking = false;
            Debug.Log("[WallWalkEffect] Wall-Walking DISABLED.");
        }
    }
}
