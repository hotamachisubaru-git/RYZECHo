using UnityEngine;
using UnityEngine.UI;

namespace RYZECHo.UI
{
    /// <summary>
    /// キーバインド編集ダイアログを管理するサブパネル
    /// </summary>
    public class KeyBindEditPanel : MonoBehaviour
    {
        [Header("UI Configuration")]
        public int rowHeight = 36;
        public int bodyFontSize = 13;

        [Header("Colors")]
        public Color accentColor = new Color(0.24f, 0.55f, 0.97f);
        public Color textNormalColor = new Color(0.9f, 0.94f, 0.97f);
        public Color textDimColor = new Color(0.6f, 0.65f, 0.7f);

        [Header("Content Area")]
        [SerializeField] private float _contentLeft;
        [SerializeField] private float _contentWidth;
        [SerializeField] private float _contentTop;

        private Text _instructions;
        private Transform _panelTransform;

        public void Initialize(Transform parentTransform, float contentLeft, float contentWidth, float contentTop)
        {
            _panelTransform = parentTransform;
            _contentLeft = contentLeft;
            _contentWidth = contentWidth;
            _contentTop = contentTop;
            CreateInstructions();
        }

        private void CreateInstructions()
        {
            var instrY = _contentTop - 240f;
            _instructions = CreateText("KeybindInstructions", bodyFontSize - 1, TextAnchor.MiddleLeft, textDimColor);
            _instructions.text = "選択中の行でEnterを押してキー入力を開始 / Escでキャンセル";
            var instrRect = _instructions.rectTransform;
            instrRect.SetParent(_panelTransform, false);
            instrRect.anchorMin = Vector2.zero;
            instrRect.anchorMax = Vector2.zero;
            instrRect.anchoredPosition = new Vector2(_contentLeft + 16, instrY);
            instrRect.sizeDelta = new Vector2(_contentWidth, 20);
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
            text.enableWordWrapping = false;
            return text;
        }

        private string FormatKeyCode(KeyCode keyCode) => keyCode.ToString();
    }
}
