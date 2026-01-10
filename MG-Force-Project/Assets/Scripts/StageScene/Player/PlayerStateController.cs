using UnityEngine;
using Game.StageScene.Magnet;
using Game.GameSystem;

namespace Game.StageScene.Player
{
    /// <summary>
    /// プレイヤーの状態管理クラス（修正版）
    /// - 入力に応じて State（静止・走行・ジャンプ・射撃など）を更新
    /// - 射撃中は移動・ジャンプ操作を完全に無効化
    /// - ジャンプ状態は空中にいる間維持
    /// - アニメーション優先順位: JUMP > SHOOT > RUN > IDLE
    /// </summary>
    public class PlayerStateController : PlayerControllerBase
    {
        // ===== 参照するコンポーネント =====
        private InputHandler _inputHandler;        // 入力処理を管理するクラス（キーボード・マウス入力）
        private BulletShootController _bulletShoot; // 射撃関連の制御クラス（チャージ・発射など）
        private Animator _animator;                 // プレイヤーのアニメーション制御用Animator（現在未使用）
        private MagnetManager _magnet;              // 磁力システム管理クラス

        /// <summary>
        /// 初期化処理（プレイヤー生成時に1回だけ呼ばれる）
        /// </summary>
        public override void OnStart()
        {
            // シーン上にある "Input" オブジェクトから入力管理クラスを取得
            _inputHandler = GameObject.Find(GameConstants.Object.INPUT).GetComponent<InputHandler>();

            // プレイヤー自身にアタッチされている射撃スクリプトを取得
            _bulletShoot = playerObject.GetComponent<BulletShootController>();

            // Animatorコンポーネントを取得（将来的に使う可能性があるため保持）
            _animator = playerObject.GetComponent<Animator>();

            // 磁力マネージャーを取得
            _magnet = GameObject.Find(GameConstants.MAGNET_MANAGER_OBJ)
                                .GetComponent<MagnetManager>();

            // ゲーム開始時の初期状態を設定
            currentState = State.STILLNESS; // 静止状態からスタート
            currentDir = Direction.RIGHT;   // 右向きからスタート
        }

        /// <summary>
        /// 毎フレーム呼ばれる更新処理
        /// 入力をチェックして、プレイヤーの状態を更新する
        /// </summary>
        public override void OnUpdate()
        {
            // ===== 0. Boot中チェック（最優先） =====
            // Boot中は射撃状態を強制的に解除
            if (_magnet != null && _magnet.IsMagnetBoot)
            {
                RemoveState(State.SHOOT);

                // Boot中でも移動・ジャンプは処理する
                JumpUpdate();
                RunUpdate();
                return;
            }

            // ===== 1. 射撃状態のチェック（Boot中でない場合のみ） =====
            // 射撃ボタンが押されているか、チャージ中か、発射中かをチェック
            bool isShooting = CheckShootInput();

            if (isShooting)
            {
                // 射撃中は他の状態を全て無効化（移動・ジャンプ不可）
                currentState = State.NONE;  // 全ての状態フラグをクリア
                AddState(State.SHOOT);      // 射撃状態のみを追加
                return; // 移動やジャンプ処理を完全にスキップ
            }

            // ===== 2. 射撃していない場合のみ通常操作を処理 =====
            RemoveState(State.SHOOT); // 射撃状態を解除

            // ===== 3. ジャンプ状態の更新 =====
            // ジャンプボタンの入力と着地をチェックして、State.JUMPを管理
            JumpUpdate();

            // ===== 4. 移動入力の更新 =====
            // 左右キーの入力をチェックして、State.RUNとcurrentDirを管理
            RunUpdate();
        }

        /// <summary>
        /// 射撃入力と状態をチェック
        /// </summary>
        /// <returns>射撃中（入力あり or チャージ中 or 発射中）ならtrue</returns>
        private bool CheckShootInput()
        {
            // 以下のいずれかが真なら射撃中と判定
            // - IsCharging: 射撃ボタンを押してチャージ中
            // - IsShooting: 弾を発射する準備ができている
            // - IsActionPressing: 射撃ボタンが現在押されている
            bool isShooting = _bulletShoot.IsCharging ||
                              _bulletShoot.IsShooting ||
                              _inputHandler.IsActionPressing(InputConstants.Action.SHOOT);

            // 射撃中であれば射撃方向を更新
            if (isShooting)
            {
                UpdateShootDirection(); // 8方向 + 真上の射撃方向を決定
            }

            return isShooting;
        }

