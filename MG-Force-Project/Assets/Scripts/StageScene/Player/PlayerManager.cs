using System.Collections.Generic;
using UnityEngine;

namespace Game.StageScene.Player
{
    /// <summary>
    /// プレイヤー全体を統括管理するクラス
    /// - 各種 PlayerController（状態・移動・アニメーションなど）を一括管理している
    /// - Start() で必要なコントローラーを自動追加・初期化
    /// - Update() で毎フレーム各コントローラーの処理を実行
    /// </summary>
    public class PlayerManager : MonoBehaviour
    {
        // ===== フィールド =====
        /// <summary>
        /// プレイヤーに関連するすべてのコントローラーを格納するリスト。
        /// PlayerControllerBaseを継承したコンポーネントが格納されます。
        /// </summary>
        private List<PlayerControllerBase> playerControllers = new List<PlayerControllerBase>();

        /// <summary>
        /// プレイヤーがアクティブかどうか（falseの場合、Update処理を停止）。
        /// </summary>
        private bool _isActive = true;

        /// <summary>
        /// 現在位置を保持して精度補正に使用（PosAdjustmentが有効な場合）。
        /// </summary>
        private Vector3 _currentPos;

        // ===== Unityイベント =====
        private void Awake()
        {
            Debug.Log($"[PlayerManager] Awake開始 - GameObject={gameObject.name}");
        }

        private void Start()
        {
            Debug.Log($"[PlayerManager] Start開始 - GameObject={gameObject.name}");

            // プレイヤー制御用コンポーネントを自動的に追加
            // PlayerFallDetector:
            playerControllers.Add(gameObject.AddComponent<PlayerFallDetector>());
            
            // PlayerStateController: 入力と状態の管理
            Debug.Log("[PlayerManager] PlayerStateControllerを追加");
            playerControllers.Add(gameObject.AddComponent<PlayerStateController>());

            // PlayerMoveController: 物理的な移動処理
            Debug.Log("[PlayerManager] PlayerMoveControllerを追加");
            playerControllers.Add(gameObject.AddComponent<PlayerMoveController>());

            // PlayerAnimationController: アニメーションの制御
            Debug.Log("[PlayerManager] PlayerAnimationControllerを追加");
            playerControllers.Add(gameObject.AddComponent<PlayerAnimationController>());

            Debug.Log($"[PlayerManager] 合計 {playerControllers.Count} 個のコントローラーを追加しました");

            // --- 各コントローラーの初期化処理を実行 ---
            foreach (var controller in playerControllers)
            {
                if (controller == null)
                {
                    Debug.LogError("[PlayerManager] コントローラーがnullです！");
                    continue;
                }

                Debug.Log($"[PlayerManager] {controller.GetType().Name}.Initialize()を呼び出し");
                // PlayerControllerBase.Initialize()を呼び出し、共通フィールド（playerObject, playerTransformなど）を初期化
                controller.Initialize(gameObject);

                Debug.Log($"[PlayerManager] {controller.GetType().Name}.OnStart()を呼び出し");
                // 各コントローラー独自のStart処理（OnStart）を呼び出し
                controller.OnStart();
            }

            Debug.Log("[PlayerManager] Start完了");
        }

        private void Update()
        {
            // 非アクティブ状態では何も行わない
            if (!_isActive)
            {
                Debug.LogWarning("[PlayerManager] 非アクティブ状態のため処理をスキップ");
                return;
            }

            // 各コントローラーの更新処理（OnUpdate）を順に呼び出す
            foreach (var controller in playerControllers)
            {
                // コントローラーがnullでない、かつ有効（enabled）な場合のみ処理を実行
                if (controller == null || !controller.enabled)
                {
                    continue;
                }

                controller.OnUpdate();
            }

            // 座標誤差の補正を行いたい場合は有効化 
            //PosAdjustment();
        }

        private void OnEnable()
        {
            Debug.Log($"[PlayerManager] OnEnable - GameObject={gameObject.name}");
        }

        private void OnDisable()
        {
            Debug.Log($"[PlayerManager] OnDisable - GameObject={gameObject.name}");
        }

        // ===== 座標補正処理 =====
        /// <summary>
        /// プレイヤーの位置ベクトルを指定精度で丸めます。
        /// （浮動小数点誤差による微小なズレを防止するための処理）
        /// </summary>
        private void PosAdjustment()
        {
            _currentPos = transform.position;
            _currentPos.x = RoundToPrecision(_currentPos.x, 3);
            _currentPos.y = RoundToPrecision(_currentPos.y, 3);
            _currentPos.z = RoundToPrecision(_currentPos.z, 3);
            transform.position = _currentPos;
        }

        /// <summary>
        /// 指定した浮動小数点数を「小数点第precision位」まで丸めます。
        /// </summary>
        /// <param name="value">丸めたい値</param>
        /// <param name="precision">保持する小数点の桁数</param>
        /// <returns>丸め後の値</returns>
        private float RoundToPrecision(float value, int precision)
        {
            float factor = Mathf.Pow(10, precision);
            return Mathf.Round(value * factor) / factor;
        }
    }
}
