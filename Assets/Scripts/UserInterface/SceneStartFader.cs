using UnityEngine;
using System.Collections;

namespace RW.MonumentValley
{
    public class SceneStartFader : MonoBehaviour
    {
        [SerializeField] private float fadeTime = 1.5f;
        [SerializeField] private float delay = 0.2f;
        private ScreenFader fader;

        private void Start()
        {
            fader = GetComponent<ScreenFader>();
            if (fader != null)
            {
                StartCoroutine(FadeRoutine());
            }
        }

        private IEnumerator FadeRoutine()
        {
            // Ensure the fader starts fully black/on
            fader.FadeOn(0.01f);
            
            yield return new WaitForSeconds(delay);
            
            // Fade out the black screen to reveal the level
            fader.FadeOff(fadeTime);
        }
    }
}
