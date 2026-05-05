namespace RYZECHo.Prototype;

internal readonly record struct InputSnapshot(
    bool MoveUp,
    bool MoveLeft,
    bool MoveDown,
    bool MoveRight,
    bool AdjustBetLeft,
    bool AdjustBetRight,
    bool Confirm,
    bool Press1,
    bool Press2,
    bool Press3,
    bool Press4,
    bool Press5,
    bool PressQ,
    bool PressE,
    bool PressR,
    bool FireHeld,
    bool InteractHeld,
    Point MousePosition);