        /// <summary>
        /// 射撃方向の更新（8方向 + 真上の9方向に対応）
        /// 入力に応じてshootDir（角度）とcurrentDir（左右の向き）を更新
        /// </summary>
        private void UpdateShootDirection()
        {
            // 真上への射撃
            if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT_ANGLE, InputConstants.ActionVector.North))
            {
                shootDir = 0; // 真上を0度とする
            }
            // 右斜め上への射撃
            else if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT_ANGLE, InputConstants.ActionVector.NorthEast))
            {
                currentDir = Direction.RIGHT; // キャラクターは右向き
                shootDir = 45;                // 45度（右斜め上）
            }
            // 右への射撃
            else if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT_ANGLE, InputConstants.ActionVector.East))
            {
                currentDir = Direction.RIGHT; // キャラクターは右向き
                shootDir = 90;                // 90度（真横）
            }
            // 右斜め下への射撃
            else if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT_ANGLE, InputConstants.ActionVector.SouthEast))
            {
                currentDir = Direction.RIGHT; // キャラクターは右向き
                shootDir = 135;               // 135度（右斜め下）
            }
            // 左斜め上への射撃
            else if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT_ANGLE, InputConstants.ActionVector.NorthWest))
            {
                currentDir = Direction.LEFT; // キャラクターは左向き
                shootDir = 45;               // 45度（左斜め上）※左向きなので実際は315度相当
            }
            // 左への射撃
            else if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT_ANGLE, InputConstants.ActionVector.West))
            {
                currentDir = Direction.LEFT; // キャラクターは左向き
                shootDir = 90;               // 90度（真横）※左向きなので実際は270度相当
            }
            // 左斜めへの射撃
            else if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT_ANGLE, InputConstants.ActionVector.SouthWest))
            {
                currentDir = Direction.LEFT; // キャラクターは左向き
                shootDir = 135;              // 135度（左斜め下）※左向きなので実際は225度相当
            }
        }

        /// <summary>
        /// 左右移動入力の更新処理
        /// キーボード入力に応じてState.RUNとcurrentDirを管理
        /// ジャンプ中は方向だけ更新（State.RUNは追加しない）
        /// </summary>
        private void RunUpdate()
        {
            // ===== ジャンプ中の処理 =====
            // ジャンプ中は移動状態フラグを立てない（アニメーションはジャンプ優先）
            // ただし、方向入力は受け付ける（空中制御のため）
            if (HasState(State.JUMP))
            {
                // まずState.RUNを解除（走行アニメーションが出ないようにする）
                RemoveState(State.RUN);
                // State.STILLNESSも解除（Idleアニメーションが一瞬表示されるのを防ぐ）
                RemoveState(State.STILLNESS);

                // 左キーが押されている場合
                if (_inputHandler.IsActionPressed(InputConstants.Action.LEFTMOVE))
                {
                    currentDir = Direction.LEFT; // 方向だけ更新（PlayerMoveControllerで横移動に使用）
                }
                // 右キーが押されている場合
                else if (_inputHandler.IsActionPressed(InputConstants.Action.RIGHTMOVE))
                {
                    currentDir = Direction.RIGHT; // 方向だけ更新
                }
                return; // ジャンプ中はState.RUNを追加しない（アニメーションがジャンプのまま）
            }

            // ===== 地面にいる時の通常の移動処理 =====
            // 左キーが押されている場合
            if (_inputHandler.IsActionPressed(InputConstants.Action.LEFTMOVE))
            {
                RemoveState(State.STILLNESS); // 静止状態を解除
                AddState(State.RUN);          // 走行状態を追加（アニメーションが走行になる）
                currentDir = Direction.LEFT;  // 向きを左に設定
            }
            // 右キーが押されている場合
            else if (_inputHandler.IsActionPressed(InputConstants.Action.RIGHTMOVE))
            {
                RemoveState(State.STILLNESS); // 静止状態を解除
                AddState(State.RUN);          // 走行状態を追加
                currentDir = Direction.RIGHT; // 向きを右に設定
            }
            // 左右キーが押されていない場合
            else
            {
                RemoveState(State.RUN);       // 走行状態を解除
                // ジャンプ中でない場合のみSTILLNESSを追加
                if (!HasState(State.JUMP))
                {
                    AddState(State.STILLNESS);    // 静止状態を追加（アニメーションが待機になる）
                }
            }
        }

        /// <summary>
        /// ジャンプ入力の更新処理（修正版）
        /// - ジャンプボタンが押された瞬間にState.JUMPを追加
        /// - 着地したらState.JUMPを解除
        /// - 空中にいる間はState.JUMPを維持
        /// </summary>
        private void JumpUpdate()
        {
            // ===== ジャンプボタンが押された瞬間（地面にいる時のみ有効） =====
            if (isGrounded && _inputHandler.IsActionPressed(InputConstants.Action.JUMP))
            {
                RemoveState(State.STILLNESS); // 静止状態を解除
                AddState(State.JUMP);         // ジャンプ状態を追加（PlayerMoveControllerでジャンプ力が与えられる）
                Debug.Log("[StateController] ジャンプ状態追加");
            }
            // ===== 地面についたらジャンプ状態を解除 =====
            else if (isGrounded && HasState(State.JUMP))
            {
                RemoveState(State.JUMP); // ジャンプ状態を解除（着地完了）
                Debug.Log("[StateController] ジャンプ状態解除（着地）");

                // 移動キーが押されていなければ静止状態に戻す
                if (!HasState(State.RUN))
                {
                    AddState(State.STILLNESS);
                }
            }
        }
    }
}