using Game.GameSystem;
using UnityEngine;

namespace Game.StageScene.Magnet
{
    /// <summary>
    /// 弾の方向表示ラインを制御するクラス
    /// - プレイヤーの位置を起点にラインを描画
    /// - 入力に応じて方向を更新
    /// </summary>
    public class BulletLineController : MonoBehaviour
    {
        private static InputHandler _inputHandler;

        private Transform _playerTransform;     // プレイヤーのTransform
        private LineRenderer _lineRenderer;     // ラインレンダラー
        private static Vector3 _currentDirection = Vector3.zero;

        private void Start()
        {
            _inputHandler = InputHandler.Instance;

            // プレイヤーを探す
            var playerObj = GameObject.Find(GameConstants.PLAYER_OBJ);
            if (playerObj != null)
            {
                _playerTransform = playerObj.transform;
            }
            else
            {
                Debug.LogError("[BulletLineController] プレイヤーオブジェクトが見つかりません！");
            }

            _lineRenderer = GetComponent<LineRenderer>();
            if (_lineRenderer == null)
            {
                Debug.LogError("[BulletLineController] LineRenderer がアタッチされていません！");
            }
        }

        private void Update()
        {
            // プレイヤーが破壊されていたら処理をスキップ
            if (_playerTransform == null) return;

            // SHOOT 入力で表示、キャンセルで非表示
            if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT) &&
                !_inputHandler.IsActionPressing(InputConstants.Action.SHOOT_CANCEL))
            {
                gameObject.SetActive(true);
            }
            else
            {
                gameObject.SetActive(false);
            }

            // ラインの開始位置をプレイヤーの少し上に設定
            Vector3 start_point = _playerTransform.position + Vector3.up * 1.0f;

            float maxDistance = 10f;

            // 入力方向を取得
            _currentDirection = GetDirection();

            // Raycast で衝突判定
            if (Physics.Raycast(start_point, _currentDirection, out RaycastHit hit, maxDistance))
            {
                if (!hit.collider.isTrigger)
                {
                    _lineRenderer.SetPosition(0, start_point);
                    _lineRenderer.SetPosition(1, hit.point);
                }
                else
                {
                    _lineRenderer.SetPosition(0, start_point);
                    _lineRenderer.SetPosition(1, start_point + _currentDirection * maxDistance);
                }
            }
            else
            {
                _lineRenderer.SetPosition(0, start_point);
                _lineRenderer.SetPosition(1, start_point + _currentDirection * maxDistance);
            }
        }

        /// <summary>
        /// 入力から射撃方向を取得
        /// </summary>
        public static Vector3 GetDirection()
        {
            if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT_ANGLE, InputConstants.ActionVector.North)) return InputConstants.ActionVector.North;
            if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT_ANGLE, InputConstants.ActionVector.NorthEast)) return InputConstants.ActionVector.NorthEast;
            if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT_ANGLE, InputConstants.ActionVector.East)) return InputConstants.ActionVector.East;
            if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT_ANGLE, InputConstants.ActionVector.SouthEast)) return InputConstants.ActionVector.SouthEast;
            if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT_ANGLE, InputConstants.ActionVector.NorthWest)) return InputConstants.ActionVector.NorthWest;
            if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT_ANGLE, InputConstants.ActionVector.West)) return InputConstants.ActionVector.West;
            if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT_ANGLE, InputConstants.ActionVector.SouthWest)) return InputConstants.ActionVector.SouthWest;

            return _currentDirection; // 入力がない場合は前回の方向を維持
        }
    }
}
