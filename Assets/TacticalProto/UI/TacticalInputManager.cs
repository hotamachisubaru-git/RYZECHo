using UnityEngine;
using System;

namespace RYZECHo.TacticalProto.UI
{
    /// <summary>
    /// 蜈･蜉帷ｮ｡逅・ｼ医く繝ｼ繝懊・繝峨・繝槭え繧ｹ蜈･蜉帙ｒ GameState 縺ｫ貂｡縺呻ｼ・    /// </summary>
    public class TacticalInputManager : MonoBehaviour
    {
        // ====================
        // 繧､繝吶Φ繝亥ｮ夂ｾｩ
        // ====================

        /// <summary>遘ｻ蜍墓婿蜷代′螟画峩縺輔ｌ縺・/summary>
        public event Action<Vector2> OnMove;

        /// <summary>辣ｧ貅紋ｽ咲ｽｮ縺悟､画峩縺輔ｌ縺・/summary>
        public event Action<Vector2> OnAimChanged;

        /// <summary>辣ｧ貅紋ｽ咲ｽｮ縺檎｢ｺ螳壹＠縺・/summary>
        public event Action<Vector2> OnAimConfirmed;

        /// <summary>謾ｻ謦・・繧ｿ繝ｳ縺梧款縺輔ｌ縺・/summary>
        public event Action OnAttack;

        /// <summary>謾ｻ謦・・繧ｿ繝ｳ縺碁屬縺輔ｌ縺・/summary>
        public event Action OnAttackReleased;

        /// <summary>繧ｹ繧ｭ繝ｫ1縺御ｽｿ逕ｨ縺輔ｌ縺・/summary>
        public event Action OnSkillOne;

        /// <summary>繧ｹ繧ｭ繝ｫ2縺御ｽｿ逕ｨ縺輔ｌ縺・/summary>
        public event Action OnSkillTwo;

        /// <summary>遨ｶ讌ｵ謚縺御ｽｿ逕ｨ縺輔ｌ縺・/summary>
        public event Action OnUltimate;

        /// <summary>繝ｪ繝ｭ繝ｼ繝峨′髢句ｧ九＆繧後◆</summary>
        public event Action OnReload;

        /// <summary>繝輔ぉ繝ｼ繧ｺ遒ｺ螳壹・繧ｿ繝ｳ縺梧款縺輔ｌ縺・/summary>
        public event Action OnPhaseConfirm;

        /// <summary>繝輔ぉ繝ｼ繧ｺ蜿匁ｶ医・繧ｿ繝ｳ縺梧款縺輔ｌ縺・/summary>
        public event Action OnPhaseCancel;

        /// <summary>繧ｫ繝｡繝ｩ繧ｺ繝ｼ繝繧､繝ｳ</summary>
        public event Action OnZoomIn;

        /// <summary>繧ｫ繝｡繝ｩ繧ｺ繝ｼ繝繧｢繧ｦ繝・/summary>
        public event Action OnZoomOut;

        /// <summary>繧ｫ繝｡繝ｩ繝代Φ・域婿蜷托ｼ・/summary>
        public event Action<Vector2> OnCameraPan;

        /// <summary>繧ｿ繝ｼ繝ｳ邨ゆｺ・/summary>
        public event Action OnTurnEnd;

        /// <summary>繝槭ャ繝苓｡ｨ遉ｺ蛻・崛</summary>
        public event Action OnToggleMinimap;

        // ====================
        // 蜈･蜉帷憾諷・        // ====================

        private Vector2 moveInput;
        private Vector2 aimInput;
        private bool attackDown;
        private bool reloadDown;
        private bool skillOneDown;
        private bool skillTwoDown;
        private bool ultimateDown;
        private bool phaseConfirmDown;
        private bool phaseCancelDown;
        private bool turnEndDown;
        private bool toggleMinimapDown;
        private bool zoomInDown;
        private bool zoomOutDown;

