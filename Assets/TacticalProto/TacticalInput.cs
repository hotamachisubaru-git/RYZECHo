namespace RYZECHo.TacticalProto;

/// <summary>
/// 入力スナップショット（既存のInputSnapshotを参照した純粋C#版）
/// WASD移動、マウスクリック、Q/E/R/Tキー、数字キー対応
/// </summary>
public readonly record struct TacticalInput
{
    /// <summary>移動方向</summary>
    public readonly bool MoveUp;
    public readonly bool MoveDown;
    public readonly bool MoveLeft;
    public readonly bool MoveRight;

    /// <summary>サブキー</summary>
    public readonly bool PressQ;
    public readonly bool PressE;
    public readonly bool PressR;
    public readonly bool PressT;

    /// <summary>数字キー (1-6)</summary>
    public readonly bool Press1;
    public readonly bool Press2;
    public readonly bool Press3;
    public readonly bool Press4;
    public readonly bool Press5;
    public readonly bool Press6;

    /// <summary>戦闘入力</summary>
    public readonly bool FireHeld;
    public readonly bool InteractHeld;
    public readonly bool Confirm;
    public readonly bool AdjustBetLeft;
    public readonly bool AdjustBetRight;

    /// <summary>マウス位置（ワールド座標）</summary>
    public readonly System.Numerics.Vector2 MouseWorldPosition;

    public TacticalInput(
        bool moveUp, bool moveDown, bool moveLeft, bool moveRight,
        bool pressQ, bool pressE, bool pressR, bool pressT,
        bool press1, bool press2, bool press3, bool press4, bool press5, bool press6,
        bool fireHeld, bool interactHeld, bool confirm,
        bool adjustBetLeft, bool adjustBetRight,
        System.Numerics.Vector2 mouseWorldPosition)
    {
        MoveUp = moveUp; MoveDown = moveDown; MoveLeft = moveLeft; MoveRight = moveRight;
        PressQ = pressQ; PressE = pressE; PressR = pressR; PressT = pressT;
        Press1 = press1; Press2 = press2; Press3 = press3; Press4 = press4; Press5 = press5; Press6 = press6;
        FireHeld = fireHeld; InteractHeld = interactHeld; Confirm = confirm;
        AdjustBetLeft = adjustBetLeft; AdjustBetRight = adjustBetRight;
        MouseWorldPosition = mouseWorldPosition;
    }
}
