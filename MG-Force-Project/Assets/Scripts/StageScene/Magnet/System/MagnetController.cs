using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.StageScene.Magnet
{
    /// <summary>
    /// 磁力動作管理クラス
    /// </summary>
    public class MagnetController
    {
        private const int FORWARD = 1;   // 正の値
        private const int REVERSE = -1;  // 負の値
        private MagnetData _selfData;     // 自分のデータ
        private MagnetData _otherData;  // 相手のデータ
        // 向きが水平かどうか
        private bool _isDirHorizontal;
        private float _horizontal;  // 水平
        private float _vertical;    // 垂直
        private float _direction;   // 向き

        /// <summary>
        /// 磁力の動作更新処理
        /// </summary>
        /// <param name="self"></param>
        /// <param name="other"></param>
        public void MagnetUpdate(GameObject self, GameObject other)
        {
            // Null チェック
            if (self == null || other == null)
            {
                Debug.LogError("[MagnetController] self または other が null です");
                return;
            }

            // データの取得
            MagnetObjectManager selfManager = self.GetComponent<MagnetObjectManager>();
            MagnetObjectManager otherManager = other.GetComponent<MagnetObjectManager>();

            if (selfManager == null)
            {
                Debug.LogError($"[MagnetController] {self.name} に MagnetObjectManager が見つかりません");
                return;
            }

            if (otherManager == null)
            {
                Debug.LogError($"[MagnetController] {other.name} に MagnetObjectManager が見つかりません");
                return;
            }

            _selfData = selfManager.MyData;
            _otherData = otherManager.MyData;

            if (_selfData == null)
            {
                Debug.LogError($"[MagnetController] {self.name} の MyData が null です");
                return;
            }

            if (_otherData == null)
            {
                Debug.LogError($"[MagnetController] {other.name} の MyData が null です");
                return;
            }

            // 向きを決める
            _horizontal = self.transform.position.x - other.transform.position.x;
            _vertical = self.transform.position.y - other.transform.position.y;

            // 大きいほうを向きの方向とする
            _isDirHorizontal = Mathf.Abs(_horizontal) > Mathf.Abs(_vertical);
            _direction = (_isDirHorizontal) ? _horizontal : _vertical;

            // 値の増減を決定
            float reverse = (_direction > 0) ? FORWARD : REVERSE;
            Rigidbody rigidbody = self.GetComponent<Rigidbody>();

            if (rigidbody == null)
            {
                Debug.LogError($"[MagnetController] {self.name} に Rigidbody が見つかりません");
                return;
            }

            // 移動処理
            if (_selfData.MyMangetType == _otherData.MyMangetType)
            {
                rigidbody.AddForce(MagnetMove(reverse), ForceMode.Force);
            }
            else
            {
                rigidbody.AddForce(MagnetMove(-reverse), ForceMode.Force);
            }
        }

        /// <summary>
        /// 磁力の移動処理
        /// </summary>
        /// <param name="reverse"></param>
        /// <returns></returns>
        private Vector3 MagnetMove(float reverse)
        {
            Vector3 magnet_move = new Vector3(0, 0, 0);
            if (_isDirHorizontal)
            {
                magnet_move.x = (int)_selfData.MyMagnetPower * (int)_otherData.MyMagnetPower * reverse;
            }
            else
            {
                magnet_move.y = (int)_selfData.MyMagnetPower * (int)_otherData.MyMagnetPower * reverse;
            }
            return magnet_move;
        }
    }
}