using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RYZECHo
{
    /// <summary>
    /// ポーズ画面用コントローラー（再開、設定、終了）。
    /// UI作成はPauseOverlayUIに、状態データはPauseInfoViewModelに分離済み。
    /// </summary>
    public sealed class PauseOverlayController : MonoBehaviour
    {
        [SerializeField] private GameModel _gameModel;

        private PauseOverlayUI _pauseUI;

        // Mouse hover states
        private bool _mouseOverResume = false;
        private bool _mouseOverSettings = false;
        private bool _mouseOverQuit = false;

        private void Awake()
        {
            _pauseUI = gameObject.AddComponent<PauseOverlayUI>();
            _pauseUI.Initialize();
        }

        private void Update()
        {
            UpdateMouseOver();
            HandlePauseInput();
        }

        #region Public API

        /// <summary>
        /// ポーズ画面を表示する。
        /// </summary>
        public void ShowPauseOverlay()
        {
            if (_gameModel != null)
            {
                _gameModel.IsPaused = true;
                var info = PauseInfoViewModel.FromGameModel(_gameModel);
                _pauseUI.UpdateInfo(info);
            }
            _pauseUI.Show();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>
        /// ポーズ画面を非表示にする。
        /// </summary>
        public void HidePauseOverlay()
        {
            _pauseUI.Hide();
            if (_gameModel != null) _gameModel.IsPaused = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>
        /// ゲームを再開する。
        /// </summary>
        public void ResumeGame()
        {
            HidePauseOverlay();
        }

        /// <summary>
        /// 設定画面を開く。
        /// </summary>
        public void OpenSettings()
        {
            var settingsController = FindObjectOfType<SettingsUIController>();
            if (settingsController != null)
            {
                settingsController.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[PauseOverlay] SettingsUIController not found in scene.");
            }
        }

        /// <summary>
        /// ゲームを終了する。
        /// </summary>
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Mouse Tracking

        private void UpdateMouseOver()
        {
            if (!_pauseUI.IsActive) return;

            var mousePos = Input.mousePosition;
            _mouseOverResume = _pauseUI.GetResumeButtonRect().Contains(mousePos);
            _mouseOverSettings = _pauseUI.GetSettingsButtonRect().Contains(mousePos);
            _mouseOverQuit = _pauseUI.GetQuitButtonRect().Contains(mousePos);

            // Update button hover colors
            _pauseUI.UpdateButtonHover(
                FindResumeButton(), _mouseOverResume,
                new Color(0.118f, 0.275f, 0.51f, 1f), new Color(0.235f, 0.549f, 0.969f, 1f),
                new Color(0.314f, 0.549f, 0.784f, 1f), new Color(0.549f, 0.784f, 1f, 1f));

            _pauseUI.UpdateButtonHover(
                FindSettingsButton(), _mouseOverSettings,
                new Color(0.118f, 0.275f, 0.51f, 1f), new Color(0.235f, 0.549f, 0.969f, 1f),
                new Color(0.314f, 0.549f, 0.784f, 1f), new Color(0.549f, 0.784f, 1f, 1f));

            _pauseUI.UpdateButtonHover(
                FindQuitButton(), _mouseOverQuit,
                new Color(0.51f, 0.118f, 0.118f, 1f), new Color(0.784f, 0.18f, 0.18f, 1f),
                new Color(0.314f, 0.549f, 0.784f, 1f), new Color(0.549f, 0.784f, 1f, 1f));
        }

        private Button FindResumeButton()
        {
            var children = new System.Collections.Generic.List<Transform>();
            GetComponentsInChildren<Transform>(children);
            foreach (var t in children)
            {
                if (t.name == "ResumeButton" && t.TryGetComponent(out Button b)) return b;
            }
            return null;
        }

        private Button FindSettingsButton()
        {
            var children = new System.Collections.Generic.List<Transform>();
            GetComponentsInChildren<Transform>(children);
            foreach (var t in children)
            {
                if (t.name == "SettingsButton" && t.TryGetComponent(out Button b)) return b;
            }
            return null;
        }

        private Button FindQuitButton()
        {
            var children = new System.Collections.Generic.List<Transform>();
            GetComponentsInChildren<Transform>(children);
            foreach (var t in children)
            {
                if (t.name == "QuitButton" && t.TryGetComponent(out Button b)) return b;
            }
            return null;
        }

        #endregion

        #region Pause Input

        private void HandlePauseInput()
        {
            if (!_pauseUI.IsActive) return;

            // ESC to resume
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ResumeGame();
                return;
            }

            // Mouse click on buttons
            if (Input.GetMouseButtonDown(0))
            {
                var mousePos = Input.mousePosition;
                if (_pauseUI.GetResumeButtonRect().Contains(mousePos)) ResumeGame();
                else if (_pauseUI.GetSettingsButtonRect().Contains(mousePos)) OpenSettings();
                else if (_pauseUI.GetQuitButtonRect().Contains(mousePos)) QuitGame();
            }
        }

        #endregion
    }
}
