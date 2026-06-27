using UnityEngine;
using Color = UnityEngine.Color;
using System.Collections.Generic;
using System;

namespace RYZECHo.UI
{
    /// <summary>
    /// UI画面の遷移を管理するマネージャー。
    /// UIScreenを登録・切り替えし、ゲームのフェーズに応じた表示を行う。
    /// </summary>
    public class UIScreenManager : MonoBehaviour
    {
        [SerializeField] public GameObject[] screenPrefabs;

        private Dictionary<ScreenType, UIScreen> _screens = new();
        private ScreenType _currentScreen;
        private bool _isGameActive;
        private int _selectedDifficulty;
        private int _setupTurnCount;

        public enum ScreenType
        {
            TitleScreen,
            MainMenu,
            DifficultySelect,
            SetupScreen,
            GameOver
        }

        public ScreenType CurrentScreen => _currentScreen;
        public bool IsGameActive => _isGameActive;

        public static UIScreenManager Instance { get; private set; }

        // イベント: 画面遷移時に発火
        public event Action<ScreenType> OnScreenChanged;

        // イベント: ゲーム開始時に発火
        public event Action<int, int> OnGameStarted;

        // イベント: ゲーム終了時に発火（勝利/敗北フラグ, スコア）
        public event Action<bool, int, int> OnGameEnded;

        private void Awake()
        {
            Instance = this;
            InitializeScreens();
        }

        private void InitializeScreens()
        {
            foreach (var prefab in screenPrefabs)
            {
                if (prefab == null) continue;

                var instance = Instantiate(prefab, transform);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localScale = Vector3.one;

                var screen = instance.GetComponent<UIScreen>();
                if (screen != null)
                {
                    screen.Initialize();
                    var type = GetScreenType(prefab);
                    _screens[type] = screen;
                }
            }

            // 初期画面をタイトルスクリーンに
            ShowScreen(ScreenType.TitleScreen);
        }

        private ScreenType GetScreenType(GameObject prefab)
        {
            var name = prefab.name.Replace("UI", "").Replace("Screen", "").Replace("ScreenUI", "");
            return name switch
            {
                "Title" => ScreenType.TitleScreen,
                "Main" => ScreenType.MainMenu,
                "DifficultySelect" => ScreenType.DifficultySelect,
                "Setup" => ScreenType.SetupScreen,
                "GameOver" => ScreenType.GameOver,
                _ => ScreenType.TitleScreen
            };
        }

        /// <summary>
        /// 指定された画面に遷移する
        /// </summary>
        public void ShowScreen(ScreenType type)
        {
            if (_screens.TryGetValue(type, out var screen))
            {
                // 現在の画面を非表示
                if (_screens.TryGetValue(_currentScreen, out var currentScreen) && currentScreen != null)
                {
                    currentScreen.Hide();
                }

                screen.Show();
                _currentScreen = type;
                OnScreenChanged?.Invoke(type);
            }
        }

        /// <summary>
        /// ゲームを開始する（難易度・ターン数付き）
        /// </summary>
        public void StartGame(int difficulty = 0, int turnCount = 10)
        {
            _isGameActive = true;
            _selectedDifficulty = difficulty;
            _setupTurnCount = turnCount;
            OnGameStarted?.Invoke(difficulty, turnCount);
        }

        /// <summary>
        /// ゲームを終了する（結果付き）
        /// </summary>
        public void EndGame(bool isVictory, int score)
        {
            _isGameActive = false;
            OnGameEnded?.Invoke(isVictory, score, _setupTurnCount);
        }

        /// <summary>
        /// ゲームをリセット（タイトル画面に戻る）
        /// </summary>
        public void ResetGame()
        {
            _isGameActive = false;
            ShowScreen(ScreenType.TitleScreen);
        }

        /// <summary>
        /// 現在のゲーム結果を表示する
        /// </summary>
        public void ShowGameOver(bool isVictory, int score, int turnsPlayed)
        {
            if (_screens.TryGetValue(ScreenType.GameOver, out var screen) && screen is GameOverScreenController gameOverUI)
            {
                gameOverUI.SetResult(isVictory, score, turnsPlayed);
            }
            ShowScreen(ScreenType.GameOver);
        }

        /// <summary>
        /// 難易度選択画面に遷移
        /// </summary>
        public void ShowDifficultySelect()
        {
            ShowScreen(ScreenType.DifficultySelect);
        }

        /// <summary>
        /// セットアップ画面に遷移
        /// </summary>
        public void ShowSetupScreen()
        {
            ShowScreen(ScreenType.SetupScreen);
        }

        /// <summary>
        /// メインメニュー画面に遷移
        /// </summary>
        public void ShowMainMenu()
        {
            ShowScreen(ScreenType.MainMenu);
        }

        /// <summary>
        /// タイトル画面に遷移
        /// </summary>
        public void ShowTitleScreen()
        {
            ShowScreen(ScreenType.TitleScreen);
        }
    }
}
