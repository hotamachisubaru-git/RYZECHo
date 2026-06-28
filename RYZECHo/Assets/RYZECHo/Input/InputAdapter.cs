using UnityEngine;

namespace RYZECHo
{
    /// <summary>
    /// Unity Input Manager から InputSnapshot を生成するアダプター。
    /// MonoGame の InputSnapshot 構造を Unity の Input クラスにマッピングする。
    /// </summary>
    internal static class InputAdapter
    {
        /// <summary>
        /// 現在のフレームの入力状態をスナップショットとして取得する。
        /// </summary>
        public static InputSnapshot Capture()
        {
            return new InputSnapshot(
                MoveUp: Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow),
                MoveLeft: Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow),
                MoveDown: Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow),
                MoveRight: Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow),
                AdjustBetLeft: Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Z),
                AdjustBetRight: Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.C),
                Confirm: Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter),
                Press1: Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha7),
                Press2: Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Alpha8),
                Press3: Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Alpha9),
                Press4: Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Alpha0),
                Press5: Input.GetKeyDown(KeyCode.Alpha5),
                Press6: Input.GetKeyDown(KeyCode.Alpha6),
                PressQ: Input.GetKeyDown(KeyCode.Q),
                PressE: Input.GetKeyDown(KeyCode.E),
                PressR: Input.GetKeyDown(KeyCode.R),
                PressT: Input.GetKeyDown(KeyCode.T),
                FireHeld: Input.GetKey(KeyCode.Mouse0),
                InteractHeld: Input.GetKey(KeyCode.Mouse1),
                MousePosition: CaptureMousePoint()
            );
        }

        public static Point CaptureMousePoint()
        {
            var mousePosition = Input.mousePosition;
            return new Point(
                Mathf.RoundToInt(mousePosition.x),
                Screen.height - Mathf.RoundToInt(mousePosition.y));
        }

        /// <summary>
        /// フレーム間の入力変化（ダウン/アップイベント）を判定する。
        /// </summary>
        public static bool IsKeyDown(KeyCode key)
        {
            return Input.GetKeyDown(key);
        }

        /// <summary>
        /// キーが離されたかを判定する。
        /// </summary>
        public static bool IsKeyUp(KeyCode key)
        {
            return Input.GetKeyUp(key);
        }

        /// <summary>
        /// マウスボタンのクリック状態を取得する。
        /// </summary>
        public static bool IsMouseButtonDown(int button)
        {
            return Input.GetMouseButtonDown(button);
        }

        /// <summary>
        /// マウスボタンのホールド状態を取得する。
        /// </summary>
        public static bool IsMouseButtonHold(int button)
        {
            return Input.GetMouseButton(button);
        }

        /// <summary>
        /// マウスホイールの値を取得する（Y軸: スクロール量）。
        /// </summary>
        public static float GetMouseScrollDelta()
        {
            return Input.mouseScrollDelta.y;
        }
    }
}
