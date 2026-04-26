using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace RW.MonumentValley
{
    [System.Serializable]
    public struct DialogueLine
    {
        [TextArea(2, 5)]
        public string text;
        [Tooltip("The character's Transform that the bubble should pop up over.")]
        public Transform speaker;
    }

    public class DialogueSequence : MonoBehaviour
    {
        [Tooltip("The DialogueBubble UI component in the scene.")]
        public DialogueBubble bubblePrefab;
        
        public List<DialogueLine> lines;
        public UnityEvent onDialogueComplete;

        private DialogueBubble activeBubble;
        private int currentLineIndex = 0;
        private bool isPlaying = false;

        private void Update()
        {
            if (!isPlaying) return;

            // Progress dialogue on Left Click or Spacebar
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                AdvanceDialogue();
            }
        }

        public void StartSequence()
        {
            if (lines == null || lines.Count == 0)
            {
                CompleteSequence();
                return;
            }

            // Create the bubble if it doesn't exist, otherwise find it
            if (activeBubble == null)
            {
#if UNITY_2023_1_OR_NEWER
                activeBubble = FindFirstObjectByType<DialogueBubble>();
#else
                activeBubble = FindObjectOfType<DialogueBubble>();
#endif
                
                // If there isn't one in the scene, instantiate the prefab
                if (activeBubble == null && bubblePrefab != null)
                {
                    activeBubble = Instantiate(bubblePrefab);
                }
            }

            if (activeBubble == null)
            {
                Debug.LogError("[DialogueSequence] No DialogueBubble found in scene or assigned as prefab!");
                CompleteSequence();
                return;
            }

            currentLineIndex = 0;
            isPlaying = true;
            PlayCurrentLine();
        }

        private void PlayCurrentLine()
        {
            if (currentLineIndex >= lines.Count)
            {
                CompleteSequence();
                return;
            }

            DialogueLine line = lines[currentLineIndex];
            Transform speaker = line.speaker != null ? line.speaker : Camera.main.transform;
            activeBubble.ShowMessage(line.text, speaker);
        }

        private void AdvanceDialogue()
        {
            if (activeBubble.IsTyping())
            {
                // Instantly complete the text if they click while it's typing
                activeBubble.CompleteTextInstantly();
            }
            else
            {
                // Move to the next line
                currentLineIndex++;
                PlayCurrentLine();
            }
        }

        private void CompleteSequence()
        {
            isPlaying = false;
            if (activeBubble != null) activeBubble.HideBubble();
            
            if (onDialogueComplete != null)
            {
                onDialogueComplete.Invoke();
            }
        }
    }
}
