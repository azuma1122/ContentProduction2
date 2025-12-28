using UnityEngine;

namespace Game.StageScene.Player
{
    /// <summary>
    /// プレイヤーのアニメーション制御クラス
    /// - ゴールアニメーションを最優先
    /// - 優先順位: GOAL > JUMP > SHOOT > RUN > IDLE
    /// - アニメーションの遷移と制御を管理
    /// - ゴール後も物理演算は継続（停止なし）
    /// </summary>
    public class PlayerAnimationController : PlayerControllerBase
    {
        #region -------- Animation 定数 --------
        private const string CURRENT_STATE = "CurrentState";          // 0=NONE,1=IDLE,2=RUN,3=JUMP,4=SHOOT,5=GOAL
        private const string CURRENT_DIRECTION = "CurrentDirection"; // 射撃方向
        #endregion

        /// <summary>
        /// アニメーション状態
        /// </summary>
        private enum AnimationState
        {
            NONE = 0,
            IDLE = 1,
            RUN = 2,
            JUMP = 3,
            SHOOT = 4,
            GOAL = 5,
        }

        /// <summary>
        /// アニメーションレイヤー
        /// </summary>
        private enum AnimationLayer
        {
            BASE = 0,
            RIGHT = 1,
            LEFT = 2,
        }

        // ===== 内部変数 =====
        private Animator _animator;
        private AnimationState _currentAnimationState;
        private AnimationLayer _currentAnimationLayer;
        private float _currentAnimationTime;

        // ===== ゴール演出用 =====
        private bool _isGoalAnimationStarted = false;
        private bool _goalAnimationCompleted = false;

        /// <summary>
        /// 初期化
        /// </summary>
        public override void OnStart()
        {
            _animator = playerObject.GetComponent<Animator>();

            if (_animator == null)
            {
                Debug.LogError("[PlayerAnimationController] Animatorが見つかりません。Playerオブジェクトに設定してください。");
                return;
            }

            Debug.Log($"[PlayerAnimationController] 初期化完了 - Animator: {_animator.name}");

            _currentAnimationState = AnimationState.IDLE;
            _currentAnimationLayer = AnimationLayer.RIGHT;
            _isGoalAnimationStarted = false;
            _goalAnimationCompleted = false;
        }

        /// <summary>
        /// 毎フレーム更新
        /// </summary>
        public override void OnUpdate()
        {
            if (_animator == null) return;

            // ===== ゴール状態の場合は向きやレイヤー変更をスキップ =====
            if (HasState(State.GOAL))
            {
                // ゴール状態が始まった瞬間の処理
                if (!_isGoalAnimationStarted)
                {
                    _isGoalAnimationStarted = true;
                    _currentAnimationState = AnimationState.GOAL;
                    _currentAnimationTime = 0f;

                    // アニメーション速度を明示的に1に設定
                    _animator.speed = 1f;

                    // BaseLayerをアクティブにする（ゴールアニメーションはBaseLayerに配置）
                    _animator.SetLayerWeight((int)AnimationLayer.BASE, 1);
                    _animator.SetLayerWeight((int)AnimationLayer.RIGHT, 0);
                    _animator.SetLayerWeight((int)AnimationLayer.LEFT, 0);

                    Debug.Log("[Animation] ゴールアニメーション開始");
                    Debug.Log($"[Animation] BaseLayer Weight = {_animator.GetLayerWeight((int)AnimationLayer.BASE)}");
                    Debug.Log($"[Animation] RightLayer Weight = {_animator.GetLayerWeight((int)AnimationLayer.RIGHT)}");
                    Debug.Log($"[Animation] LeftLayer Weight = {_animator.GetLayerWeight((int)AnimationLayer.LEFT)}");
                }

                // Animator反映を実行
                UpdateAnimatorParameters();

                // 現在のアニメーション状態をチェック
                AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo((int)AnimationLayer.BASE);

                // デバッグ：現在の状態を表示
                Debug.Log($"[Animation] Current State Name: {(stateInfo.IsName("Goal") ? "Goal" : "Other")}, " +
                         $"NormalizedTime: {stateInfo.normalizedTime:F3}, " +
                         $"Speed: {_animator.speed}, " +
                         $"CurrentState Param: {_animator.GetInteger(CURRENT_STATE)}");

                // ===== アニメーション完了後の処理（停止なし） =====
                if (stateInfo.IsName("Goal") && stateInfo.normalizedTime >= 1.0f && !_goalAnimationCompleted)
                {
                    _goalAnimationCompleted = true;
                    Debug.Log("[Animation] ゴールアニメーション完了 - 継続再生");

                    // ❌ 削除: _animator.speed = 0;
                    // アニメーションを停止せず、ループまたは最終フレームを維持
                    // Animatorの設定でLoop Timeをオフにしていれば、最終フレームで自動的に止まります
                }

                return;
            }

            // ===== 1. レイヤー切り替え =====
            _animator.SetLayerWeight((int)_currentAnimationLayer, 0);

            _currentAnimationLayer =
                currentDir == Direction.RIGHT ? AnimationLayer.RIGHT : AnimationLayer.LEFT;

            _animator.SetLayerWeight((int)_currentAnimationLayer, 1);

            // ===== 2. 向き設定 =====
            SetAnimationDir();

            // ===== 3. 状態更新 =====
            StateUpdate();

            // ===== 4. Animator反映 =====
            UpdateAnimatorParameters();
        }

