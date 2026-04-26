using System.Collections;
using UnityEngine;

public class TutorialPopup : MonoBehaviour
{
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private float visibleTime = 4f;

    private bool hasShown = false;

    private float lastTapTime = 0f;
    private const float doubleTapThreshold = 0.3f;

    private void Start()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.OnDialogueEnded.AddListener(HandleDialogueEnded);
        }
    }

    private void OnDisable()
    {
        // Safety for WebGL!
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.OnDialogueEnded.RemoveListener(HandleDialogueEnded);
        }
    }

    private void Update()
    {
        if (tutorialPanel == null || !tutorialPanel.activeSelf) return;

        // Skip logic (Right Click or Double Tap)
        if (Input.GetMouseButtonDown(1) || IsDoubleTap())
        {
            CloseTutorial();
        }
    }

    private bool IsDoubleTap()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                bool isDouble = (Time.time - lastTapTime < doubleTapThreshold);
                lastTapTime = Time.time;
                return isDouble;
            }
        }
        return false;
    }

    private void HandleDialogueEnded()
    {
        if (hasShown) return;
        hasShown = true;
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
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
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
    }
}