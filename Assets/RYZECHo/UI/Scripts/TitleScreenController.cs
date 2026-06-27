using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RYZECHo.UI
{
    /// <summary>
    /// タイトル画面のコントローラー。
    /// </summary>
    public class TitleScreenController : UIScreen
    {
        [Header("Title Screen Elements")]
        [SerializeField] private TMP_Text titleLogoText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;

        private void Awake()
        {
            Initialize();
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void OnShow()
        {
            base.OnShow();

            // UI要素の参照を取得（SerializeFieldがnullの場合のフォールバック）
            if (titleLogoText == null && ScreenRoot != null)
            {
                var logoObj = ScreenRoot.transform.Find("TitleLogo");
                if (logoObj != null)
                    titleLogoText = logoObj.GetComponent<TMP_Text>();
            }

            if (startButton == null && ScreenRoot != null)
            {
                var startObj = ScreenRoot.transform.Find("StartButton");
                if (startObj != null)
                    startButton = startObj.GetComponent<Button>();
            }

            if (settingsButton == null && ScreenRoot != null)
            {
                var settingsObj = ScreenRoot.transform.Find("SettingsButton");
                if (settingsObj != null)
                    settingsButton = settingsObj.GetComponent<Button>();
            }

            if (exitButton == null && ScreenRoot != null)
            {
                var exitObj = ScreenRoot.transform.Find("ExitButton");
                if (exitObj != null)
                    exitButton = exitObj.GetComponent<Button>();
            }

            // ボタンイベントを登録
            AddButtonListener(startButton, OnStartGame);
            AddButtonListener(settingsButton, OnOpenSettings);
            AddButtonListener(exitButton, OnExitGame);

            // デフォルトフォントを適用
            if (defaultFont != null)
            {
                if (titleLogoText != null)
                    titleLogoText.font = defaultFont;
            }
        }

        public override void OnHide()
        {
            base.OnHide();
        }

        public override void OnStartGame()
        {
            // 難易度選択画面を表示
            UIScreenManager.Instance?.ShowDifficultySelect();
        }

        public override void OnOpenSettings()
        {
            // 設定画面を表示（SettingsUIControllerに委譲）
            UIScreenManager.Instance?.ShowScreen(UIScreenManager.ScreenType.MainMenu);
        }

        public override void OnExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public override void OnRetry() { }
    }
}
