using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance;
    [SerializeField] private DialogueUIController ui;
    private DialogueComponent activeDialogue;

    public UnityEvent OnDialogueEnded;
    public UnityEvent OnDialogueStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Fallback: If not assigned in Inspector, try to find it
        if (ui == null) ui = FindAnyObjectByType<DialogueUIController>();
        activeDialogue = null;
    }

    private void Update()
    {
        // If dialogue is active, listen for left-clicks to progress
        if (activeDialogue != null && Input.GetMouseButtonDown(0))
        {
            NextLine();
        }
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

        if (ui != null)
        {
            ui.SetNPCInfo(
                dialogue.dialogueData.characterName,
                dialogue.dialogueData.characterPortrait
            );
            ui.ShowDialogueUI(true);
        }
        DisplayCurrentLine();

        OnDialogueStarted?.Invoke();
    }

    void NextLine()
    {
        if (activeDialogue == null)
            return;

        var dialogue = activeDialogue;

        if (dialogue.isTyping)
        {
            // Autocomplete the line!
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

        OnDialogueEnded?.Invoke();
    }

    public bool IsDialogueActive()
    {
        return activeDialogue != null;
    }
}