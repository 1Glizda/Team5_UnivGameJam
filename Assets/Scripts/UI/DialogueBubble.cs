using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RW.MonumentValley
{
    public class DialogueBubble : MonoBehaviour
    {
        [Tooltip("The Text component that will display the dialogue.")]
        public Text dialogueText;
        
        [Tooltip("Seconds to wait between typing each character.")]
        public float typeSpeed = 0.05f;
        
        [Tooltip("Duration of the bouncy pop-in animation.")]
        public float popDuration = 0.3f;
        
        [Tooltip("World-space offset to hover the bubble above the speaker's origin.")]
        public Vector3 offset = new Vector3(0, 2f, 0);

        private Transform currentSpeaker;
        private Coroutine typeRoutine;
        private Coroutine popRoutine;
        private bool isTyping = false;
        private string fullText = "";

        private void Awake()
        {
            if (dialogueText == null) dialogueText = GetComponentInChildren<Text>();
            transform.localScale = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (currentSpeaker != null && transform.localScale.sqrMagnitude > 0.01f)
            {
                // Smoothly follow the speaker
                transform.position = Vector3.Lerp(transform.position, currentSpeaker.position + offset, Time.deltaTime * 10f);
                
                // Always face the camera so the UI is readable
                if (Camera.main != null)
                {
                    transform.rotation = Camera.main.transform.rotation;
                }
            }
        }

        public void ShowMessage(string message, Transform speaker)
        {
            fullText = message;
            dialogueText.text = "";
            currentSpeaker = speaker;
            
            // Instantly snap to the speaker position on start so it doesn't fly across the map
            transform.position = speaker.position + offset;

            if (popRoutine != null) StopCoroutine(popRoutine);
            popRoutine = StartCoroutine(PopAnimation(true));

            if (typeRoutine != null) StopCoroutine(typeRoutine);
            typeRoutine = StartCoroutine(TypeText());
        }

        public void HideBubble()
        {
            if (popRoutine != null) StopCoroutine(popRoutine);
            popRoutine = StartCoroutine(PopAnimation(false));
            isTyping = false;
        }

        public bool IsTyping() => isTyping;

        public void CompleteTextInstantly()
        {
            if (typeRoutine != null) StopCoroutine(typeRoutine);
            dialogueText.text = fullText;
            isTyping = false;
        }

        private IEnumerator TypeText()
        {
            isTyping = true;
            for (int i = 0; i <= fullText.Length; i++)
            {
                dialogueText.text = fullText.Substring(0, i);
                yield return new WaitForSeconds(typeSpeed);
            }
            isTyping = false;
        }

        private IEnumerator PopAnimation(bool show)
        {
            float timer = 0f;
            Vector3 startScale = transform.localScale;
            Vector3 targetScale = show ? Vector3.one : Vector3.zero;

            while (timer < popDuration)
            {
                timer += Time.deltaTime;
                float p = timer / popDuration;
                
                // Math for a cute bouncy pop-in, or a fast squish-out
                float easedP = show ? 1f - Mathf.Pow(1f - p, 3f) : p * p;
                if (show) easedP += Mathf.Sin(p * Mathf.PI) * 0.15f; // Add bounce

                transform.localScale = Vector3.Lerp(startScale, targetScale, easedP);
                yield return null;
            }
            transform.localScale = targetScale;
        }
    }
}
