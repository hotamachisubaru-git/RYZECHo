#if RYZECHO_LEGACY_SYSTEM_DRAWING_RENDERER
using UnityEngine;
using System.Drawing;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using Color = System.Drawing.Color;
using Graphics = System.Drawing.Graphics;
using FontStyle = System.Drawing.FontStyle;
using GraphicsUnit = System.Drawing.GraphicsUnit;

namespace RYZECHo
{
    /// <summary>
    /// UnityシーンでGameModelの描画を管理するコンポーネント。
    /// InputAdapterから入力を受け取り、GameModel.Render()に渡す。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class GameRenderer : MonoBehaviour
    {
        [SerializeField] private Camera _renderCamera;
        [SerializeField] private float _pixelScale = 1f;
        [SerializeField] private GameplaySettingsSO _gameplaySettings;
        [SerializeField] private GameRulesSettingsSO _gameRulesSettings;
        [SerializeField] private LayoutSettingsSO _layoutSettings;

        private IGameModelFactory _factory;
        private IGameModel _gameModel = null!;
        private RenderTexture _renderTexture;
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            // DI: factory を常に初期化（テスト時はモック注入、本番時は GameModelFactory.Instance）
            _factory = GameModelFactory.Instance;

            // Use serialized settings if available, otherwise fall back to factory defaults
            if (_gameplaySettings != null || _gameRulesSettings != null || _layoutSettings != null)
            {
                _gameModel = _factory.Create(
                    gameRules: _gameRulesSettings,
                    layoutSettings: _layoutSettings,
                    gameplaySettings: _gameplaySettings);
            }
            else
            {
                _gameModel = _factory.Create();
            }
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            _renderCamera = _renderCamera ?? GetComponent<Camera>();

            // RenderTextureを作成
            var width = Screen.width;
            var height = Screen.height;
            _renderTexture = new RenderTexture(width, height, 0);
            _renderTexture.Create();

            // SpriteRendererにRenderTextureを割り当て
            var sprite = Sprite.Create(_renderTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
            _spriteRenderer.sprite = sprite;
        }

        private void Update()
        {
            // InputAdapterからスナップショットを取得
            var snapshot = InputAdapter.Capture();

            // GameModelの更新
            _gameModel.Update(Time.deltaTime, snapshot);

            // 描画
            Render();
        }

        private void Render()
        {
            using var bitmap = new System.Drawing.Bitmap(_renderTexture.width, _renderTexture.height);
            using var graphics = Graphics.FromImage(bitmap);

            // クライアント領域
            var clientBounds = new Rectangle(0, 0, _renderTexture.width, _renderTexture.height);

            // GameModel.Render()を呼び出し
            _gameModel.Render(graphics, clientBounds, Input.mousePosition.ToPoint());

            // RenderTextureに転送
            var pixels = bitmap.GetPixels();
            _renderTexture.SetPixels(pixels);
            _renderTexture.Apply();
        }

        private void OnDestroy()
        {
            _renderTexture?.Release();
            Destroy(_renderTexture);
        }
    }
}
#endif
