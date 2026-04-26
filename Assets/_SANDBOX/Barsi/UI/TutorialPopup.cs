using System.Collections;
using UnityEngine;

public class TutorialPopup : MonoBehaviour
{
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private float visibleTime = 4f;

    private float lastTapTime = 0f;
    private const float doubleTapThreshold = 0.3f;

    private void Update()
    {
        if (tutorialPanel == null || !tutorialPanel.activeSelf) return;

        bool skipDetected = false;

        // Desktop: Right Click
        if (Input.GetMouseButtonDown(1))
        {
            skipDetected = true;
        }

        // Mobile: Double Tap
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                if (Time.time - lastTapTime < doubleTapThreshold)
                {
                    skipDetected = true;
                }
                lastTapTime = Time.time;
            }
        }

        if (skipDetected)
        {
            CloseTutorial();
        }
    }

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
        CloseTutorial();
    }

    private void CloseTutorial()
    {
        StopAllCoroutines();
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }
}