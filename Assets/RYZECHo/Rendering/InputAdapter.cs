#if RYZECHO_LEGACY_SYSTEM_DRAWING_RENDERER
using UnityEngine;
using System.Drawing;
using Point = System.Drawing.Point;

namespace RYZECHo
{
    /// <summary>
    /// Unity の Input クラスから InputSnapshot を生成するアダプター。
    /// MonoGame の InputSnapshot と同じ構造を持つ。
    /// </summary>
    public static class InputAdapter
    {
        public static InputSnapshot Capture()
        {
            return new InputSnapshot(
                MoveUp: Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow),
                MoveLeft: Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow),
                MoveDown: Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow),
                MoveRight: Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow),
                AdjustBetLeft: Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Alpha0),
                AdjustBetRight: Input.GetKeyDown(KeyCode.Alpha5),
                Confirm: Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space),
                Press1: Input.GetKeyDown(KeyCode.Alpha1),
                Press2: Input.GetKeyDown(KeyCode.Alpha2),
                Press3: Input.GetKeyDown(KeyCode.Alpha3),
                Press4: Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Alpha0),
                Press5: Input.GetKeyDown(KeyCode.Alpha5),
                Press6: Input.GetKeyDown(KeyCode.Alpha6),
                PressQ: Input.GetKeyDown(KeyCode.Q),
                PressE: Input.GetKeyDown(KeyCode.E),
                PressR: Input.GetKeyDown(KeyCode.R),
                PressT: Input.GetKeyDown(KeyCode.T),
                FireHeld: Input.GetKey(KeyCode.Mouse0),
                InteractHeld: Input.GetKey(KeyCode.Mouse1),
                MousePosition: ToPoint(Input.mousePosition)
            );
        }

        private static Point ToPoint(Vector2 vector)
        {
            // Unity の座標系 (原点: 左下) から System.Drawing の座標系 (原点: 左上) へ変換
            return new Point((int)vector.x, Screen.height - (int)vector.y);
        }
    }
}
#endif
