using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace RW.MonumentValley
{
    public class CreditsScroller : MonoBehaviour
    {
        public float scrollSpeed = 80f;
        public int fontSize = 28;
        public Color textColor = Color.white;

        private const string LoremIpsum =
            "GAME CREDITS\n\n\n" +
            "Scripting\n\n" +
            "Bârsan Patricia-Diana\n" +
            "Cristea Bogdan\n\n\n" +
            "3D Art\n\n" +
            "Roșca Adina Elena\n" +
            "Predescu Tudor\n" +
            "Mitru Bogdan-Alexandru\n\n\n" +
            "Storytelling\n\n" +
            "Sipos Diana-Livia\n\n\n" +
            "Sound Design\n\n" +
            "Matija Purkovic\n\n\n" +
            "- Thank you for playing -\n\n\n\n\n";

        private RectTransform scrollRect;
        private float canvasHeight;
        private float contentHeight;
        private bool finished;

        private void Start()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            var canvasGO = new GameObject("CreditsCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform, false);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImage = bgGO.AddComponent<Image>();
            bgImage.color = Color.black;

            canvasHeight = scaler.referenceResolution.y;

            var textGO = new GameObject("CreditsText");
            textGO.transform.SetParent(canvasGO.transform, false);
            scrollRect = textGO.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.1f, 1f);
            scrollRect.anchorMax = new Vector2(0.9f, 1f);
            scrollRect.pivot = new Vector2(0.5f, 1f);
            scrollRect.sizeDelta = new Vector2(0, 3000f);
            scrollRect.anchoredPosition = new Vector2(0, -800f);

            contentHeight = scrollRect.sizeDelta.y;

            var text = textGO.AddComponent<Text>();
            text.text = LoremIpsum;
            text.fontSize = fontSize;
            text.color = textColor;
            text.alignment = TextAnchor.UpperCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.lineSpacing = 1.2f;
        }

        private void Update()
        {
            if (finished || scrollRect == null) return;

            scrollRect.anchoredPosition += Vector2.up * (scrollSpeed * Time.deltaTime);

            if (Input.anyKeyDown)
            {
                LoadMainMenu();
                return;
            }

            // Return to menu once all text has scrolled off the top
            if (scrollRect.anchoredPosition.y > contentHeight)
            {
                LoadMainMenu();
            }
        }

        private void LoadMainMenu()
        {
            finished = true;
            SceneManager.LoadScene("MainMenu");
        }
    }
}
