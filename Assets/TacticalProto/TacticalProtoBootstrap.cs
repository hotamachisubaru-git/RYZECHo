using UnityEngine;
using RYZECHo.TacticalProto;
using RYZECHo.TacticalProto.UI;

namespace RYZECHo.TacticalProto
{
    /// <summary>
    /// TacticalProtoの初期化・接続コンポーネント。
    /// TacticalGameModelの生成、HUD/Minimap/InputManagerへの参照設定を行う。
    /// </summary>
    public class TacticalProtoBootstrap : MonoBehaviour
    {
        [Header("参照設定")]
        [SerializeField] private TacticalHUD hud;
        [SerializeField] private TacticalMinimap minimap;
        [SerializeField] private TacticalInputManager inputManager;

        private TacticalGameModel _gameModel;

        private void Awake()
        {
            _gameModel = new TacticalGameModel();

            if (hud != null)
            {
                hud.SetModel(_gameModel);
                hud.Sync(_gameModel);
            }

            if (minimap != null)
            {
                minimap.InitializeForPhase(_gameModel.CurrentPhase);
            }

            if (inputManager != null)
            {
                inputManager.OnPhaseConfirm += () =>
                {
                    if (_gameModel.CurrentPhase == GamePhase.Construct)
                    {
                        _gameModel.Update(0f, new TacticalInput(
                            false, false, false, false,
                            false, false, false, false,
                            false, false, false, false, false, false,
                            false, false, true,
                            false, false,
                            mouseWorldPosition: System.Numerics.Vector2.Zero));
                    }
                    else if (_gameModel.CurrentPhase == GamePhase.Bet)
                    {
                        _gameModel.Update(0f, new TacticalInput(
                            false, false, false, false,
                            false, false, false, false,
                            false, false, false, false, false, false,
                            false, false, true,
                            false, false,
                            System.Numerics.Vector2.Zero));
                    }
                };

                inputManager.OnTurnEnd += () =>
                {
                    if (_gameModel.CurrentPhase == GamePhase.Hunt)
                    {
                        _gameModel.EndRound(true, "ラウンド終了。");
                    }
                };

                inputManager.OnMove += (dir) =>
                {
                    if (_gameModel.CurrentPhase == GamePhase.Hunt && _gameModel.Player != null)
                    {
                        var input = new TacticalInput(
                            moveUp: dir.y > 0.1f,
                            moveDown: dir.y < -0.1f,
                            moveLeft: dir.x < -0.1f,
                            moveRight: dir.x > 0.1f,
                            pressQ: false, pressE: false, pressR: false, pressT: false,
                            press1: false, press2: false, press3: false, press4: false, press5: false, press6: false,
                            fireHeld: false, interactHeld: false, confirm: false,
                            adjustBetLeft: false, adjustBetRight: false,
                            mouseWorldPosition: System.Numerics.Vector2.Zero);
                        _gameModel.Update(Time.deltaTime, input);
                    }
                };

                inputManager.OnAttack += () =>
                {
                    if (_gameModel.CurrentPhase == GamePhase.Hunt && _gameModel.Player != null)
                    {
                        var input = new TacticalInput(
                            moveUp: false, moveDown: false, moveLeft: false, moveRight: false,
                            pressQ: false, pressE: false, pressR: false, pressT: false,
                            press1: false, press2: false, press3: false, press4: false, press5: false, press6: false,
                            fireHeld: true, interactHeld: false, confirm: false,
                            adjustBetLeft: false, adjustBetRight: false,
                            mouseWorldPosition: System.Numerics.Vector2.Zero);
                        _gameModel.Update(Time.deltaTime, input);
                    }
                };

                inputManager.OnReload += () => { };
            }

            Debug.Log("[TacticalProto] Bootstrap complete. Phase: " + _gameModel.CurrentPhase);
        }

        private void Update()
        {
            if (_gameModel == null) return;

            var moveInput = inputManager != null ? inputManager.GetMoveInput() : Vector2.zero;
            var input = new TacticalInput(
                moveUp: moveInput.y > 0.1f,
                moveDown: moveInput.y < -0.1f,
                moveLeft: moveInput.x < -0.1f,
                moveRight: moveInput.x > 0.1f,
                pressQ: Input.GetKeyDown(KeyCode.Q),
                pressE: Input.GetKeyDown(KeyCode.E),
                pressR: Input.GetKeyDown(KeyCode.R),
                pressT: Input.GetKeyDown(KeyCode.T),
                press1: Input.GetKeyDown(KeyCode.Alpha1),
                press2: Input.GetKeyDown(KeyCode.Alpha2),
                press3: Input.GetKeyDown(KeyCode.Alpha3),
                press4: Input.GetKeyDown(KeyCode.Alpha4),
                press5: Input.GetKeyDown(KeyCode.Alpha5),
                press6: Input.GetKeyDown(KeyCode.Alpha6),
                fireHeld: Input.GetKey(KeyCode.Mouse0),
                interactHeld: Input.GetKey(KeyCode.Mouse1),
                confirm: Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space),
                adjustBetLeft: Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Alpha0),
                adjustBetRight: Input.GetKeyDown(KeyCode.Alpha5),
                mouseWorldPosition: new System.Numerics.Vector2((float)Input.mousePosition.x, (float)Input.mousePosition.y));

            _gameModel.Update(Time.deltaTime, input);

            if (hud != null)
            {
                hud.Sync(_gameModel);
            }
        }
    }
}