        /// <summary>
        /// アニメーション状態決定
        /// 優先順位: GOAL > JUMP > SHOOT > RUN > IDLE
        /// </summary>
        private void StateUpdate()
        {
            // ===== 最優先:ゴール演出 =====
            if (HasState(State.GOAL))
            {
                if (_currentAnimationState != AnimationState.GOAL)
                {
                    _currentAnimationTime = 0f;
                    Debug.Log("[Animation] ゴール状態に遷移");
                }

                _currentAnimationState = AnimationState.GOAL;
                return;
            }

            // ===== ジャンプ =====
            if (!isGrounded)
            {
                if (_currentAnimationState != AnimationState.JUMP)
                {
                    _currentAnimationTime = 0f;
                }

                _currentAnimationState = AnimationState.JUMP;
                JumpUpdate();
                return;
            }

            // ===== 射撃 =====
            if (HasState(State.SHOOT))
            {
                if (_currentAnimationState != AnimationState.SHOOT)
                {
                    _currentAnimationTime = 0f;
                }

                _currentAnimationState = AnimationState.SHOOT;
                ShootUpdate();
                return;
            }

            // ===== 走行 =====
            if (isGrounded && HasState(State.RUN))
            {
                _currentAnimationState = AnimationState.RUN;
                return;
            }

            // ===== 待機 =====
            _currentAnimationState = AnimationState.IDLE;
        }

        /// <summary>
        /// モデルの向き設定
        /// </summary>
        private void SetAnimationDir()
        {
            playerTransform.eulerAngles =
                _currentAnimationLayer == AnimationLayer.RIGHT
                    ? new Vector3(0f, 90f, 0f)
                    : new Vector3(0f, 270f, 0f);
        }

        /// <summary>
        /// Animatorパラメータ反映
        /// </summary>
        private void UpdateAnimatorParameters()
        {
            if (_animator.GetInteger(CURRENT_DIRECTION) != (int)shootDir)
            {
                _animator.SetInteger(CURRENT_DIRECTION, (int)shootDir);
            }

            if (_animator.GetInteger(CURRENT_STATE) != (int)_currentAnimationState)
            {
                _animator.SetInteger(CURRENT_STATE, (int)_currentAnimationState);
                Debug.Log($"[Animation] CurrentState = {(int)_currentAnimationState} ({_currentAnimationState})");
            }
        }

        /// <summary>
        /// ジャンプアニメーション制御
        /// </summary>
        private void JumpUpdate()
        {
            AnimatorStateInfo info =
                _animator.GetCurrentAnimatorStateInfo((int)_currentAnimationLayer);
            _currentAnimationTime = info.normalizedTime;
        }

        /// <summary>
        /// 射撃アニメーション制御
        /// </summary>
        private void ShootUpdate()
        {
            AnimatorStateInfo info =
                _animator.GetCurrentAnimatorStateInfo((int)_currentAnimationLayer);
            _currentAnimationTime = info.normalizedTime;
        }

        /// <summary>
        /// ゴール到達時の演出処理（オーバーライド）
        /// ※ 物理演算停止なし版
        /// </summary>
        protected override void OnGoal()
        {
            // ===== オプション1: 物理演算を完全に停止しない場合 =====
            // 以下をコメントアウトすると、ゴール後も重力や移動が継続します

            /*
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                Debug.Log("[ゴール] Rigidbody停止");
            }

            var characterController = GetComponent<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = false;
                Debug.Log("[ゴール] CharacterController停止");
            }
            */

            // ===== オプション2: 速度だけゼロにして物理演算は継続 =====
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                // rb.isKinematic = true; ← コメントアウト（物理演算は継続）
                Debug.Log("[ゴール] Rigidbody速度リセット（物理演算は継続）");
            }

            // ゴールアニメーション準備
            _isGoalAnimationStarted = false;
            _goalAnimationCompleted = false;
            Debug.Log("[Animation] OnGoal呼び出し - 次のOnUpdateでゴールアニメーション開始");
        }
    }
}