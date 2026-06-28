using System.Collections.Generic;
using UnityEngine;
using Color = UnityEngine.Color;
using UnityEngine.UI;
using RYZECHo.UI.ViewModels;

namespace RYZECHo.UI
{
    /// <summary>
    /// キーコンフィグタブのUI制御パネル
    /// KeyBindListPanel と KeyBindEditPanel に分割されたサブパネルを統合管理
    /// </summary>
    public class KeyConfigPanel : MonoBehaviour
    {
        [Header("UI Configuration")]
        public int rowHeight = 36;
        public int bodyFontSize = 13;
        public int headerFontSize = 16;
        public int keybindsPerPage = 6;

        [Header("Colors")]
        public Color accentColor = new Color(0.24f, 0.55f, 0.97f);
        public Color textNormalColor = new Color(0.9f, 0.94f, 0.97f);
        public Color textDimColor = new Color(0.6f, 0.65f, 0.7f);
        public Color highlightColor = new Color(0.3f, 0.55f, 0.95f);
        public Color selectedRowColor = new Color(0.15f, 0.25f, 0.4f, 0.3f);

        [Header("Content Area")]
        [SerializeField] private float _contentLeft;
        [SerializeField] private float _contentWidth;
        [SerializeField] private float _contentTop;

        private Text _sectionTitle;
        private Transform _panelTransform;

        // Sub-panels
        private KeyBindListPanel _listPanel;
        private KeyBindEditPanel _editPanel;

        #region Properties
        public int SelectedIndex => _listPanel?.SelectedIndex ?? -1;
        public List<KeyBindingViewModel.KeybindEntryViewModel> Keybinds => _listPanel?.Keybinds;
        public bool IsRecording => _listPanel?.IsRecording ?? false;
        #endregion

        public void Initialize(Transform parentTransform, float contentLeft, float contentWidth, float contentTop)
        {
            _panelTransform = parentTransform;
            _contentLeft = contentLeft;
            _contentWidth = contentWidth;
            _contentTop = contentTop;
            CreateUI();
        }

        private void CreateUI()
        {
            var startY = _contentTop - 16f;
            CreateSectionTitle("キーコンフィグ", startY);

            _listPanel = gameObject.AddComponent<KeyBindListPanel>();
            _listPanel.Initialize(_panelTransform, _contentLeft, _contentWidth, startY - 32f);

            _editPanel = gameObject.AddComponent<KeyBindEditPanel>();
            _editPanel.Initialize(_panelTransform, _contentLeft, _contentWidth, _contentTop);
        }

        private void CreateSectionTitle(string text, float yPos)
        {
            var go = new GameObject("SectionTitle", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(_panelTransform, false);
            var title = go.GetComponent<Text>();
            title.font = Font.CreateDynamicFontFromOSFont("Yu Gothic UI", headerFontSize);
            title.fontSize = headerFontSize;
            title.alignment = TextAnchor.MiddleLeft;
            title.color = accentColor;
            title.text = text;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1f);
            rect.anchoredPosition = new Vector2(_contentLeft, yPos);
            rect.sizeDelta = new Vector2(_contentWidth, 28);
            _sectionTitle = title;
        }

        public void NavigateSelected(int direction) => _listPanel?.NavigateSelected(direction);
        public void SetSelectedIndex(int index) => _listPanel?.SetSelectedIndex(index);
        public int GetMaxIndex() => _listPanel?.GetMaxIndex() ?? 0;
        public bool StartKeyRecording() => _listPanel?.StartKeyRecording() ?? false;
        public void CancelKeyRecording() => _listPanel?.CancelKeyRecording();
        public void CompleteKeyRecording(KeyCode newKey) => _listPanel?.CompleteKeyRecording(newKey);
        public void RefreshUI() => _listPanel?.RefreshUI();
    }
}
