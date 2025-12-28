using UnityEngine;

namespace Game.StageScene.Player
{
    /// <summary>
    /// プレイヤーの各コントローラーの共通ベースクラス
    /// </summary>
    public class PlayerControllerBase : MonoBehaviour
    {
        #region ===== 初期化 =====
        public virtual void Initialize(GameObject player)
        {
            playerObject = player;
            playerTransform = player.transform;
            // 初期値を設定
            currentState = State.STILLNESS;
            currentDir = Direction.RIGHT;
            isGrounded = true;
            shootDir = 1f;

            // Animatorの自動取得
            if (_animator == null)
            {
                _animator = player.GetComponent<Animator>();
            }
        }
        #endregion

        #region ===== 状態定義 =====
        [System.Flags]
        public enum State
        {
            NONE = 0,
            NOT_STATE = 0,
            STILLNESS = 1 << 0,
            RUN = 1 << 1,
            JUMP = 1 << 2,
            SHOOT = 1 << 3,
            GOAL = 1 << 4,   // ゴール状態
        }

        public enum Direction
        {
            LEFT,
            RIGHT,
        }
        #endregion

        #region ===== 共通フィールド =====
        public static State currentState = State.STILLNESS;
        public static Direction currentDir = Direction.RIGHT;
        public static float shootDir = 1f;
        public static bool isGrounded = false;
        public static GameObject playerObject;
        public static Transform playerTransform;
        #endregion

        #region ===== ゴール演出設定 =====
        [Header("ゴール演出設定")]
        [SerializeField] private Animator _animator;
        [SerializeField] private string _goalAnimationTrigger = "Goal"; // トリガー名（使用しない場合は空）
        [SerializeField] private bool _disablePhysicsOnGoal = true; // 物理演算を停止するか
        #endregion

        #region ===== State操作メソッド =====
        public void AddState(State state) => currentState |= state;
        public void RemoveState(State state) => currentState &= ~state;
        public void ForceSetState(State newState) => currentState = newState;
        public void SetState(State newState) => currentState = newState;
        public bool HasState(State state) => (currentState & state) != 0;
        public State GetState() => currentState;
        public void ClearState() => currentState = State.NOT_STATE;
        #endregion

        #region ===== ゴール処理 =====
        /// <summary>
        /// ゴール到達時に CrystalController から呼ばれる
        /// </summary>
        public virtual void SetGoal()
        {
            // すでにゴール状態なら何もしない
            if (HasState(State.GOAL)) return;

            Debug.Log("Player : ゴール到達");

            // 状態をゴールに固定（操作停止用）
            ClearState();
            AddState(State.GOAL);

            // ゴール演出を実行
            OnGoal();
        }

        /// <summary>
        /// ゴール時の演出処理
        /// - 勝利ポーズアニメーション（shourishouri）再生
        /// - 物理演算の停止
        /// ※ PlayerAnimationController で上書きされる場合があります
        /// </summary>
        protected virtual void OnGoal()
        {
            // 勝利ポーズアニメーション再生
            PlayVictoryAnimation();

            // 物理演算を停止
            if (_disablePhysicsOnGoal)
            {
                DisablePhysics();
            }
        }

        /// <summary>
        /// 勝利ポーズアニメーション（shourishouri）を再生
        /// PlayerAnimationControllerが整数パラメータ方式を使用している場合、
        /// CurrentState = 5 を設定することでゴールアニメーションが再生されます
        /// </summary>
        private void PlayVictoryAnimation()
        {
            if (_animator == null)
            {
                Debug.LogWarning("Animatorが設定されていません。Playerに設定してください。");
                return;
            }

            bool animationTriggered = false;

            // 方式1: 整数パラメータ方式（PlayerAnimationController用）
            // CurrentState = 5 (GOAL) を設定
            foreach (var param in _animator.parameters)
            {
                if (param.name == "CurrentState" && param.type == AnimatorControllerParameterType.Int)
                {
                    _animator.SetInteger("CurrentState", 5); // AnimationState.GOAL = 5
                    Debug.Log("[ゴール] CurrentState = 5 (GOAL) に設定");
                    animationTriggered = true;
                    break;
                }
            }

            // 方式2: トリガー方式（従来の方法）
            // Goalトリガーが存在する場合のみ実行
            if (!animationTriggered && !string.IsNullOrEmpty(_goalAnimationTrigger))
            {
                bool hasTrigger = false;
                foreach (var param in _animator.parameters)
                {
                    if (param.name == _goalAnimationTrigger && param.type == AnimatorControllerParameterType.Trigger)
                    {
                        hasTrigger = true;
                        break;
                    }
                }

                if (hasTrigger)
                {
                    _animator.SetTrigger(_goalAnimationTrigger);
                    Debug.Log($"[ゴール] トリガー発火: {_goalAnimationTrigger}");
                    animationTriggered = true;
                }
            }

            if (!animationTriggered)
            {
                Debug.LogWarning("[ゴール] アニメーションパラメータが見つかりません。Animatorに「CurrentState」(Int)または「Goal」(Trigger)を設定してください。");
            }
        }

        /// <summary>
        /// 物理演算を停止
        /// </summary>
        private void DisablePhysics()
        {
            // CharacterControllerを使用している場合
            var characterController = GetComponent<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = false;
                Debug.Log("CharacterController停止");
            }

            // Rigidbodyを使用している場合
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                Debug.Log("Rigidbody停止");
            }
        }
        #endregion

        #region ===== 基底メソッド =====
        public virtual void OnStart() { }
        public virtual void OnUpdate() { }
        #endregion
    }
}