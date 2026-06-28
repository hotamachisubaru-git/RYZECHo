using UnityEngine;
using Color = UnityEngine.Color;
using UnityEngine.UI;
using TMPro;

namespace RYZECHo
{
    /// <summary>
    /// ポーズオーバーレイのUI作成・レンダリングを管理するクラス。
    /// PauseOverlayControllerからUIロジックを分離。
    /// </summary>
    public class PauseOverlayUI : MonoBehaviour
    {
        #region UI References

        private GameObject _overlay;
        private Image _backdropImage;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _subtitleText;
        private Button _resumeButton;
        private Button _settingsButton;
        private Button _quitButton;
        private TextMeshProUGUI _phaseInfoText;
        private TextMeshProUGUI _scoreText;
        private TextMeshProUGUI _creditsText;

        // Button rects for hover detection
        private Rect _resumeButtonRect;
        private Rect _settingsButtonRect;
        private Rect _quitButtonRect;

        #endregion

        #region Configuration

        [Header("Pause Overlay Configuration")]
        [Range(0f, 1f)] [Tooltip("オーバーレイの透過度（0〜1）")] public float overlayAlpha = 0.7f;
        [Tooltip("タイトルフォントサイズ")] public int titleFontSize = 42;
        [Tooltip("サブタイトルフォントサイズ")] public int subtitleFontSize = 12;
        [Tooltip("ボタン幅")] public float buttonWidth = 200f;
        [Tooltip("ボタン高さ")] public float buttonHeight = 44f;
        [Tooltip("ボタンの角丸半径")] public float buttonBorderRadius = 8f;
        [Tooltip("スコア表示フォントサイズ")] public int scoreFontSize = 11;

        #endregion

        #region Colors

        private static readonly Color BackdropColor = new(0.016f, 0.031f, 0.055f, 0.7f);
        private static readonly Color TitleColor = new(0.902f, 0.941f, 0.973f, 1f);
        private static readonly Color SubtitleColor = new(0.706f, 0.784f, 0.824f, 0.706f);
        private static readonly Color ButtonNormalColor = new(0.118f, 0.275f, 0.51f, 1f);
        private static readonly Color ButtonHoverColor = new(0.235f, 0.549f, 0.969f, 1f);
        private static readonly Color ButtonBorderColor = new(0.314f, 0.549f, 0.784f, 1f);
        private static readonly Color ButtonBorderHoverColor = new(0.549f, 0.784f, 1f, 1f);
        private static readonly Color PhaseInfoColor = new(0.706f, 0.784f, 0.839f, 0.627f);
        private static readonly Color CreditsColor = new(0.933f, 0.792f, 0.439f, 0.627f);
        private static readonly Color QuitButtonNormalColor = new(0.51f, 0.118f, 0.118f, 1f);
        private static readonly Color QuitButtonHoverColor = new(0.784f, 0.18f, 0.18f, 1f);

        #endregion

        #region Unity Lifecycle

        public void Initialize()
        {
            CreateOverlay();
            gameObject.SetActive(false);
        }

        private void Update()
        {
            // Mouse hover updates are handled externally via UpdateMouseOver()
        }

        #endregion

        #region Overlay Creation

        private void CreateOverlay()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[PauseOverlay] No Canvas found in scene. Creating one.");
                var canvasGO = new GameObject("RYZECHoCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var c = canvasGO.GetComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
                canvas = c;
            }

            _overlay = new GameObject("PauseOverlay", typeof(RectTransform), typeof(Image));
            _overlay.transform.SetParent(canvas.transform, false);
            _overlay.transform.SetAsLastSibling();

            var overlayRect = _overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            _backdropImage = _overlay.GetComponent<Image>();
            _backdropImage.color = BackdropColor;
            _backdropImage.raycastTarget = false;

            var screenWidth = Screen.width;
            var screenHeight = Screen.height;
            var centerX = screenWidth / 2f;
            var centerY = screenHeight / 2f;

            // Title text
            _titleText = CreateTextMeshPro("PauseTitle", "一時停止", titleFontSize, TextAnchor.MiddleCenter, TitleColor);
            _titleText.rectTransform.SetParent(_overlay.transform, false);
            _titleText.rectTransform.anchorMin = Vector2.up;
            _titleText.rectTransform.anchorMax = Vector2.up;
            _titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
            _titleText.rectTransform.anchoredPosition = new Vector2(centerX - 180, centerY - 160);
            _titleText.rectTransform.sizeDelta = new Vector2(360, 56);

