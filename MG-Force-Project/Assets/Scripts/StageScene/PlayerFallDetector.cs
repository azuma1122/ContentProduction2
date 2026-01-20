using UnityEngine;
using UnityEngine.SceneManagement;
using Game.GameSystem;

namespace Game.StageScene.Player
{
    /// <summary>
    /// プレイヤーの落下検知とシーンリセット処理
    /// - Y座標が一定値以下になったら落下と判定
    /// - 操作を無効化して待機後、SceneLoaderを使ってリロード
    /// </summary>
    public class PlayerFallDetector : PlayerControllerBase
    {
        #region ===== 設定 =====
        [Header("落下判定設定")]
        [SerializeField] private float _fallThresholdY = -10f; // この高さ以下で落下判定
        [SerializeField] private float _resetDelay = 0.5f;     // リセットまでの待機時間（秒）
        #endregion

        #region ===== 内部変数 =====
        private bool _isFalling = false; // 落下中フラグ
        private float _fallTimer = 0f;   // 落下後の経過時間
        private SceneLoader _sceneLoader;
        #endregion

        /// <summary>
        /// 初期化処理
        /// </summary>
        public override void OnStart()
        {
            _isFalling = false;
            _fallTimer = 0f;

            // SceneLoaderを取得
            _sceneLoader = SceneLoader.Instance;

            Debug.Log($"[FallDetector] 初期化完了 - 落下判定Y座標: {_fallThresholdY}");
        }

        /// <summary>
        /// 毎フレーム更新処理
        /// </summary>
        public override void OnUpdate()
        {
            // ゴール状態の場合はスキップ
            if (HasState(State.GOAL))
                return;

            // 既に落下処理中の場合
            if (_isFalling)
            {
                _fallTimer += Time.deltaTime;

                // 待機時間経過後にリセット実行
                if (_fallTimer >= _resetDelay)
                {
                    ResetStage();
                }
                return;
            }

            // Y座標チェック：閾値以下なら落下判定
            if (playerTransform.position.y < _fallThresholdY)
            {
                OnFallDetected();
            }
        }

        /// <summary>
        /// 落下検知時の処理
        /// </summary>
        private void OnFallDetected()
        {
            _isFalling = true;
            _fallTimer = 0f;
            SEManager.instance.PlaySE(SEManager.Player.PLAYER_FALLED);
            Debug.Log($"[FallDetector] 落下検知！ Y座標: {playerTransform.position.y}");

            // プレイヤーの操作を無効化
            DisablePlayerControl();
        }

        /// <summary>
        /// プレイヤー操作を無効化
        /// </summary>
        private void DisablePlayerControl()
        {
            // 全ての状態をクリア
            ClearState();

            // Rigidbodyの操作を停止
            var rb = playerObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.isKinematic = true;
            }

            // プレイヤーコントローラーを無効化
            var moveController = playerObject.GetComponent<PlayerMoveController>();
            if (moveController != null)
            {
                moveController.enabled = false;
            }

            var stateController = playerObject.GetComponent<PlayerStateController>();
            if (stateController != null)
            {
                stateController.enabled = false;
            }
        }

        /// <summary>
        /// ステージをリセット（SceneLoaderを使用）
        /// </summary>
        private void ResetStage()
        {
            Debug.Log("[FallDetector] ステージリセット開始");

            // ゲーム状態を強制リセット
            ForceResumeAll();

            // 現在のシーン名を取得
            string currentSceneName = SceneManager.GetActiveScene().name;

            Debug.Log($"[FallDetector] SceneLoaderでシーンをリロード: {currentSceneName}");

            // SceneLoaderを使用してロード画面経由で遷移
            if (_sceneLoader != null)
            {
                _sceneLoader.LoadScene(currentSceneName);
            }
            else
            {
                // SceneLoaderが利用できない場合は直接ロード
                Debug.LogWarning("[FallDetector] SceneLoaderが見つからないため直接ロードします");
                SceneManager.LoadScene(currentSceneName);
            }
        }

        /// <summary>
        /// ゲーム状態を強制的にリセット
        /// </summary>
        private void ForceResumeAll()
        {
            Debug.Log("[FallDetector] ゲーム状態をリセット");

            // Time.timeScaleをリセット
            Time.timeScale = 1f;

            // 物理シミュレーションもリセット
            Physics.simulationMode = SimulationMode.FixedUpdate;
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;

            Debug.Log($"[FallDetector] Time.timeScale={Time.timeScale}, Physics={Physics.simulationMode}");
        }

#if UNITY_EDITOR
        /// <summary>
        /// Sceneビューで落下判定ラインを可視化
        /// </summary>
        //private void OnDrawGizmos()
        //{
        //    // 落下判定ラインを赤色で表示
        //    Gizmos.color = Color.red;
        //    Vector3 center = transform.position;
        //    center.y = _fallThresholdY;

        //    // 横線を描画（視認しやすくするため）
        //    Gizmos.DrawLine(
        //        center + Vector3.left * 50f,
        //        center + Vector3.right * 50f
        //    );
        //    Gizmos.DrawLine(
        //        center + Vector3.back * 50f,
        //        center + Vector3.forward * 50f
        //    );
        //}
#endif
    }
}