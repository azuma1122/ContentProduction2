using UnityEngine;

namespace Game.StageScene.Player
{
    /// <summary>
    /// プレイヤーの各コントローラーの共通ベースクラス
    /// 
    /// 【役割】
    /// - 各プレイヤー関連スクリプト（移動・アニメーション・射撃など）が継承する基底クラス
    /// - プレイヤーの状態（State / Direction / isGrounded など）を保持・共有
    /// - プレイヤーの Transform / GameObject 参照を保持
    /// - 各コントローラーで共通処理を統一し、重複を防ぐ
    /// </summary>
    public class PlayerControllerBase : MonoBehaviour
    {
        #region ===== 初期化 =====
        /// <summary>
        /// プレイヤーオブジェクトを初期化
        /// - 各継承クラスで呼び出すことを想定
        /// - Transform / GameObject の参照をセット
        /// - 初期状態を待機(STILLNESS)・右向き(RIGHT)に設定
        /// </summary>
        /// <param name="player">プレイヤーのGameObject</param>
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
        /// <summary>
        /// プレイヤーの行動状態を定義する列挙体
        /// Flags 属性により、複数の状態をビットフラグとして併用可能
        /// </summary>
        [System.Flags]
        public enum State
        {
            NONE = 0,
            NOT_STATE = 0,     // 状態なし
            STILLNESS = 1 << 0, // 静止中
            RUN = 1 << 1,       // 走行中
            JUMP = 1 << 2,      // ジャンプ中
            SHOOT = 1 << 3,     // 射撃中
        }

        /// <summary>
        /// プレイヤーの向き
        /// </summary>
        public enum Direction
        {
            LEFT,   // 左向き
            RIGHT,  // 右向き
        }
        #endregion

        #region ===== 共通フィールド =====
        /// <summary>現在のプレイヤー状態（複数状態を同時保持可能）</summary>
        public static State currentState = State.STILLNESS;

        /// <summary>プレイヤーの向き</summary>
        public static Direction currentDir = Direction.RIGHT;

        /// <summary>射撃方向（1:右 / -1:左）</summary>
        public static float shootDir = 1f;

        /// <summary>プレイヤーが地面に接地しているかどうか</summary>
        public static bool isGrounded = false;

        /// <summary>プレイヤー本体の GameObject 参照</summary>
        public static GameObject playerObject;

        /// <summary>プレイヤーの Transform 参照</summary>
        public static Transform playerTransform;
        #endregion

        #region ===== State操作メソッド =====
        /// <summary>
        /// 状態を追加（例：RUN中にJUMPを追加するなど）
        /// </summary>
        public void AddState(State state) => currentState |= state;

        /// <summary>
        /// 指定状態を削除（例：JUMP状態を解除）
        /// </summary>
        public void RemoveState(State state) => currentState &= ~state;

        /// <summary>
        /// 現在の状態を強制的に上書き（他の状態をすべてリセットして新しい状態に変更）
        /// </summary>
        public void ForceSetState(State newState) => currentState = newState;

        /// <summary>
        /// 状態を通常の方法で上書き（用途は ForceSetState と同じ）
        /// </summary>
        public void SetState(State newState) => currentState = newState;

        /// <summary>
        /// 指定した状態を現在保持しているかどうかを返す
        /// </summary>
        public bool HasState(State state) => (currentState & state) != 0;

        /// <summary>
        /// 現在の状態を取得
        /// </summary>
        public State GetState() => currentState;

        /// <summary>
        /// すべての状態をクリア（NOT_STATEにリセット）
        /// </summary>
        public void ClearState() => currentState = State.NOT_STATE;
        #endregion

        #region ===== 基底メソッド =====
        /// <summary>
        /// 各コントローラーの Start 相当処理
        /// - 継承先で必要に応じてオーバーライドして使用
        /// </summary>
        public virtual void OnStart() { }

        /// <summary>
        /// 各コントローラーの Update 相当処理
        /// - 継承先で必要に応じてオーバーライドして使用
        /// </summary>
        public virtual void OnUpdate() { }
        #endregion
    }
}
