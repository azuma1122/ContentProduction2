using UnityEngine;
using Game.StageScene.Magnet; // Magnet名前空間は使用されていない可能性あり
using Game.GameSystem;

namespace Game.StageScene.Player
{
    /// <summary>
    /// プレイヤーの状態管理クラス。
    /// 入力に応じて State（静止・走行・ジャンプ・射撃など）を更新し、
    /// 他のコントローラー（移動、アニメーション）に現在の状態を伝達します。
    /// </summary>
    public class PlayerStateController : PlayerControllerBase
    {
        // ===== 参照するコンポーネント =====
        private InputHandler _inputHandler;         // 入力処理管理（シングルトン）
        private BulletShootController _bulletShoot; // 射撃制御
        private Animator _animator;                 // Animator（現在は未使用）

        // InputHandlerがない場合のフォールバックフラグ
        private bool _useDirectInput = false;

        // InputHandler取得の再試行制御
        private int _inputHandlerRetryCount = 0;
        private const int MAX_RETRY_COUNT = 30; // 最大30フレーム（約0.5秒）試行

        /// <summary>
        /// 初期化処理（プレイヤー生成時1回）。
        /// 必要なコンポーネントの取得と、InputHandlerの初期取得を試みます。
        /// </summary>
        public override void OnStart()
        {
            Debug.Log("[PlayerStateController] OnStart開始");

            // 射撃制御コンポーネント取得
            _bulletShoot = playerObject.GetComponent<BulletShootController>();
            if (_bulletShoot == null)
            {
                Debug.LogWarning("[PlayerStateController] BulletShootControllerが見つかりません");
            }

            // Animator取得（現在は未使用）
            _animator = playerObject.GetComponent<Animator>();

            // 初期状態設定
            currentState = State.STILLNESS; // 静止状態からスタート
            currentDir = Direction.RIGHT;   // 右向きからスタート

            // InputHandlerの取得を試みる（見つからなくても直接入力で続行）
            TryGetInputHandler();

            Debug.Log($"[PlayerStateController] 初期化完了 - useDirectInput={_useDirectInput}");
        }

        /// <summary>
        /// InputHandlerのシングルトンインスタンスの取得を試みます。
        /// 失敗した場合、_useDirectInputフラグを立てて直接入力に切り替えます。
        /// </summary>
        private void TryGetInputHandler()
        {
            _inputHandler = InputHandler.Instance;

            if (_inputHandler == null)
            {
                Debug.LogWarning("[PlayerStateController] InputHandlerが見つかりません。直接入力を使用します");
                _useDirectInput = true;
            }
            else
            {
                Debug.Log("[PlayerStateController] InputHandlerを取得しました");
                _useDirectInput = false;
            }
        }

        /// <summary>
        /// 毎フレーム更新処理。
        /// 入力状態をチェックし、プレイヤーの状態（State）を更新します。
        /// </summary>
        public override void OnUpdate()
        {
            // InputHandlerの取得を再試行（InputHandlerの初期化遅延に対応）
            if (_useDirectInput && _inputHandlerRetryCount < MAX_RETRY_COUNT)
            {
                _inputHandlerRetryCount++;
                if (_inputHandlerRetryCount % 10 == 0) // 10フレームごとに再試行
                {
                    TryGetInputHandler();
                }
            }

            // ===== 1. 射撃優先チェック =====
            // 射撃中は他の操作（移動・ジャンプ）を無効化するため、最優先でチェック
            bool isShooting = CheckShootInput();
            if (isShooting)
            {
                currentState = State.NONE; // 全状態を一旦クリア
                AddState(State.SHOOT);     // 射撃状態のみ追加
                return;                    // 射撃中は移動・ジャンプ処理をスキップ
            }
            RemoveState(State.SHOOT);       // 射撃が終了していれば射撃状態を解除

            // ===== 2. ジャンプ状態更新 =====
            // 接地状態とジャンプ入力をチェックし、JUMP状態を追加/解除
            JumpUpdate();

            // ===== 3. 移動入力更新 =====
            // 左右の移動入力をチェックし、RUN/STILLNESS状態と向きを更新
            RunUpdate();
        }

        /// <summary>
        /// 射撃入力の判定を行います。
        /// InputHandlerまたは直接入力（Fire1/マウスボタン）を使用します。
        /// </summary>
        /// <returns>射撃入力がある場合は true</returns>
        private bool CheckShootInput()
        {
            bool isShooting = false;

            // BulletShootControllerが射撃中/チャージ中の状態をチェック
            if (_bulletShoot != null)
            {
                isShooting = _bulletShoot.IsCharging || _bulletShoot.IsShooting;
            }

            // InputHandler経由での射撃入力をチェック
            if (!_useDirectInput && _inputHandler != null)
            {
                try
                {
                    // InputHandlerのSHOOTアクションが押されているかチェック
                    isShooting |= _inputHandler.IsActionPressing(InputConstants.Action.SHOOT);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[PlayerStateController] InputHandler.IsActionPressingエラー: {e.Message} -> 直接入力に切り替え");
                    _useDirectInput = true; // エラーが出た場合は直接入力に切り替え
                }
            }
            // 直接入力（フォールバック）での射撃入力をチェック
            else
            {
                // Fire1ボタン（スペースキーやマウス左クリック）またはマウス右クリック
                isShooting |= Input.GetButton("Fire1") || Input.GetMouseButton(0) || Input.GetMouseButton(1);
            }

            // 射撃入力がある場合、射撃方向を更新
            if (isShooting) UpdateShootDirection();

            return isShooting;
        }

