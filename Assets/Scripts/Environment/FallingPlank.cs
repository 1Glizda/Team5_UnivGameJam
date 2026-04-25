using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace RW.MonumentValley
{
    /// <summary>
    /// Attach this to your Plank object. 
    /// Trigger "TriggerFall()" from a Unity Event (like clicking a button or stepping on a node).
    /// </summary>
    public class FallingPlank : MonoBehaviour
    {
        [Header("Animation Settings")]
        [Tooltip("The exact position the plank will land at.")]
        public Vector3 endPosition = new Vector3(-4.63999987f, -3.04999995f, -8.89000034f);
        
        [Tooltip("The exact rotation the plank will land at.")]
        public Vector3 endRotationEuler = new Vector3(270f, 180f, 0f);
        
        [Tooltip("How many seconds the fall takes.")]
        public float fallDuration = 1.0f;
        
        [Header("Events")]
        [Tooltip("Triggered the moment the plank finishes falling. Hook this up to a NodeConnectionToggler's Reconnect() function!")]
        public UnityEvent onPlankFallen;

        private bool hasFallen = false;

        public void TriggerFall()
        {
            if (!hasFallen)
            {
                hasFallen = true;
                StartCoroutine(FallRoutine());
            }
        }

        private IEnumerator FallRoutine()
        {
            // Use localPosition and localRotation so the values match what you type in the Inspector!
            Vector3 startPos = transform.localPosition;
            Quaternion startRot = transform.localRotation;
            Quaternion endRot = Quaternion.Euler(endRotationEuler);
            
            float t = 0;
            while (t < fallDuration)
            {
                t += Time.deltaTime;
                float percent = t / fallDuration;
                
                // Dramatic gravity fall (accelerates as it goes down instead of moving at a linear speed)
                float easeInSquare = percent * percent;
                
                transform.localPosition = Vector3.Lerp(startPos, endPosition, easeInSquare);
                transform.localRotation = Quaternion.Slerp(startRot, endRot, easeInSquare);
                
                yield return null;
            }
            
            // Snap to exact final coordinates before the wiggle
            transform.localPosition = endPosition;
            transform.localRotation = endRot;

            // Small bouncy wiggle effect
            float bounceDuration = 0.4f;
            float bounceT = 0;
            while (bounceT < bounceDuration)
            {
                bounceT += Time.deltaTime;
                float percent = bounceT / bounceDuration;
                
                // Damping sine wave for a wobble effect: sin(time) * fadeOut * maxAngle
                float angleX = Mathf.Sin(percent * Mathf.PI * 4f) * (1f - percent) * 15f; 
                
                // Apply the wobble to the final rotation
                transform.localRotation = endRot * Quaternion.Euler(angleX, 0, 0);
                
                yield return null;
            }

            // Snap completely flat
            transform.localPosition = endPosition;
            transform.localRotation = endRot;

            Debug.Log("[FallingPlank] Plank has landed and bounced!");

            // Fire the event to reconnect the pathfinding nodes!
            if (onPlankFallen != null)
            {
                onPlankFallen.Invoke();
            }
        }
    }
}
