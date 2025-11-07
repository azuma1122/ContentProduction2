using Game.StageScene.Magnet;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.StageScene
{
    /// <summary>
    /// 障害物オブジェクトの動作を管理するクラス。
    /// プレイヤーと接触している間は動かなくなり、
    /// 離れたら再び動けるようにする処理を行う。
    /// </summary>
    public class obstaclesObjectController : MonoBehaviour
    {
        // オブジェクトが動ける状態かどうか（true=動ける / false=動けない）
        private bool _canMove;

        // Rigidbodyコンポーネント（物理演算・移動に関係する）
        private Rigidbody _rigidbody;

        private void Start()
        {
            // ゲーム開始時にRigidbodyを取得して記憶しておく
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            // 現在の状態によってRigidbodyの制約（Constraints）を切り替える
            if (_canMove)
            {
                // 通常時の動きが可能な制約
                SetDefultConstraints();
            }
            else
            {
                // プレイヤーと接触中の制約（動かなくする）
                SetHitPlayerConstraints();
            }

            // 毎フレーム、速度を0にリセットして物理的な移動を止める
            _rigidbody.velocity = Vector3.zero;
        }

        /// <summary>
        /// 何かと衝突した瞬間に呼ばれるメソッド
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            // プレイヤー以外との接触なら何もしないで終了
            if (!collision.gameObject.CompareTag(GameConstants.Tag.PLAYER.ToString())) return;

            // プレイヤーと当たったら動かなくする
            _canMove = false;
        }

        /// <summary>
        /// 衝突していた物体が離れた時に呼ばれるメソッド
        /// </summary>
        private void OnCollisionExit(Collision collision)
        {
            // プレイヤー以外なら何もしない
            if (!collision.gameObject.CompareTag(GameConstants.Tag.PLAYER.ToString())) return;

            // プレイヤーが離れたら、再び動けるようにする
            _canMove = true;
        }

        /// <summary>
        /// デフォルト時のRigidbody制約（基本的な動きのみ許可）
        /// Z方向の移動とZ回転だけを固定して、その他は自由に動ける。
        /// </summary>
        private void SetDefultConstraints()
        {
            _rigidbody.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationZ;
        }

        /// <summary>
        /// プレイヤーと接触している間の制約（完全に固定する想定）
        /// ※現在はコメントアウトされており動作していない
        /// </summary>
        private void SetHitPlayerConstraints()
        {
            // すべての移動・回転を固定したい場合はこちらを使用
            // _rigidbody.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;
        }
    }
}
