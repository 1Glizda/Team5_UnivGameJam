using UnityEngine;

public class DealerAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private DialogueComponent dialogue;

    private bool dialogueSpoken = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TogglePopup(true);

            if (!dialogueSpoken)
            {
                dialogueSpoken = true;
                DialogueSystem.Instance.HandleInteraction(dialogue);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TogglePopup(false);
        }
    }

    private void TogglePopup(bool state)
    {
        if (animator != null)
        {
            animator.SetBool("shouldPopup", state);
        }
    }
}