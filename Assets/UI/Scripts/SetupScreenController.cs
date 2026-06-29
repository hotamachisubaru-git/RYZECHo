using UnityEngine;
using Color = UnityEngine.Color;
using UnityEngine.UI;
using TMPro;

namespace RYZECHo.UI
{
    /// <summary>
    /// セットアップ画面のコントローラー。
    /// エージェント選択、武器選択、賭けを行う画面。
    /// </summary>
    public class SetupScreenController : UIScreen
    {
        [Header("Setup Screen Elements")]
        [SerializeField] private GameObject agentSelectionPanel;
        [SerializeField] private GameObject agentList;
        [SerializeField] private GameObject agentItemPrefab;
        [SerializeField] private GameObject weaponSelectionPanel;
        [SerializeField] private GameObject weaponList;
        [SerializeField] private GameObject weaponItemPrefab;
        [SerializeField] private GameObject betPanel;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button backButton;

        [Header("Setup Data")]
        [SerializeField] private int selectedAgentIndex = 0;
        [SerializeField] private int selectedWeaponIndex = 0;
        [SerializeField] private int betAmount = 0;

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

            // UI要素の参照を取得
            if (agentSelectionPanel == null && ScreenRoot != null)
            {
                var panel = ScreenRoot.transform.Find("AgentSelectionPanel");
                if (panel != null)
                    agentSelectionPanel = panel.gameObject;
            }

            if (agentList == null && ScreenRoot != null)
            {
                var list = ScreenRoot.transform.Find("AgentSelectionPanel/AgentList");
                if (list != null)
                    agentList = list.gameObject;
            }

            if (weaponSelectionPanel == null && ScreenRoot != null)
            {
                var panel = ScreenRoot.transform.Find("WeaponSelectionPanel");
                if (panel != null)
                    weaponSelectionPanel = panel.gameObject;
            }

            if (weaponList == null && ScreenRoot != null)
            {
                var list = ScreenRoot.transform.Find("WeaponSelectionPanel/WeaponList");
                if (list != null)
                    weaponList = list.gameObject;
            }

            if (betPanel == null && ScreenRoot != null)
            {
                var panel = ScreenRoot.transform.Find("BetPanel");
                if (panel != null)
                    betPanel = panel.gameObject;
            }

            if (confirmButton == null && ScreenRoot != null)
            {
                var btn = ScreenRoot.transform.Find("ConfirmButton");
                if (btn != null)
                    confirmButton = btn.GetComponent<Button>();
            }

            if (backButton == null && ScreenRoot != null)
            {
                var btn = ScreenRoot.transform.Find("BackButton");
                if (btn != null)
                    backButton = btn.GetComponent<Button>();
            }

            // Agentリストの生成（サンプル）
            PopulateAgentList();
            PopulateWeaponList();

            // ボタンイベントを登録
            if (confirmButton != null)
                AddButtonListener(confirmButton, OnConfirm);
            if (backButton != null)
                AddButtonListener(backButton, OnBack);
        }

        public override void OnHide()
        {
            base.OnHide();
        }

        private void PopulateAgentList()
        {
            if (agentList == null) return;

            // サンプルエージェントリスト
            string[] agentNames = { "エージェントA", "エージェントB", "エージェントC", "エージェントD" };

            foreach (var name in agentNames)
            {
                if (agentItemPrefab != null)
                {
                    var item = Instantiate(agentItemPrefab, agentList.transform);
                    item.SetActive(true);

                    var text = item.GetComponentInChildren<TMP_Text>();
                    if (text != null && defaultFont != null)
                        text.font = defaultFont;
                }
            }
        }

        private void PopulateWeaponList()
        {
            if (weaponList == null) return;

            // サンプル武器リスト
            string[] weaponNames = { "武器A", "武器B", "武器C", "武器D", "武器E" };

            foreach (var name in weaponNames)
            {
                if (weaponItemPrefab != null)
                {
                    var item = Instantiate(weaponItemPrefab, weaponList.transform);
                    item.SetActive(true);

                    var text = item.GetComponentInChildren<TMP_Text>();
                    if (text != null && defaultFont != null)
                        text.font = defaultFont;
                }
            }
        }

        private void OnConfirm()
        {
            // セットアップ完了、ゲーム開始へ
            UIScreenManager.Instance?.ShowSetupScreen();
        }

        private void OnBack()
        {
            // 難易度選択画面に戻る
            UIScreenManager.Instance?.ShowScreen(UIScreenManager.ScreenType.DifficultySelect);
        }

        public override void OnStartGame() { }
        public override void OnOpenSettings() { }
        public override void OnExitGame() { }
        public override void OnRetry() { }

        /// <summary>
        /// 選択されたエージェントインデックスを取得
        /// </summary>
        public int GetSelectedAgentIndex() => selectedAgentIndex;

        /// <summary>
        /// 選択された武器インデックスを取得
        /// </summary>
        public int GetSelectedWeaponIndex() => selectedWeaponIndex;

        /// <summary>
        /// 賭け金額を取得
        /// </summary>
        public int GetBetAmount() => betAmount;
    }
}