            // Subtitle text
            _subtitleText = CreateTextMeshPro("PauseSubtitle", "ESC で再開 / 下のボタンでも再開できます", subtitleFontSize, TextAnchor.MiddleCenter, SubtitleColor);
            _subtitleText.rectTransform.SetParent(_overlay.transform, false);
            _subtitleText.rectTransform.anchorMin = Vector2.up;
            _subtitleText.rectTransform.anchorMax = Vector2.up;
            _subtitleText.rectTransform.pivot = new Vector2(0.5f, 1f);
            _subtitleText.rectTransform.anchoredPosition = new Vector2(centerX - 200, centerY - 100);
            _subtitleText.rectTransform.sizeDelta = new Vector2(400, 24);

            // Resume button
            var resumeY = centerY - 30f;
            _resumeButton = CreatePauseButton("ResumeButton", "再開 (ESC)", buttonWidth, buttonHeight, ButtonNormalColor, ButtonBorderColor, resumeY);
            _resumeButtonRect = new Rect(centerX - buttonWidth / 2f, resumeY, buttonWidth, buttonHeight);

            // Settings button
            var settingsY = resumeY - buttonHeight - 16f;
            _settingsButton = CreatePauseButton("SettingsButton", "設定", buttonWidth, buttonHeight, ButtonNormalColor, ButtonBorderColor, settingsY);
            _settingsButtonRect = new Rect(centerX - buttonWidth / 2f, settingsY, buttonWidth, buttonHeight);

            // Quit button
            var quitY = settingsY - buttonHeight - 16f;
            _quitButton = CreatePauseButton("QuitButton", "ゲームを終了", buttonWidth, buttonHeight, QuitButtonNormalColor, ButtonBorderColor, quitY);
            _quitButtonRect = new Rect(centerX - buttonWidth / 2f, quitY, buttonWidth, buttonHeight);

            // Phase info text
            var phaseInfoY = centerY + 40f;
            _phaseInfoText = CreateTextMeshPro("PausePhaseInfo", "", scoreFontSize, TextAnchor.MiddleCenter, PhaseInfoColor);
            _phaseInfoText.rectTransform.SetParent(_overlay.transform, false);
            _phaseInfoText.rectTransform.anchorMin = new Vector2(0.5f, 0);
            _phaseInfoText.rectTransform.anchorMax = new Vector2(0.5f, 0);
            _phaseInfoText.rectTransform.pivot = new Vector2(0.5f, 0f);
            _phaseInfoText.rectTransform.anchoredPosition = new Vector2(0, phaseInfoY);
            _phaseInfoText.rectTransform.sizeDelta = new Vector2(440, 22);

            // Score text
            _scoreText = CreateTextMeshPro("PauseScore", "", scoreFontSize, TextAnchor.MiddleCenter, PhaseInfoColor);
            _scoreText.rectTransform.SetParent(_overlay.transform, false);
            _scoreText.rectTransform.anchorMin = new Vector2(0.5f, 0);
            _scoreText.rectTransform.anchorMax = new Vector2(0.5f, 0);
            _scoreText.rectTransform.pivot = new Vector2(0.5f, 0f);
            _scoreText.rectTransform.anchoredPosition = new Vector2(0, phaseInfoY + 26f);
            _scoreText.rectTransform.sizeDelta = new Vector2(400, 22);

            // Credits text
            _creditsText = CreateTextMeshPro("PauseCredits", "", scoreFontSize, TextAnchor.MiddleCenter, CreditsColor);
            _creditsText.rectTransform.SetParent(_overlay.transform, false);
            _creditsText.rectTransform.anchorMin = new Vector2(0.5f, 0);
            _creditsText.rectTransform.anchorMax = new Vector2(0.5f, 0);
            _creditsText.rectTransform.pivot = new Vector2(0.5f, 0f);
            _creditsText.rectTransform.anchoredPosition = new Vector2(0, phaseInfoY + 52f);
            _creditsText.rectTransform.sizeDelta = new Vector2(200, 22);
        }

        private TextMeshProUGUI CreateTextMeshPro(string name, string text, int fontSize, TextAnchor alignment, Color color)
        {
            var go = new GameObject(name, typeof(TextMeshProUGUI));
            var textComponent = go.GetComponent<TextMeshProUGUI>();
            textComponent.fontSize = fontSize;
            textComponent.alignment = ToTextAlignment(alignment);
            textComponent.color = color;
            textComponent.text = text;
            textComponent.textWrappingMode = TextWrappingModes.NoWrap;
            return textComponent;
        }

