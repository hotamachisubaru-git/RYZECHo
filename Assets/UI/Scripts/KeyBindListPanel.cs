using System;
using System.Collections.Generic;
using UnityEngine;
using Color = UnityEngine.Color;
using UnityEngine.UI;
using RYZECHo.UI.ViewModels;

namespace RYZECHo.UI
{
    /// <summary>
    /// キーバインドリストを表示するサブパネル
    /// </summary>
    public class KeyBindListPanel : MonoBehaviour
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
        private GameObject _highlightBox;
        private Transform _panelTransform;
        private KeyBindingViewModel _viewModel;

        public int SelectedIndex => _viewModel?.SelectedIndex ?? -1;
        public List<KeyBindingViewModel.KeybindEntryViewModel> Keybinds => _viewModel?.Keybinds as List<KeyBindingViewModel.KeybindEntryViewModel>;
        public bool IsRecording => _viewModel?.IsRecording ?? false;

        private void Awake()
        {
            _viewModel = new KeyBindingViewModel();
            BindViewModelEvents();
        }

        private void OnDestroy()
        {
            UnbindViewModelEvents();
            _viewModel?.Save();
            _viewModel?.Dispose();
        }

        private void BindViewModelEvents()
        {
            if (_viewModel == null) return;
            _viewModel.OnSelectedIndexChanged += OnViewModelSelectedIndexChanged;
            _viewModel.OnKeybindChanged += OnViewModelKeybindChanged;
        }

        private void UnbindViewModelEvents()
        {
            if (_viewModel == null) return;
            _viewModel.OnSelectedIndexChanged -= OnViewModelSelectedIndexChanged;
            _viewModel.OnKeybindChanged -= OnViewModelKeybindChanged;
        }

        private void OnViewModelSelectedIndexChanged(int index) => RefreshUI();
        private void OnViewModelKeybindChanged(string actionKey) => RefreshUI();

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
            var listY = startY - 32f;
            var visibleCount = Mathf.Min(keybindsPerPage, _viewModel.Count);

            for (int i = 0; i < visibleCount; i++)
            {
                var idx = i;
                if (idx >= _viewModel.Count) break;
                var entry = _viewModel.Keybinds[idx];
                var yPos = listY - i * rowHeight;
                var label = CreateText($"Keybind_{entry.ActionKey}_Label", bodyFontSize, TextAnchor.MiddleLeft, textNormalColor);
                label.text = entry.Label;
                var labelRect = label.rectTransform;
                labelRect.SetParent(_panelTransform, false);
                labelRect.anchorMin = new Vector2(0, 1);
                labelRect.anchorMax = new Vector2(0, 1);
                labelRect.pivot = new Vector2(0, 1f);
                labelRect.anchoredPosition = new Vector2(_contentLeft + 16, yPos);
                labelRect.sizeDelta = new Vector2(_contentWidth * 0.5f, rowHeight);

                var keyText = CreateText($"Keybind_{entry.ActionKey}_Key", bodyFontSize, TextAnchor.MiddleCenter,
                    entry.IsRecording ? highlightColor : textDimColor);
                keyText.text = entry.DisplayKey;
                var keyRect = keyText.rectTransform;
                keyRect.SetParent(_panelTransform, false);
                keyRect.anchorMin = new Vector2(1, 1);
                keyRect.anchorMax = new Vector2(1, 1);
                keyRect.pivot = new Vector2(1, 1f);
                keyRect.anchoredPosition = new Vector2(-_contentLeft - _contentWidth + 24, yPos);
                keyRect.sizeDelta = new Vector2(120, rowHeight);

                if (idx == _viewModel.SelectedIndex)
                    AddHighlightBox(_contentLeft + 8, yPos - rowHeight, _contentWidth - 16, rowHeight, selectedRowColor);
            }
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

        private Text CreateText(string name, int fontSize, TextAnchor alignment, Color color)
        {
            var go = new GameObject(name, typeof(Text));
            var text = go.GetComponent<Text>();
            text.font = Font.CreateDynamicFontFromOSFont("Yu Gothic UI", fontSize);
            if (text.font == null) text.font = Font.CreateDynamicFontFromOSFont("Yu Gothic UI", fontSize);
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }

        private void AddHighlightBox(float x, float y, float w, float h, Color color)
        {
            var go = new GameObject("Highlight", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_panelTransform, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
            _highlightBox = go;
        }

        public void RefreshUI()
        {
            if (_viewModel == null) return;
            if (_highlightBox != null) { GameObject.Destroy(_highlightBox); _highlightBox = null; }

            var startY = _contentTop - 48f;
            var listY = startY - 16f;
            var visibleCount = Mathf.Min(keybindsPerPage, _viewModel.Count);

            for (int i = 0; i < visibleCount; i++)
            {
                var idx = i;
                if (idx >= _viewModel.Count) break;
                var entry = _viewModel.Keybinds[idx];
                var yPos = listY - i * rowHeight;

                var oldLabel = _panelTransform.Find($"Keybind_{entry.ActionKey}_Label");
                var oldKey = _panelTransform.Find($"Keybind_{entry.ActionKey}_Key");
                if (oldLabel != null) GameObject.Destroy(oldLabel.gameObject);
                if (oldKey != null) GameObject.Destroy(oldKey.gameObject);

                var label = CreateText($"Keybind_{entry.ActionKey}_Label", bodyFontSize, TextAnchor.MiddleLeft, textNormalColor);
                label.text = entry.Label;
                var labelRect = label.rectTransform;
                labelRect.SetParent(_panelTransform, false);
                labelRect.anchorMin = new Vector2(0, 1);
                labelRect.anchorMax = new Vector2(0, 1);
                labelRect.pivot = new Vector2(0, 1f);
                labelRect.anchoredPosition = new Vector2(_contentLeft + 16, yPos);
                labelRect.sizeDelta = new Vector2(_contentWidth * 0.5f, rowHeight);

                var keyText = CreateText($"Keybind_{entry.ActionKey}_Key", bodyFontSize, TextAnchor.MiddleCenter,
                    entry.IsRecording ? highlightColor : textDimColor);
                keyText.text = entry.DisplayKey;
                var keyRect = keyText.rectTransform;
                keyRect.SetParent(_panelTransform, false);
                keyRect.anchorMin = new Vector2(1, 1);
                keyRect.anchorMax = new Vector2(1, 1);
                keyRect.pivot = new Vector2(1, 1f);
                keyRect.anchoredPosition = new Vector2(-_contentLeft - _contentWidth + 24, yPos);
                keyRect.sizeDelta = new Vector2(120, rowHeight);

                if (idx == _viewModel.SelectedIndex)
                    AddHighlightBox(_contentLeft + 8, yPos - rowHeight, _contentWidth - 16, rowHeight, selectedRowColor);
            }
        }

        public void NavigateSelected(int direction) => _viewModel?.NavigateSelected(direction);
        public void SetSelectedIndex(int index) => _viewModel?.SetSelectedIndex(index);
        public int GetMaxIndex() => _viewModel?.Count - 1 ?? 0;
        public bool StartKeyRecording() => _viewModel?.StartKeyRecording() ?? false;
        public void CancelKeyRecording() => _viewModel?.CancelKeyRecording();
        public void CompleteKeyRecording(KeyCode newKey) => _viewModel?.CompleteKeyRecording(newKey);
    }
}
