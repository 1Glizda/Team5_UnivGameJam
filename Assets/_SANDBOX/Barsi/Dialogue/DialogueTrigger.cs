using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] private DialogueComponent dialogue;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            DialogueSystem.Instance.HandleInteraction(dialogue);
        }
    }
}