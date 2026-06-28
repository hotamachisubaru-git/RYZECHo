using UnityEngine;
using Color = UnityEngine.Color;
using UnityEngine.UI;
using TMPro;

namespace RYZECHo.UI
{
    /// <summary>
    /// メインメニュー画面のコントローラー。
    /// </summary>
    public class MainMenuController : UIScreen
    {
        [Header("Main Menu Elements")]
        [SerializeField] private TMP_Text menuTitleText;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button loadGameButton;
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
            if (menuTitleText == null && ScreenRoot != null)
            {
                var titleObj = ScreenRoot.transform.Find("MenuTitle");
                if (titleObj != null)
                    menuTitleText = titleObj.GetComponent<TMP_Text>();
            }

            if (startGameButton == null && ScreenRoot != null)
            {
                var startObj = ScreenRoot.transform.Find("StartGameButton");
                if (startObj != null)
                    startGameButton = startObj.GetComponent<Button>();
            }

            if (loadGameButton == null && ScreenRoot != null)
            {
                var loadObj = ScreenRoot.transform.Find("LoadGameButton");
                if (loadObj != null)
                    loadGameButton = loadObj.GetComponent<Button>();
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
            AddButtonListener(startGameButton, OnStartGame);
            AddButtonListener(loadGameButton, OnOpenSettings);
            AddButtonListener(settingsButton, OnOpenSettings);
            AddButtonListener(exitButton, OnExitGame);

            // デフォルトフォントを適用
            if (defaultFont != null)
            {
                if (menuTitleText != null)
                    menuTitleText.font = defaultFont;
            }
        }

        public override void OnHide()
        {
            base.OnHide();
        }

        public override void OnStartGame()
        {
            // ゲーム開始処理
            UIScreenManager.Instance?.StartGame();
        }

        public override void OnOpenSettings()
        {
            // 設定画面を表示
            UIScreenManager.Instance?.ShowScreen(UIScreenManager.ScreenType.SetupScreen);
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
