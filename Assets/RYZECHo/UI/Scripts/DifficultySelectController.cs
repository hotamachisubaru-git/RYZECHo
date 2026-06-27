using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RYZECHo.UI
{
    /// <summary>
    /// 難易度選択画面のコントローラー。
    /// </summary>
    public class DifficultySelectController : UIScreen
    {
        [Header("Difficulty Select Elements")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button easyButton;
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button backButton;

        private int _selectedDifficulty = 0;

        private void Awake()
        {
            Initialize();
        }

        public override void Initialize()
        {
            base.Initialize();
            _selectedDifficulty = 1; // デフォルトは通常
        }

        public override void OnShow()
        {
            base.OnShow();

            // UI要素の参照を取得
            if (titleText == null && ScreenRoot != null)
            {
                var titleObj = ScreenRoot.transform.Find("Title");
                if (titleObj != null)
                    titleText = titleObj.GetComponent<TMP_Text>();
            }

            if (easyButton == null && ScreenRoot != null)
            {
                var easyObj = ScreenRoot.transform.Find("EasyButton");
                if (easyObj != null)
                    easyButton = easyObj.GetComponent<Button>();
            }

            if (normalButton == null && ScreenRoot != null)
            {
                var normalObj = ScreenRoot.transform.Find("NormalButton");
                if (normalObj != null)
                    normalButton = normalObj.GetComponent<Button>();
            }

            if (hardButton == null && ScreenRoot != null)
            {
                var hardObj = ScreenRoot.transform.Find("HardButton");
                if (hardObj != null)
                    hardButton = hardObj.GetComponent<Button>();
            }

            if (confirmButton == null && ScreenRoot != null)
            {
                var confirmObj = ScreenRoot.transform.Find("ConfirmButton");
                if (confirmObj != null)
                    confirmButton = confirmObj.GetComponent<Button>();
            }

            if (backButton == null && ScreenRoot != null)
            {
                var backObj = ScreenRoot.transform.Find("BackButton");
                if (backObj != null)
                    backButton = backObj.GetComponent<Button>();
            }

            // 選択状態を更新
            UpdateDifficultySelection();

            // ボタンイベントを登録
            AddButtonListener(easyButton, () => SelectDifficulty(0));
            AddButtonListener(normalButton, () => SelectDifficulty(1));
            AddButtonListener(hardButton, () => SelectDifficulty(2));
            AddButtonListener(confirmButton, OnConfirm);
            AddButtonListener(backButton, OnBack);

            // デフォルトフォントを適用
            if (defaultFont != null && titleText != null)
            {
                titleText.font = defaultFont;
            }
        }

        public override void OnHide()
        {
            base.OnHide();
        }

        private void SelectDifficulty(int difficulty)
        {
            _selectedDifficulty = difficulty;
            UpdateDifficultySelection();
        }

        private void UpdateDifficultySelection()
        {
            // 選択中のボタンのスタイルを更新
            if (easyButton != null)
                easyButton.gameObject.SetActive(_selectedDifficulty == 0);

            if (normalButton != null)
                normalButton.gameObject.SetActive(_selectedDifficulty == 1);

            if (hardButton != null)
                hardButton.gameObject.SetActive(_selectedDifficulty == 2);
        }

        private void OnConfirm()
        {
            // 難易度を設定してゲーム開始
            UIScreenManager.Instance?.StartGame(_selectedDifficulty, 10);
        }

        private void OnBack()
        {
            // タイトル画面に戻る
            UIScreenManager.Instance?.ShowScreen(UIScreenManager.ScreenType.TitleScreen);
        }

        public override void OnStartGame() { }
        public override void OnOpenSettings() { }
        public override void OnExitGame() { }
        public override void OnRetry() { }

        /// <summary>
        /// 選択された難易度を取得
        /// </summary>
        public int GetSelectedDifficulty() => _selectedDifficulty;
    }
}
