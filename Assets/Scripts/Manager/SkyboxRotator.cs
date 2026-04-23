using UnityEngine;

namespace RW.MonumentValley
{
    public class SkyboxRotator : MonoBehaviour
    {
        [Tooltip("How fast the skybox rotates (degrees per second)")]
        public float rotationSpeed = 1.5f;
        
        private void Update()
        {
            // Make sure we actually have a skybox assigned in the Lighting settings!
            if (RenderSettings.skybox != null)
            {
                // The built-in 6-sided skybox shader has a property called "_Rotation"
                RenderSettings.skybox.SetFloat("_Rotation", Time.time * rotationSpeed);
            }
        }
    }
}