        // 蜈･蜉帙く繝ｼ險ｭ螳・        [Header("蜈･蜉帙く繝ｼ險ｭ螳・)]
        [SerializeField] private KeyCode moveForward = KeyCode.W;
        [SerializeField] private KeyCode moveBackward = KeyCode.S;
        [SerializeField] private KeyCode moveLeft = KeyCode.A;
        [SerializeField] private KeyCode moveRight = KeyCode.D;
        [SerializeField] private KeyCode attackKey = KeyCode.Mouse0;
        [SerializeField] private KeyCode reloadKey = KeyCode.R;
        [SerializeField] private KeyCode skillOneKey = KeyCode.Q;
        [SerializeField] private KeyCode skillTwoKey = KeyCode.E;
        [SerializeField] private KeyCode ultimateKey = KeyCode.F;
        [SerializeField] private KeyCode confirmKey = KeyCode.Return;
        [SerializeField] private KeyCode cancelKey = KeyCode.Escape;
        [SerializeField] private KeyCode turnEndKey = KeyCode.Space;
        [SerializeField] private KeyCode minimapKey = KeyCode.M;

        // ====================
        // 繧ｷ繝ｳ繧ｰ繝ｫ繝医Φ
        // ====================

        private static TacticalInputManager instance;
        public static TacticalInputManager Instance
        {
            get => instance;
            private set => instance = value;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        // ====================
        // 繝｡繧､繝ｳ繝ｫ繝ｼ繝・        // ====================

        private void Update()
        {
            ReadInput();
            FireEvents();
        }

        // ====================
        // 蜈･蜉幄ｪｭ縺ｿ蜿悶ｊ
        // ====================

        /// <summary>入力状態を読み込む</summary>
        private void ReadInput()
        {
            // WASD移動
            float h = 0f;
            float v = 0f;

            if (Input.GetKey(moveForward)) v += 1f;
            if (Input.GetKey(moveBackward)) v -= 1f;
            if (Input.GetKey(moveLeft)) h -= 1f;
            if (Input.GetKey(moveRight)) h += 1f;

            moveInput = new Vector2(h, v).normalized;

            // マウス位置
            var mousePos = Input.mousePosition;
            aimInput = ScreenToNormalizedScreen(mousePos);

            // ボタン入力
            attackDown = Input.GetKeyDown(attackKey);
            reloadDown = Input.GetKeyDown(reloadKey);
            skillOneDown = Input.GetKeyDown(skillOneKey);
            skillTwoDown = Input.GetKeyDown(skillTwoKey);
            ultimateDown = Input.GetKeyDown(ultimateKey);
            phaseConfirmDown = Input.GetKeyDown(confirmKey);
            phaseCancelDown = Input.GetKeyDown(cancelKey);
            turnEndDown = Input.GetKeyDown(turnEndKey);
            toggleMinimapDown = Input.GetKeyDown(minimapKey);

            // マウススクロール
            float scroll = Input.mouseScrollDelta.y;
            if (scroll > 0.1f) zoomInDown = true;
            if (scroll < -0.1f) zoomOutDown = true;

            // カメラパン
            if (Input.GetMouseButton(1))
            {
                OnCameraPan?.Invoke(new Vector2(Input.GetAxisRaw("Mouse X") * 0.05f, Input.GetAxisRaw("Mouse Y") * 0.05f));
            }
        }

        /// <summary>逕ｻ髱｢蠎ｧ讓吶ｒ豁｣隕丞喧・・1縲・・・/summary>
        private Vector2 ScreenToNormalizedScreen(Vector3 screenPos)
        {
            float x = (screenPos.x / Screen.width) * 2f - 1f;
            float y = (screenPos.y / Screen.height) * 2f - 1f;
            return new Vector2(x, y);
        }

        // ====================
        // 繧､繝吶Φ繝育匱轣ｫ
        // ====================

        /// <summary>繧､繝吶Φ繝医ｒ逋ｺ轣ｫ縺励※迥ｶ諷九ｒ繧ｯ繝ｪ繧｢</summary>
        private void FireEvents()
        {
            // 遘ｻ蜍・            if (moveInput != Vector2.zero)
            {
                OnMove?.Invoke(moveInput);
            }

            // 辣ｧ貅・            OnAimChanged?.Invoke(aimInput);

            // 謾ｻ謦・            if (attackDown)
            {
                OnAttack?.Invoke();
            }

            // 繝ｪ繝ｭ繝ｼ繝・            if (reloadDown)
            {
                OnReload?.Invoke();
            }

            // 繧ｹ繧ｭ繝ｫ
            if (skillOneDown) OnSkillOne?.Invoke();
            if (skillTwoDown) OnSkillTwo?.Invoke();
            if (ultimateDown) OnUltimate?.Invoke();

            // 繝輔ぉ繝ｼ繧ｺ謫堺ｽ・            if (phaseConfirmDown) OnPhaseConfirm?.Invoke();
            if (phaseCancelDown) OnPhaseCancel?.Invoke();

            // 繧ｿ繝ｼ繝ｳ
            if (turnEndDown) OnTurnEnd?.Invoke();

            // 繝槭ャ繝・            if (toggleMinimapDown) OnToggleMinimap?.Invoke();

            // 繧ｺ繝ｼ繝
            if (zoomInDown) OnZoomIn?.Invoke();
            if (zoomOutDown) OnZoomOut?.Invoke();

            // 辣ｧ貅也｢ｺ螳夲ｼ医・繧ｦ繧ｹ繧ｯ繝ｪ繝・け譎ゑｼ・            if (attackDown)
            {
                OnAimConfirmed?.Invoke(aimInput);
                OnAttackReleased?.Invoke();
            }
        }

        // ====================
        // 蜈･蜉帷憾諷句叙蠕暦ｼ亥､夜Κ縺九ｉ蜿ら・逕ｨ・・        // ====================

        /// <summary>迴ｾ蝨ｨ縺ｮ遘ｻ蜍募・蜉帙ｒ蜿門ｾ・/summary>
        public Vector2 GetMoveInput() => moveInput;

        /// <summary>迴ｾ蝨ｨ縺ｮ辣ｧ貅門・蜉帙ｒ蜿門ｾ・/summary>
        public Vector2 GetAimInput() => aimInput;

        /// <summary>遘ｻ蜍募・蜉帙・X謌仙・繧貞叙蠕・/summary>
        public float GetMoveHorizontal() => moveInput.x;

        /// <summary>遘ｻ蜍募・蜉帙・Y謌仙・繧貞叙蠕・/summary>
        public float GetMoveVertical() => moveInput.y;

        /// <summary>謾ｻ謦・ｸｭ縺句愛螳・/summary>
        public bool IsAttacking() => Input.GetKey(attackKey);

        /// <summary>繝ｪ繝ｭ繝ｼ繝我ｸｭ縺句愛螳・/summary>
        public bool IsReloading() => Input.GetKey(reloadKey);

        /// <summary>繝槭え繧ｹ菴咲ｽｮ繧貞叙蠕・/summary>
        public Vector3 GetMousePosition() => Input.mousePosition;

        /// <summary>繧ｹ繧ｯ繝ｭ繝ｼ繝ｫ驥上ｒ蜿門ｾ・/summary>
        public float GetScrollDelta() => Input.mouseScrollDelta.y;

        // ====================
        // 蜈･蜉幄ｨｭ螳・        // ====================

        /// <summary>繧ｭ繝ｼ繧｢繧ｵ繧､繝ｳ繧定ｨｭ螳・/summary>
        public void SetKeyBinding(string action, KeyCode key)
        {
            switch (action.ToUpper())
            {
                case "MOVEFORWARD": moveForward = key; break;
                case "MOVEBACKWARD": moveBackward = key; break;
                case "MOVELEFT": moveLeft = key; break;
                case "MOVERIGHT": moveRight = key; break;
                case "ATTACK": attackKey = key; break;
                case "RELOAD": reloadKey = key; break;
                case "SKILLONE": skillOneKey = key; break;
                case "SKILLTWO": skillTwoKey = key; break;
                case "ULTIMATE": ultimateKey = key; break;
                case "CONFIRM": confirmKey = key; break;
                case "CANCEL": cancelKey = key; break;
                case "TURNEND": turnEndKey = key; break;
                case "MINIMAP": minimapKey = key; break;
            }
        }

        /// <summary>蜈･蜉帷┌蜉ｹ蛹厄ｼ医Γ繝九Η繝ｼ陦ｨ遉ｺ譎ゅ↑縺ｩ・・/summary>
        public void DisableInput(bool disable)
        {
            Cursor.lockState = disable ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }
}

