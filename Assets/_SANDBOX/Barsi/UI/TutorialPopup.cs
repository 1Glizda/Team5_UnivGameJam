using System.Collections;
using UnityEngine;

public class TutorialPopup : MonoBehaviour
{
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private float visibleTime = 4f;

    private bool hasShown = false;

    private void Start()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.OnDialogueEnded.AddListener(HandleDialogueEnded);
        }
    }

    private void HandleDialogueEnded()
    {
        if (hasShown) return;

        hasShown = true;

        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);

        StartCoroutine(AutoCloseTutorial());
    }

    private IEnumerator AutoCloseTutorial()
    {
        yield return new WaitForSeconds(visibleTime);

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }
}