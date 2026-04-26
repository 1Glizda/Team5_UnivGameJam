using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterDialogue", menuName = "Character Dialogue")]
public class CharacterDialogue : ScriptableObject
{
    public string characterName;
    public Sprite characterPortrait;
    public string[] dialogueLines;
    public bool[] autoProgressLines;
    public bool[] endDialogueLines;
    public float autoProgressDelay = 1.5f;

    public float typingSpeed = 0.05f;
}