using UnityEngine;

namespace RW.MonumentValley
{
    [RequireComponent(typeof(DialogueSequence))]
    public class DialogueNodeTrigger : MonoBehaviour
    {
        private PlayerController player;
        private DialogueSequence sequence;
        private bool hasTriggered = false;

        private void Awake()
        {
            sequence = GetComponent<DialogueSequence>();
        }

        public void TriggerDialogue()
        {
            if (hasTriggered) return;
            hasTriggered = true;

#if UNITY_2023_1_OR_NEWER
            player = FindFirstObjectByType<PlayerController>();
#else
            player = FindObjectOfType<PlayerController>();
#endif
            if (player != null)
            {
                // Freeze the player while they talk!
                player.EnableControls(false);
            }

            // Hook up what happens when the dialogue finishes
            sequence.onDialogueComplete.AddListener(OnDialogueFinished);
            
            // Start the conversation
            sequence.StartSequence();
        }

        private void OnDialogueFinished()
        {
            if (player != null)
            {
                // Unfreeze the player
                player.EnableControls(true);

                // Instantly unlock the special state mechanics!
                player.specialStateUnlocked = true;
                Debug.Log("[DialogueNodeTrigger] Dialogue finished. Right-Click Special State UNLOCKED!");
            }
        }
    }
}
