using UnityEngine;
using System.Collections;

namespace RW.MonumentValley
{
    public class CatSquasher : MonoBehaviour
    {
        [Header("Squash Settings")]
        [Tooltip("Drag the child GameObject that holds your Cat's Animator/Mesh here.")]
        public Transform catModel; 
        
        public float squashSpeed = 0.2f;
        
        // This is the "Pancake" shape! 
        // Notice Y is tiny (flat), but X and Z are large (spread out).
        public Vector3 pancakeScale = new Vector3(1.8f, 0.05f, 1.8f);

        private Vector3 normalScale;
        private Coroutine squashRoutine;

        private void Start()
        {
            if (catModel != null)
            {
                normalScale = catModel.localScale;
            }
        }

        // Call this when entering a low section!
        public void Squash()
        {
            if (catModel == null) return;
            if (squashRoutine != null) StopCoroutine(squashRoutine);
            squashRoutine = StartCoroutine(ScaleRoutine(pancakeScale));
        }

        // Call this when exiting a low section!
        public void Unsquash()
        {
            if (catModel == null) return;
            if (squashRoutine != null) StopCoroutine(squashRoutine);
            squashRoutine = StartCoroutine(ScaleRoutine(normalScale));
        }

        private IEnumerator ScaleRoutine(Vector3 targetScale)
        {
            Vector3 startScale = catModel.localScale;
            float t = 0;
            
            while (t < squashSpeed)
            {
                t += Time.deltaTime;
                float percent = t / squashSpeed;
                
                // Smooth easing makes it look like a bouncy cartoon squash
                float ease = Mathf.SmoothStep(0, 1, percent);
                
                catModel.localScale = Vector3.Lerp(startScale, targetScale, ease);
                yield return null;
            }
            
            catModel.localScale = targetScale;
        }
    }
}
