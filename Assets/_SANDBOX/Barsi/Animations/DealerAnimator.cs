using UnityEngine;

public class DealerAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private DialogueComponent dialogue;

    public bool dialogueSpoken = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TogglePopup(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TogglePopup(false);
        }
    }

    public void TogglePopup(bool state)
    {
        if (animator != null)
        {
            animator?.SetBool("shouldPopup", state);
            if(dialogueSpoken == false)
            {
                DialogueSystem.Instance.HandleInteraction(dialogue);
                dialogueSpoken = true;
            }
           
        }
    }
}
