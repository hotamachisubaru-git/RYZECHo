using UnityEngine;
using UnityEngine.UI;
using RYZECHo.UI.ViewModels;

namespace RYZECHo.UI
{
    /// <summary>
    /// 音声タブのUI制御パネル (マスター/音楽/効果音/ボイス/環境)
    /// AudioMasterPanel と AudioMusicPanel に分割されたサブパネルを統合管理
    /// </summary>
    public class AudioSettingsPanel : MonoBehaviour
    {
        [Header("UI Configuration")]
        public int sliderHeight = 24;
        public int bodyFontSize = 13;
        public int headerFontSize = 16;
        public Color accentColor = new Color(0.24f, 0.55f, 0.97f);
        public Color textNormalColor = new Color(0.9f, 0.94f, 0.97f);
        public Color textDimColor = new Color(0.6f, 0.65f, 0.7f);

        [Header("Content Area")]
        [SerializeField] private float _contentLeft;
        [SerializeField] private float _contentWidth;
        [SerializeField] private float _contentTop;

        private Text _sectionTitle;
        private Transform _panelTransform;
        private int _selectedIndex = 0;
        private const int MaxAudioSliders = 5;

        // Sub-panels
        private AudioMasterPanel _masterPanel;
        private AudioMusicPanel _musicPanel;

        #region Properties
        public float MasterVolume => _masterPanel?.MasterVolume ?? 1.0f;
        public float MusicVolume => _musicPanel?.MusicVolume ?? 1.0f;
        public float SfxVolume => _musicPanel?.SfxVolume ?? 1.0f;
        public float VoiceVolume => _musicPanel?.VoiceVolume ?? 1.0f;
        public float EnvVolume => _musicPanel?.EnvVolume ?? 0.8f;
        public int SelectedIndex { get; private set; }
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
            CreateSectionTitle("音声設定", startY);

            // Create master panel sub-component
            _masterPanel = gameObject.AddComponent<AudioMasterPanel>();
            _masterPanel.Initialize(_panelTransform, _contentLeft, _contentWidth, startY - 48f);

            // Create music/SFX panel sub-component
            _musicPanel = gameObject.AddComponent<AudioMusicPanel>();
            _musicPanel.Initialize(_panelTransform, _contentLeft, _contentWidth, startY - 48f);
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
            title.enableWordWrapping = false;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1f);
            rect.anchoredPosition = new Vector2(_contentLeft, yPos);
            rect.sizeDelta = new Vector2(_contentWidth, 28);
            _sectionTitle = title;
        }

        public void NavigateSelected(int direction)
        {
            SelectedIndex = Mathf.Clamp(SelectedIndex + direction, 0, MaxAudioSliders - 1);
        }

        public void SetSelectedIndex(int index)
        {
            SelectedIndex = Mathf.Clamp(index, 0, MaxAudioSliders - 1);
        }

        public int GetMaxIndex() => MaxAudioSliders - 1;
    }
}
