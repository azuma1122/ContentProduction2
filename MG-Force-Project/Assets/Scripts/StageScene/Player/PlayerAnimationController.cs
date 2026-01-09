using UnityEngine;

namespace Game.StageScene.Player
{
    /// <summary>
    /// プレイヤーのアニメーション制御クラス
    /// - ゴールアニメーションを最優先
    /// - 優先順位: GOAL > JUMP > SHOOT > RUN > IDLE
    /// - アニメーションの遷移と制御を管理
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

        [Header("ゴール時の物理演算")]
        [SerializeField] private bool _stopPhysicsOnGoal = true;

        [Header("ゴールアニメーション設定")]
        [SerializeField] private bool _useBaseLayerForGoal = true; // BaseLayerにゴールアニメーションがある

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

            // アニメーター速度を確認
            Debug.Log($"[PlayerAnimationController] Animator初期速度: {_animator.speed}");
        }

        /// <summary>
        /// 毎フレーム更新
        /// </summary>
        public override void OnUpdate()
        {
            if (_animator == null) return;

            // ===== ゴール状態の場合は特殊処理 =====
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
                    Debug.Log($"[Animation] Animator速度を1に設定: {_animator.speed}");

                    // ゴールアニメーションのレイヤー設定
                    if (_useBaseLayerForGoal)
                    {
                        // BaseLayerを使用する場合
                        _animator.SetLayerWeight((int)AnimationLayer.BASE, 1);
                        _animator.SetLayerWeight((int)AnimationLayer.RIGHT, 0);
                        _animator.SetLayerWeight((int)AnimationLayer.LEFT, 0);
                        Debug.Log("[Animation] ゴールアニメーション: BaseLayer使用");
                    }
                    else
                    {
                        // 現在のレイヤーをそのまま使用
                        Debug.Log($"[Animation] ゴールアニメーション: {_currentAnimationLayer}レイヤー使用");
                    }

                    Debug.Log("[Animation] ゴールアニメーション開始準備完了");
                }

                // Animator反映を実行（ゴール状態でも必ず実行）
                UpdateAnimatorParameters();

                // デバッグ: 現在のアニメーション状態を確認
                int layerToCheck = _useBaseLayerForGoal ? (int)AnimationLayer.BASE : (int)_currentAnimationLayer;
                AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(layerToCheck);

                if (Time.frameCount % 30 == 0) // 30フレームごとにログ出力
                {
                    Debug.Log($"[Animation] レイヤー{layerToCheck} - IsName(Goal): {stateInfo.IsName("Goal")}, " +
                              $"normalizedTime: {stateInfo.normalizedTime:F2}, " +
                              $"CurrentState param: {_animator.GetInteger(CURRENT_STATE)}");
                }

                return;
            }

            // ===== 通常の処理 =====
            // 1. レイヤー切り替え
            _animator.SetLayerWeight((int)_currentAnimationLayer, 0);

            _currentAnimationLayer =
                currentDir == Direction.RIGHT ? AnimationLayer.RIGHT : AnimationLayer.LEFT;

            _animator.SetLayerWeight((int)_currentAnimationLayer, 1);

            // 2. 向き設定
            SetAnimationDir();

            // 3. 状態更新
            StateUpdate();

            // 4. Animator反映
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
            // 射撃方向の更新
            if (_animator.GetInteger(CURRENT_DIRECTION) != (int)shootDir)
            {
                _animator.SetInteger(CURRENT_DIRECTION, (int)shootDir);
            }

            // 状態の更新
            int newState = (int)_currentAnimationState;
            if (_animator.GetInteger(CURRENT_STATE) != newState)
            {
                _animator.SetInteger(CURRENT_STATE, newState);
                Debug.Log($"[Animation] CurrentState更新: {newState} ({_currentAnimationState}) ");
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
        /// </summary>
        protected override void OnGoal()
        {
            Debug.Log("[Animation] ===== OnGoal呼び出し =====");

            // Animator速度を確認・修正
            if (_animator != null)
            {
                if (_animator.speed != 1f)
                {
                    Debug.LogWarning($"[Animation] Animator速度が異常: {_animator.speed} -> 1.0に修正");
                    _animator.speed = 1f;
                }
            }

            // ===== 物理演算の停止（オプション） =====
            if (_stopPhysicsOnGoal)
            {
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
            }
            else
            {
                var rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    Debug.Log("[ゴール] Rigidbody速度リセット（物理演算は継続）");
                }
            }

            // ゴールアニメーション準備
            _isGoalAnimationStarted = false;
            Debug.Log("[Animation] 次のOnUpdateでゴールアニメーション開始");
        }
    }
}