using RYZECHo.Unity;
using UnityEngine;

namespace RYZECHo;

/// <summary>
/// Unity scene entry point for driving the simulation and Unity-native presentation.
/// </summary>
public sealed class GameController : MonoBehaviour
{
    private IGameModelFactory _factory;

    [Header("Scene References")]
    [SerializeField] private Camera gameCamera;
    [SerializeField] private Transform playerView;
    [SerializeField] private HuntFovRenderer huntFovRenderer;

    [Header("Settings")][SerializeField] private GameplaySettingsSO gameplaySettings;
    [SerializeField] private GameRulesSettingsSO gameRulesSettings;
    [SerializeField] private LayoutSettingsSO layoutSettings;

    [Header("World Mapping")]
    [SerializeField] private Vector2 worldOrigin = new(-9f, -6f);
    [SerializeField] private float cellSizeUnits = 1f;
    [SerializeField] private bool createPlayerViewIfMissing = true;

    [Header("Camera")]
    [SerializeField] private bool followPlayerCamera = true;
    [SerializeField] private Vector3 cameraOffset = new(0f, 0f, -10f);
    [SerializeField] private float cameraLerp = 18f;
    [SerializeField] private float orthographicSize = 6.5f;

    private GameModel _gameModel = null!;
    private Sprite? _generatedPlayerSprite;

    private void Awake()
    {
        // DI: factory を明示的に注入（テスト時はモック、本番時は GameModelFactory.Instance）
        _factory = GameModelFactory.Instance;

        _gameModel = (GameModel)_factory.Create(
            gameRulesSettings,
            layoutSettings,
            gameplaySettings);
        gameCamera ??= Camera.main;
        EnsurePlayerView();
        huntFovRenderer ??= GetComponentInChildren<HuntFovRenderer>();

        if (huntFovRenderer != null && playerView != null)
        {
            huntFovRenderer.SetTarget(playerView);
        }
    }

    private void Update()
    {
        var snapshot = InputAdapter.Capture();

        if (InputAdapter.IsKeyDown(KeyCode.Tab))
        {
            _gameModel.CycleBuildTool();
        }

        if (InputAdapter.IsKeyDown(KeyCode.B))
        {
            _gameModel.ToggleBriefing();
        }

        if (InputAdapter.IsMouseButtonDown(0))
        {
            _gameModel.HandleLeftClick(InputAdapter.CaptureMousePoint());
        }

        if (InputAdapter.IsMouseButtonDown(1))
        {
            _gameModel.HandleRightClick(InputAdapter.CaptureMousePoint());
        }

        _gameModel.Update(Time.deltaTime, snapshot);
        SyncPlayerView();
        SyncCamera();
    }

    private void OnDestroy()
    {
        if (_generatedPlayerSprite != null)
        {
            Destroy(_generatedPlayerSprite.texture);
            Destroy(_generatedPlayerSprite);
        }
    }

    private void EnsurePlayerView()
    {
        if (playerView != null || !createPlayerViewIfMissing)
        {
            return;
        }

        var playerObject = new GameObject("Player View");
        playerObject.transform.SetParent(transform, worldPositionStays: false);
        playerView = playerObject.transform;

        var spriteRenderer = playerObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreateDiscSprite();
        spriteRenderer.color = new UnityEngine.Color(0.48f, 0.9f, 1f, 1f);
        spriteRenderer.sortingOrder = 50;
    }

    private Sprite CreateDiscSprite()
    {
        const int textureSize = 64;
        var texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, mipChain: false)
        {
            name = "Generated Player Disc",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        var center = (textureSize - 1) * 0.5f;
        var radius = textureSize * 0.42f;
        var pixels = new UnityEngine.Color32[textureSize * textureSize];

        for (var y = 0; y < textureSize; y++)
        {
            for (var x = 0; x < textureSize; x++)
            {
                var dx = x - center;
                var dy = y - center;
                var distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                var alpha = Mathf.Clamp01(radius + 1.5f - distance);
                pixels[(y * textureSize) + x] = new UnityEngine.Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

        _generatedPlayerSprite = Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f), textureSize);
        return _generatedPlayerSprite;
    }

    private void SyncPlayerView()
    {
        if (playerView == null)
        {
            return;
        }

        playerView.position = ModelToUnityPosition(_gameModel.PlayerModelPosition);
        playerView.rotation = Quaternion.Euler(0f, 0f, -_gameModel.PlayerFacingRadians * Mathf.Rad2Deg);

        if (huntFovRenderer != null)
        {
            huntFovRenderer.SetTarget(playerView);
            huntFovRenderer.SetVision(_gameModel.PlayerFovDegrees, _gameModel.PlayerVisionRange / _gameModel.ModelCellSize * cellSizeUnits);
        }
    }

    private void SyncCamera()
    {
        if (!followPlayerCamera || gameCamera == null || playerView == null)
        {
            return;
        }

        gameCamera.orthographic = true;
        gameCamera.orthographicSize = orthographicSize;

        var target = playerView.position + cameraOffset;
        gameCamera.transform.position = Vector3.Lerp(
            gameCamera.transform.position,
            target,
            1f - Mathf.Exp(-cameraLerp * Time.deltaTime));
    }

    private Vector3 ModelToUnityPosition(PointF modelPosition)
    {
        var cellSpace = _gameModel.ModelToCellSpace(modelPosition);
        return new Vector3(
            worldOrigin.x + (cellSpace.X * cellSizeUnits),
            worldOrigin.y + (cellSpace.Y * cellSizeUnits),
            0f);
    }
}
