using Game.GameSystem;
using UnityEngine;

namespace Game.StageScene.Camera
{
    /// <summary>
    /// プレイヤーを追尾するカメラクラス
    /// </summary>
    public class PlayerViewCameraController : MonoBehaviour
    {
        // 左下の頂点座標（カメラ移動制限用）
        private Vector3 lowerLeft = GameConstants.LowerLeftCamera;
        // 右上の頂点座標（カメラ移動制限用）
        private Vector3 topRight = GameConstants.TopRightCamera;
        // 現在のプレイヤーの Transform
        private Transform currentPlayerTransform;
        // プレイヤー追尾スピード
        [SerializeField]
        private float followSpeed = 5.0f;
        // プレイヤーとの Y 軸の差分
        private const float Y_DIFF_TO_PLAYER = 1.0f;
        // カメラの高さオフセット（この値を変更してカメラの高さを調整）
        private const float CAMERA_HEIGHT_OFFSET = 3.0f;

        /// <summary>
        /// 初期化処理（Start時にプレイヤーを探す）
        /// </summary>
        private void Start()
        {
            TryFindPlayer();
        }

        /// <summary>
        /// 毎フレーム更新
        /// - プレイヤーが null の場合は再取得
        /// - カメラ追尾
        /// </summary>
        private void Update()
        {
            // Player が null または破壊されている場合は再取得
            if (currentPlayerTransform == null)
            {
                TryFindPlayer();
                if (currentPlayerTransform == null) return; // Player がまだいなければ追尾しない
            }
            // Player を追尾
            TrackThePlayer();
        }

        /// <summary>
        /// プレイヤーの Transform を探す
        /// </summary>
        private void TryFindPlayer()
        {
            GameObject player = GameObject.Find(GameConstants.PLAYER_OBJ);
            if (player != null)
            {
                currentPlayerTransform = player.transform;
            }
            else
            {
                Debug.LogWarning("[PlayerViewCameraController] プレイヤーが見つかりません。");
            }
        }

        /// <summary>
        /// プレイヤーを追尾してカメラ位置を更新
        /// </summary>
        private void TrackThePlayer()
        {
            // X軸とY軸を制限範囲内に Clamp（Y軸にオフセットを追加）
            float target_x = Mathf.Clamp(currentPlayerTransform.position.x, lowerLeft.x, topRight.x);
            float target_y = Mathf.Clamp(currentPlayerTransform.position.y + CAMERA_HEIGHT_OFFSET, lowerLeft.y, topRight.y);

            // Y軸差分が大きい場合のみ調整してスムーズに追尾
            if (Mathf.Abs(currentPlayerTransform.position.y - transform.position.y) > Y_DIFF_TO_PLAYER)
            {
                target_y = Mathf.Clamp(currentPlayerTransform.position.y + CAMERA_HEIGHT_OFFSET +
                                       (currentPlayerTransform.position.y - transform.position.y),
                                       lowerLeft.y, topRight.y);
            }

            // カメラの Z 軸は固定
            Vector3 target_pos = new Vector3(target_x, target_y, transform.position.z);

            // Lerp でスムーズ追尾
            transform.position = Vector3.Lerp(transform.position, target_pos, followSpeed * Time.deltaTime);
        }
    }
}