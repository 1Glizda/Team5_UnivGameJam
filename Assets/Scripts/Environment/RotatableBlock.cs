using UnityEngine;
using System.Collections;
using RW.MonumentValley;

namespace RW.MonumentValley
{
    public class RotatableBlock : MonoBehaviour
    {
        [Header("Animation")]
        public float duration = 1.2f;
        public AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private bool isRotating = false;

        public void Rotate(Vector3 axis)
        {
            if (isRotating) return;
            StartCoroutine(RotateRoutine(axis));
        }

        private IEnumerator RotateRoutine(Vector3 axis)
        {
            isRotating = true;
            Quaternion startRotation = transform.localRotation;
            Quaternion endRotation = Quaternion.Euler(axis * 90f) * startRotation;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = rotationCurve.Evaluate(elapsed / duration);
                transform.localRotation = Quaternion.Slerp(startRotation, endRotation, t);
                yield return null;
            }

            transform.localRotation = endRotation;
            isRotating = false;

            // Rebuild the pathfinding graph after movement!
            Graph graph = FindFirstObjectByType<Graph>();
            if (graph != null) graph.RebuildGraph();
        }
    }
}
