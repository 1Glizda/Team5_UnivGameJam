using System.Collections;
using UnityEngine;

namespace RW.MonumentValley
{
    public class AcidPuddle : MonoBehaviour
    {
        public float fadeDuration = 2.0f;
        public float targetScaleMultiplier = 2.0f;
        
        [Header("Noise Settings")]
        public float noiseSpeed = 1.5f;
        public float noiseAmount = 0.05f;

        private Material puddleMat;
        private Vector3 baseScale;
        private bool hasFinishedFading = false;
        private float randomOffset;
        private int colorPropId = -1;

        private void Start()
        {
            randomOffset = Random.Range(0f, 100f);
            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
            {
                puddleMat = rend.material;
                if (puddleMat.HasProperty("_BaseColor")) colorPropId = Shader.PropertyToID("_BaseColor");
                else if (puddleMat.HasProperty("_Color")) colorPropId = Shader.PropertyToID("_Color");
                StartCoroutine(FadeInRoutine());
            }
        }

        private void Update()
        {
            if (!hasFinishedFading) return;
            
            float timeOffset = Time.time * noiseSpeed + randomOffset;
            float noiseX = Mathf.PerlinNoise(timeOffset, 0f) * 2f - 1f;
            float noiseZ = Mathf.PerlinNoise(0f, timeOffset) * 2f - 1f;
            
            transform.localScale = new Vector3(
                baseScale.x * (1f + noiseX * noiseAmount),
                baseScale.y,
                baseScale.z * (1f + noiseZ * noiseAmount)
            );
        }

        private IEnumerator FadeInRoutine()
        {
            float t = 0;
            Vector3 targetScale = transform.localScale;
            Vector3 initialScale = targetScale * 0.1f;
            transform.localScale = initialScale;

            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float percent = Mathf.Clamp01(t / fadeDuration);

                if (colorPropId != -1)
                {
                    Color c = puddleMat.GetColor(colorPropId);
                    c.a = percent;
                    puddleMat.SetColor(colorPropId, c);
                }

                float spreadEase = 1f - Mathf.Pow(1f - percent, 3f);
                transform.localScale = Vector3.Lerp(initialScale, targetScale, spreadEase);

                yield return null;
            }
            
            baseScale = targetScale;
            hasFinishedFading = true;
        }
    }
}
