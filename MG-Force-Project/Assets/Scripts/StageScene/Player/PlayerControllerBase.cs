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
        [SerializeField] private bool _disablePhysicsOnGoal = true;
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
            if (HasState(State.GOAL)) return;

            Debug.Log("[Player] ゴール到達");

            ClearState();
            AddState(State.GOAL);

            // アニメーション制御は PlayerAnimationController で処理
            OnGoal();
        }

        /// <summary>
        /// ゴール時の演出処理
        /// ※ アニメーション制御は PlayerAnimationController に任せる
        /// </summary>
        protected virtual void OnGoal()
        {
            if (_disablePhysicsOnGoal)
            {
                DisablePhysics();
            }
        }

        /// <summary>
        /// 物理演算を停止
        /// </summary>
        private void DisablePhysics()
        {
            var characterController = GetComponent<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = false;
                Debug.Log("[Player] CharacterController停止");
            }

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                Debug.Log("[Player] Rigidbody停止");
            }
        }
        #endregion

        #region ===== 基底メソッド =====
        public virtual void OnStart() { }
        public virtual void OnUpdate() { }
        #endregion
    }
}