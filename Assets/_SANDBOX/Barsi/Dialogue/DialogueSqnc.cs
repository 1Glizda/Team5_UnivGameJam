using UnityEngine;
using RW.MonumentValley;
using UnityEngine.Events;

namespace RW.MonumentValley
{
    public class DialogueSqnc : MonoBehaviour
    {
        [Header("Dialogue Data")]
        [SerializeField] private DialogueComponent dialogue;
        
        [Header("Camera Control")]
        [SerializeField] private GameObject dialogueCamera; // The Cinemachine camera to activate
        
        [Header("Events")]
        public UnityEvent onSequenceStarted;
        public UnityEvent onSequenceEnded;

        private PlayerController player;

        private void Start()
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        public void TriggerSequence()
        {
            if (DialogueSystem.Instance.IsDialogueActive()) return;

            // 1. Disable Player Controls
            if (player != null)
            {
                player.EnableControls(false);
            }

            // 2. Activate Dialogue Camera
            if (dialogueCamera != null)
            {
                dialogueCamera.SetActive(true);
            }

            // 3. Listen for dialogue end
            DialogueSystem.Instance.OnDialogueEnded.AddListener(EndSequence);

            // 4. Start the dialogue
            DialogueSystem.Instance.StartDialogue(dialogue);
            
            onSequenceStarted?.Invoke();
        }

        private void EndSequence()
        {
            // Stop listening
            DialogueSystem.Instance.OnDialogueEnded.RemoveListener(EndSequence);

            // 1. Re-enable Player Controls
            if (player != null)
            {
                player.EnableControls(true);
            }

            // 2. Deactivate Dialogue Camera
            if (dialogueCamera != null)
            {
                dialogueCamera.SetActive(false);
            }

            onSequenceEnded?.Invoke();
        }

        // Shortcut for the Node's UnityEvent
        public void OnNodeReached()
        {
            TriggerSequence();
        }
    }
}