        /// <summary>
        /// 射撃方向を更新します（8方向+真上）。
        /// InputHandlerまたは現在のプレイヤーの向きを使用します。
        /// </summary>
        private void UpdateShootDirection()
        {
            // InputHandler経由での射撃方向入力をチェック
            if (!_useDirectInput && _inputHandler != null)
            {
                try
                {
                    // InputHandlerのSHOOT_ANGLEアクションから8方向の入力を判定し、shootDirとcurrentDirを更新
                    // ... (8方向の複雑な判定ロジック) ...
                    if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT_ANGLE, InputConstants.ActionVector.North))
                    {
                        shootDir = 0;
                    }
                    else if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT_ANGLE, InputConstants.ActionVector.NorthEast))
                    {
                        currentDir = Direction.RIGHT;
                        shootDir = 45;
                    }
                    else if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT_ANGLE, InputConstants.ActionVector.East))
                    {
                        currentDir = Direction.RIGHT;
                        shootDir = 90;
                    }
                    else if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT_ANGLE, InputConstants.ActionVector.SouthEast))
                    {
                        currentDir = Direction.RIGHT;
                        shootDir = 135;
                    }
                    else if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT_ANGLE, InputConstants.ActionVector.NorthWest))
                    {
                        currentDir = Direction.LEFT;
                        shootDir = 45;
                    }
                    else if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT_ANGLE, InputConstants.ActionVector.West))
                    {
                        currentDir = Direction.LEFT;
                        shootDir = 90;
                    }
                    else if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT_ANGLE, InputConstants.ActionVector.SouthWest))
                    {
                        currentDir = Direction.LEFT;
                        shootDir = 135;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[PlayerStateController] UpdateShootDirectionエラー: {e.Message}");
                }
            }
            // 直接入力の場合は現在のプレイヤーの向きを維持
            else
            {
                // 90度（右）または -90度（左）に設定
                shootDir = currentDir == Direction.RIGHT ? 90 : -90;
            }
        }

        /// <summary>
        /// 移動入力（左右）をチェックし、RUN/STILLNESS状態と向きを更新します。
        /// 射撃中は呼び出されません。
        /// </summary>
        private void RunUpdate()
        {
            bool leftPressed = false;
            bool rightPressed = false;

            // InputHandler経由での移動入力をチェック
            if (!_useDirectInput && _inputHandler != null)
            {
                try
                {
                    leftPressed = _inputHandler.IsActionPressed(InputConstants.Action.LEFTMOVE);
                    rightPressed = _inputHandler.IsActionPressed(InputConstants.Action.RIGHTMOVE);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[PlayerStateController] RunUpdate InputHandlerエラー: {e.Message} -> 直接入力に切り替え");
                    _useDirectInput = true;
                }
            }

            // 直接入力（フォールバック）での移動入力をチェック
            if (_useDirectInput)
            {
                // Axis入力（ジョイスティックなど）
                float horizontal = Input.GetAxis("Horizontal");
                leftPressed = horizontal < -0.1f;
                rightPressed = horizontal > 0.1f;

                // キーボード入力（A/D, 左右矢印）
                leftPressed |= Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
                rightPressed |= Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
            }

            // ジャンプ中の処理
            if (HasState(State.JUMP))
            {
                // ジャンプ中はRUN/STILLNESS状態を解除
                RemoveState(State.RUN);
                RemoveState(State.STILLNESS);

                // ジャンプ中でも向きの更新は行う
                if (leftPressed)
                    currentDir = Direction.LEFT;
                else if (rightPressed)
                    currentDir = Direction.RIGHT;

                return; // ジャンプ中は走行状態にしない
            }

            // 地上での移動処理
            if (leftPressed)
            {
                RemoveState(State.STILLNESS);
                AddState(State.RUN);
                currentDir = Direction.LEFT;
            }
            else if (rightPressed)
            {
                RemoveState(State.STILLNESS);
                AddState(State.RUN);
                currentDir = Direction.RIGHT;
            }
            else
            {
                // 入力がない場合
                RemoveState(State.RUN);
                // ジャンプ中でなければ静止状態にする
                if (!HasState(State.JUMP))
                    AddState(State.STILLNESS);
            }
        }

        /// <summary>
        /// ジャンプ入力をチェックし、JUMP状態の開始/終了を更新します。
        /// </summary>
        private void JumpUpdate()
        {
            bool jumpPressed = false;

            // InputHandler経由でのジャンプ入力をチェック
            if (!_useDirectInput && _inputHandler != null)
            {
                try
                {
                    // JUMPアクションが押された瞬間をチェック
                    jumpPressed = _inputHandler.IsActionPressed(InputConstants.Action.JUMP);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[PlayerStateController] JumpUpdate InputHandlerエラー: {e.Message} -> 直接入力に切り替え");
                    _useDirectInput = true;
                }
            }

            // 直接入力（フォールバック）でのジャンプ入力をチェック
            if (_useDirectInput)
            {
                // Jumpボタン（デフォルトでスペースキー）またはW/上矢印キーが押された瞬間をチェック
                jumpPressed = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
            }

            // ジャンプ開始の条件: 地面に接地しており、ジャンプ入力があった場合
            if (isGrounded && jumpPressed)
            {
                RemoveState(State.STILLNESS);
                AddState(State.JUMP);
                Debug.Log("[PlayerStateController] ジャンプ開始");
            }
            // 着地の条件: 地面に接地しており、かつJUMP状態であった場合
            else if (isGrounded && HasState(State.JUMP))
            {
                RemoveState(State.JUMP);
                Debug.Log("[PlayerStateController] 着地でジャンプ解除");

                // 走行中でなければ静止状態にする
                if (!HasState(State.RUN))
                    AddState(State.STILLNESS);
            }
        }
    }
}
