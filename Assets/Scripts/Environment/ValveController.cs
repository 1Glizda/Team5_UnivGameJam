using UnityEngine;
using System.Collections;
using RW.MonumentValley;

namespace RW.MonumentValley
{
    [RequireComponent(typeof(Clickable))]
    public class ValveController : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private RotatableBlock[] targetBlocks;
        [SerializeField] private Vector3 rotationAxis = Vector3.right; // X or Y for the blocks

        [Header("Valve Animation")]
        [SerializeField] private Vector3 valveLocalAxis = Vector3.forward; // Usually Z for a valve
        [SerializeField] private float mainTurnDuration = 1.0f;
        [SerializeField] private float anticipationAngle = 20f;
        [SerializeField] private float anticipationDuration = 0.2f;

        private Clickable clickable;
        private Highlighter highlighter;
        private bool isAnimating = false;

        private void Awake()
        {
            clickable = GetComponent<Clickable>();
            highlighter = GetComponent<Highlighter>();
        }

        private void OnEnable()
        {
            if (clickable != null) clickable.clickAction += OnValveClicked;
        }

        private void OnDisable()
        {
            if (clickable != null) clickable.clickAction -= OnValveClicked;
        }

        private void OnValveClicked(Clickable c, Vector3 pos)
        {
            if (isAnimating) return;
            
            // 1. Rotate the L-shapes
            foreach (var block in targetBlocks)
            {
                if (block != null) block.Rotate(rotationAxis);
            }

            // 2. Animate the valve itself
            StartCoroutine(AnimateValve());
        }

        private IEnumerator AnimateValve()
        {
            isAnimating = true;
            Quaternion startRot = transform.localRotation;
            
            // --- Phase 1: Anticipation (Slight back-turn) ---
            Quaternion antRot = startRot * Quaternion.Euler(valveLocalAxis * -anticipationAngle);
            float elapsed = 0f;
            while (elapsed < anticipationDuration)
            {
                elapsed += Time.deltaTime;
                transform.localRotation = Quaternion.Slerp(startRot, antRot, elapsed / anticipationDuration);
                yield return null;
            }

            // --- Phase 2: The big 180 turn ---
            Quaternion targetRot = startRot * Quaternion.Euler(valveLocalAxis * 180f);
            elapsed = 0f;
            while (elapsed < mainTurnDuration)
            {
                elapsed += Time.deltaTime;
                // Use a fast-start curve for the main turn
                float t = Mathf.SmoothStep(0, 1, elapsed / mainTurnDuration);
                transform.localRotation = Quaternion.Slerp(antRot, targetRot, t);
                yield return null;
            }

            transform.localRotation = targetRot;
            isAnimating = false;
        }
    }
}
