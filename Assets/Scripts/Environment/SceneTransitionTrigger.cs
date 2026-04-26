using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace RW.MonumentValley
{
    public class SceneTransitionTrigger : MonoBehaviour
    {
        [Header("Transition Settings")]
        [SerializeField] private string sceneToLoad;
        [SerializeField] private float fadeTime = 1.5f;
        [SerializeField] private ScreenFader fader;

        [Header("Options")]
        [SerializeField] private bool disablePlayerOnTrigger = true;

        public void TransitionToScene()
        {
            StartCoroutine(TransitionRoutine());
        }

        private IEnumerator TransitionRoutine()
        {
            // Optional: Disable player movement so they don't walk off during fade
            if (disablePlayerOnTrigger)
            {
                PlayerController player = FindFirstObjectByType<PlayerController>();
                if (player != null) player.EnableControls(false);
            }

            if (fader != null)
            {
                fader.FadeOn(fadeTime);
                yield return new WaitForSeconds(fadeTime + 0.1f);
            }
            else
            {
                Debug.LogWarning("[SceneTransitionTrigger] No ScreenFader assigned! Transitioning immediately.");
            }
            
            if (!string.IsNullOrEmpty(sceneToLoad))
            {
                SceneManager.LoadScene(sceneToLoad);
            }
            else
            {
                Debug.LogError("[SceneTransitionTrigger] Scene name is empty!");
            }
        }
        
        // Convenience method for Node UnityEvents
        public void OnNodeReached()
        {
            TransitionToScene();
        }
    }
}
