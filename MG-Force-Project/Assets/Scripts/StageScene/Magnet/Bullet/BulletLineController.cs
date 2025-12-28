using Game.GameSystem;
using UnityEngine;

namespace Game.StageScene.Magnet
{
    /// <summary>
    /// 弾の方向表示ラインを制御するクラス
    /// - プレイヤーの位置を起点にラインを描画
    /// - 入力に応じて方向を更新
    /// ※ Sceneビュー専用（Gameビューには表示されない）
    /// </summary>
    public class BulletLineController : MonoBehaviour
    {
        private static InputHandler _inputHandler;

        private Transform _playerTransform;     // プレイヤーのTransform
        private LineRenderer _lineRenderer;     // （※使用しないが既存構成維持）
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
                Debug.LogError("[BulletLineController] プレイヤーオブジェクトが見つかりません!");
            }

            _lineRenderer = GetComponent<LineRenderer>();
            if (_lineRenderer != null)
            {
                // ★ 修正：Gameビューに出ないよう常に無効
                _lineRenderer.enabled = false;
            }
        }

        private void Update()
        {
            // ★ 修正：LineRenderer は一切使わない
            if (_playerTransform == null) return;

            // 方向だけ更新（ロジックは維持）
            if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT) &&
                !_inputHandler.IsActionPressing(InputConstants.Action.SHOOT_CANCEL))
            {
                _currentDirection = GetDirection();
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Sceneビュー専用の方向ライン表示
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            if (_playerTransform == null || _inputHandler == null) return;

            if (!_inputHandler.IsActionPressing(InputConstants.Action.SHOOT)) return;

            Vector3 startPoint = _playerTransform.position + Vector3.up * 1.0f;
            Vector3 direction = _currentDirection == Vector3.zero ? Vector3.forward : _currentDirection;

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(startPoint, direction * 10f);
        }
#endif

        /// <summary>
        /// 入力から射撃方向を取得
        /// </summary>
        public static Vector3 GetDirection()
        {
            if (_inputHandler == null) return _currentDirection;

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
