using System.Collections;
using UnityEngine;

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance;
    private DialogueUIController ui;
    private DialogueComponent activeDialogue;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ui = DialogueUIController.Instance;
        activeDialogue = null;
    }

    public void HandleInteraction(DialogueComponent dialogue)
    {
        if (activeDialogue != null && activeDialogue != dialogue)
            return;

        if (dialogue.isDialogueActive)
            NextLine();
        else
            StartDialogue(dialogue);
    }

    public void StartDialogue(DialogueComponent dialogue)
    {
        if (activeDialogue != null)
            return;

        activeDialogue = dialogue;

        dialogue.isDialogueActive = true;
        dialogue.dialogueIndex = 0;

        ui.SetNPCInfo(
            dialogue.dialogueData.characterName,
            dialogue.dialogueData.characterPortrait
        );

        ui.ShowDialogueUI(true);
        DisplayCurrentLine();
    }

    void NextLine()
    {
        if (activeDialogue == null)
            return;

        var dialogue = activeDialogue;

        if (dialogue.isTyping)
        {
            StopAllCoroutines();
            ui.SetDialogueText(dialogue.dialogueData.dialogueLines[dialogue.dialogueIndex]);
            dialogue.isTyping = false;
            return;
        }

        if (dialogue.dialogueData.endDialogueLines.Length > dialogue.dialogueIndex &&
            dialogue.dialogueData.endDialogueLines[dialogue.dialogueIndex])
        {
            EndDialogue();
            return;
        }

        dialogue.dialogueIndex++;

        if (dialogue.dialogueIndex < dialogue.dialogueData.dialogueLines.Length)
            DisplayCurrentLine();
        else
            EndDialogue();
    }

    void DisplayCurrentLine()
    {
        if (activeDialogue == null)
            return;

        StopAllCoroutines();
        StartCoroutine(TypeLine());
    }

    IEnumerator TypeLine()
    {
        var dialogue = activeDialogue;

        dialogue.isTyping = true;
        ui.SetDialogueText("");

        string line = dialogue.dialogueData.dialogueLines[dialogue.dialogueIndex];

        foreach (char c in line)
        {
            ui.SetDialogueText(ui.dialogueText.text += c);
            yield return new WaitForSeconds(dialogue.dialogueData.typingSpeed);
        }

        dialogue.isTyping = false;

        if (dialogue.dialogueData.autoProgressLines.Length > dialogue.dialogueIndex &&
            dialogue.dialogueData.autoProgressLines[dialogue.dialogueIndex])
        {
            yield return new WaitForSeconds(dialogue.dialogueData.autoProgressDelay);
            NextLine();
        }
    }

    public void EndDialogue()
    {
        if (activeDialogue == null)
            return;

        StopAllCoroutines();

        activeDialogue.isDialogueActive = false;
        activeDialogue = null;

        ui.SetDialogueText("");
        ui.ShowDialogueUI(false);
    }

    public bool IsDialogueActive()
    {
        return activeDialogue != null;
    }
}