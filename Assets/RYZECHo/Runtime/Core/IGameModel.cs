namespace RYZECHo;

/// <summary>
/// GameModelの純粋ドメイン層APIを定義するインターフェース。
/// Unityビュー（描画/入力/オーディオ）からの依存を逆転させる。
/// </summary>
internal interface IGameModel
{
    /// <summary>ゲームの更新（論理フレーム）</summary>
    void Update(float deltaSeconds, InputSnapshot input);

    /// <summary>描画用クライアント領域の取得</summary>
    Size LayoutSize { get; }

    /// <summary>現在のフェーズ</summary>
    GamePhase Phase { get; }

    /// <summary>プレイヤーの位置（モデル座標）</summary>
    PointF PlayerModelPosition { get; }

    /// <summary>プレイヤーの向き（ラジアン）</summary>
    float PlayerFacingRadians { get; }

    /// <summary>プレイヤーのFOV（度）</summary>
    float PlayerFovDegrees { get; }

    /// <summary>プレイヤーの視認範囲</summary>
    float PlayerVisionRange { get; }

    /// <summary>モデル座標をセル座標に変換</summary>
    PointF ModelToCellSpace(PointF modelPosition);

    /// <summary>ワールドのセルサイズ</summary>
    float ModelCellSize { get; }

    /// <summary>プレイヤーが生存中か</summary>
    bool PlayerIsAlive { get; }

    /// <summary>一時停止状態</summary>
    bool IsPaused { get; set; }

    /// <summary>ブリーフィング表示中か</summary>
    bool ShowBriefing { get; }

    /// <summary>オーディオキューの発行イベント</summary>
    event System.Action<RippleKind, PointF, float> AudioCueEmitted;
}
