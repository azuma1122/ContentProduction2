using System.Collections.Generic;
using UnityEngine;

namespace Game.StageScene.Player
{
    /// <summary>
    /// プレイヤー全体を統括管理するクラス
    /// - 各種 PlayerController（状態・移動・アニメーションなど）を一括管理
    /// - Start() で必要なコントローラーを自動追加・初期化
    /// - Update() で毎フレーム各コントローラーの処理を実行
    /// - 座標の浮動小数点誤差を補正（PosAdjustment）
    /// </summary>
    public class PlayerManager : MonoBehaviour
    {
        // ===== フィールド =====
        /// <summary>
        /// プレイヤーに関連するすべてのコントローラーを格納するリスト
        /// </summary>
        private List<PlayerControllerBase> playerControllers = new List<PlayerControllerBase>();

        /// <summary>
        /// プレイヤーがアクティブかどうか（falseの場合、Update処理を停止）
        /// </summary>
        private bool _isActive = true;

        /// <summary>
        /// 現在位置を保持して精度補正に使用
        /// </summary>
        private Vector3 _currentPos;


        // ===== Unityイベント =====
        private void Start()
        {
            // プレイヤー制御用コンポーネントを自動的に追加
            // 状態管理
            playerControllers.Add(gameObject.AddComponent<PlayerStateController>());
            // 移動制御
            playerControllers.Add(gameObject.AddComponent<PlayerMoveController>());
            // アニメーション制御
            playerControllers.Add(gameObject.AddComponent<PlayerAnimationController>());

            // --- 各コントローラーの初期化処理を実行 ---
            foreach (var controller in playerControllers)
            {
                // ゲームオブジェクト情報を渡して初期化
                controller.Initialize(gameObject);
                // 各コントローラー独自のStart処理を呼び出し
                controller.OnStart();
            }
        }

        private void Update()
        {
            // 非アクティブ状態では何も行わない
            if (!_isActive) return;

            // 各コントローラーの更新処理を順に呼び出す
            foreach (var controller in playerControllers)
            {
                controller.OnUpdate();
            }

            // 座標誤差の補正を行いたい場合は有効化 
            //PosAdjustment();
        }


        // ===== 座標補正処理 =====

        /// <summary>
        /// プレイヤーの位置ベクトルを指定精度で丸める
        /// （浮動小数点誤差による微小なズレを防止）
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
        /// 指定した浮動小数点数を「小数点第precision位」まで丸める
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