        private static TextAlignmentOptions ToTextAlignment(TextAnchor alignment)
        {
            return alignment switch
            {
                TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
                TextAnchor.UpperCenter => TextAlignmentOptions.Top,
                TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
                TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
                TextAnchor.MiddleRight => TextAlignmentOptions.Right,
                TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
                TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
                TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
                _ => TextAlignmentOptions.Center,
            };
        }

        private Button CreatePauseButton(string name, string label, float width, float height, Color normalColor, Color borderColor, float yPos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var button = go.GetComponent<Button>();
            var image = go.GetComponent<Image>();
            image.color = normalColor;
            image.raycastTarget = true;

            button.colors = new ColorBlock
            {
                normalColor = normalColor,
                highlightedColor = label == "ゲームを終了" ? QuitButtonHoverColor : ButtonHoverColor,
                pressedColor = label == "ゲームを終了" ? new Color(0.65f, 0.1f, 0.1f, 1f) : ButtonBorderColor,
                disabledColor = normalColor,
                fadeDuration = 0.1f,
            };

            // Border
            var borderGO = new GameObject("Border", typeof(RectTransform), typeof(Image));
            borderGO.transform.SetParent(go.transform, false);
            borderGO.transform.SetAsFirstSibling();
            var borderImage = borderGO.GetComponent<Image>();
            borderImage.color = borderColor;
            borderImage.raycastTarget = false;
            var borderRect = borderGO.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = new Vector2(2, 2);

            // Label text
            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(go.transform, false);
            var labelText = labelGO.GetComponent<TextMeshProUGUI>();
            labelText.fontSize = 14;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = new Color(0.941f, 0.961f, 0.98f, 1f);
            labelText.text = label;
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.sizeDelta = new Vector2(width - 16, height - 4);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.up;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(Screen.width / 2f - 100, yPos);
            rect.sizeDelta = new Vector2(width, height);

            return button;
        }

        #endregion

        #region Public Display Methods

        public void Show()
        {
            if (_overlay != null) _overlay.SetActive(true);
        }

        public void Hide()
        {
            if (_overlay != null) _overlay.SetActive(false);
        }

        public bool IsActive => _overlay != null && _overlay.activeInHierarchy;

        public void UpdateInfo(PauseInfoViewModel info)
        {
            if (_phaseInfoText != null)
            {
                _phaseInfoText.text = $"第{info.CurrentRound}ラウンド  |  状態: {info.PhaseLabel}";
            }
            if (_scoreText != null)
            {
                _scoreText.text = $"SCORE {info.PlayerScore} - {info.EnemyScore}";
            }
            if (_creditsText != null)
            {
                _creditsText.text = $"所持金: {info.Credits} 円";
            }
        }

        public void SetButtonClickHandlers(System.Action onResume, System.Action onSettings, System.Action onQuit)
        {
            if (_resumeButton != null) _resumeButton.onClick.AddListener(() => onResume?.Invoke());
            if (_settingsButton != null) _settingsButton.onClick.AddListener(() => onSettings?.Invoke());
            if (_quitButton != null) _quitButton.onClick.AddListener(() => onQuit?.Invoke());
        }

        #endregion

        #region Hover Detection

        public void UpdateMouseOver(Vector2 mousePos)
        {
            // Returns hover states for the controller to use
            _resumeButtonRect.Contains(mousePos);
            _settingsButtonRect.Contains(mousePos);
            _quitButtonRect.Contains(mousePos);
        }

        public void UpdateButtonHover(Button button, bool isHovered, Color normalColor, Color hoverColor, Color borderNormal, Color borderHover)
        {
            if (button == null) return;
            var image = button.GetComponent<Image>();
            if (image == null) return;
            image.color = isHovered ? hoverColor : normalColor;

            var borderGO = button.transform.Find("Border");
            if (borderGO != null)
            {
                var borderImage = borderGO.GetComponent<Image>();
                if (borderImage != null)
                {
                    borderImage.color = isHovered ? borderHover : borderNormal;
                }
            }
        }

        public Rect GetResumeButtonRect() => _resumeButtonRect;
        public Rect GetSettingsButtonRect() => _settingsButtonRect;
        public Rect GetQuitButtonRect() => _quitButtonRect;

        #endregion
    }
}
