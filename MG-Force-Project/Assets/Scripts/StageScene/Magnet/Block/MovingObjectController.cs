using System.Collections.Generic;
using UnityEngine;

namespace Game.StageScene.Magnet
{
    /// <summary>
    /// 可動オブジェクトクラス
    /// - 磁力に反応して移動可能なオブジェクト
    /// - プレイヤーとの接触で動作制御
    /// - 上下左右のみの移動制限
    /// </summary>
    public class MovingObjectController : MagnetObjectManager
    {
        // ===== 内部変数 =====
        private bool _canMove;                     // 動作可能か
        private Rigidbody _rigitbody;              // Rigidbodyコンポーネント
        private List<Collider> _isHitMagnet = new(); // 磁力範囲に入っているコライダーのリスト

        /// <summary>
        /// 初期化処理
        /// </summary>
        protected override void Start()
        {
            base.Start();

            // Rigidbody を取得
            _rigitbody = GetComponent<Rigidbody>();
            if (_rigitbody == null)
                Debug.LogError("[MovingObjectController] Rigidbody が見つかりません！");

            _rigitbody.isKinematic = true;// Kinematicをする前提

            _canMove = true;

            // magnetManager が null の場合は自動取得
            if (magnetManager == null)
            {
                magnetManager = FindObjectOfType<MagnetManager>();
                if (magnetManager == null)
                    Debug.LogError("[MovingObjectController] MagnetManager が見つかりません！");
            }

            // magnetController が null の場合は初期化
            if (magnetController == null)
            {
                magnetController = new MagnetController();
                Debug.Log($"[MovingObjectController] {gameObject.name} で MagnetController を初期化しました");
            }
        }

        protected override void Update()
        {
            base.Update();

            // Rigidbody がない場合は処理をスキップ
            if (_rigitbody == null) return;

            // 動作可能な場合の制御
            if (_canMove)
            {
                SetDefultConstraints();

                // magnetManager が null または磁力が起動していない場合は停止
                if (magnetManager == null || !magnetManager.IsMagnetBoot)
                {
                    _rigitbody.velocity = Vector3.zero;
                }
                else
                {
                    // 磁力起動中は速度を上下左右のみに制限
                    RestrictVelocityToFourDirections();
                }
            }
            else
            {
                // プレイヤーと当たった場合の制約
                SetHitPlayerConstraints();
            }
        }

        /// <summary>
        /// 速度を上下左右のみに制限する
        /// </summary>
        private void RestrictVelocityToFourDirections()
        {
            Vector3 velocity = _rigitbody.velocity;

            // X軸とY軸の絶対値を比較して、大きい方向のみを残す
            if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y))
            {
                // 左右移動のみ
                _rigitbody.velocity = new Vector3(velocity.x, 0f, 0f);
            }
            else
            {
                // 上下移動のみ
                _rigitbody.velocity = new Vector3(0f, velocity.y, 0f);
            }
        }

        #region -------- 判定処理 --------

        /// <summary>
        /// プレイヤーと衝突した時の処理
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            if (magnetManager != null && magnetManager.IsMagnetBoot) return;

            // プレイヤー以外は無視
            if (!collision.gameObject.CompareTag(GameConstants.Tag.PLAYER.ToString())) return;

            _canMove = false; // プレイヤーと接触中は動かない
        }

        /// <summary>
        /// プレイヤーから離れた時の処理
        /// </summary>
        private void OnCollisionExit(Collision collision)
        {
            if (magnetManager != null && magnetManager.IsMagnetBoot) return;

            if (!collision.gameObject.CompareTag(GameConstants.Tag.PLAYER.ToString())) return;

            if (magnetFixed) return; // 固定状態なら無視

            _canMove = true; // プレイヤーが離れたら再度動作可能
        }

        /// <summary>
        /// 磁力範囲に入った時の処理
        /// </summary>
        protected override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);

            if (other.gameObject.layer != (int)GameConstants.Layer.MAGNET_RANGE) return;

            // 磁力範囲リストに追加
            if (!_isHitMagnet.Contains(other))
                _isHitMagnet.Add(other);
        }

        /// <summary>
        /// 磁力範囲内にいる間の処理
        /// </summary>
        private void OnTriggerStay(Collider other)
        {
            // magnetManager が null か磁力が起動していなければ終了
            if (magnetManager == null || !magnetManager.IsMagnetBoot) return;

            // 対象が磁力オブジェクトでない場合は終了
            if (other.gameObject.layer != (int)GameConstants.Layer.N_MAGNET &&
                other.gameObject.layer != (int)GameConstants.Layer.S_MAGNET) return;

            // このオブジェクトが可動オブジェクトの場合のみ磁力処理
            if (MyData.MyObjectType == GameConstants.Tag.MOVING)
            {
                // magnetController が null なら初期化
                if (magnetController == null)
                {
                    magnetController = new MagnetController();
                    Debug.LogWarning($"[MovingObjectController] {gameObject.name} の magnetController が null だったため、初期化しました");
                }

                // 相手のオブジェクトのデータが初期化されているか確認
                var otherManager = other.gameObject.GetComponent<MagnetObjectManager>();
                if (otherManager == null)
                {
                    Debug.LogWarning($"[MovingObjectController] {other.gameObject.name} に MagnetObjectManager が見つかりません");
                    return;
                }

                if (otherManager.MyData == null)
                {
                    // データが初期化されていない場合はスキップ（エラーログは出さない）
                    return;
                }

                magnetController.MagnetUpdate(gameObject, other.gameObject);
            }
        }

        /// <summary>
        /// 磁力範囲から出た時の処理
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer != (int)GameConstants.Layer.MAGNET_RANGE) return;

            if (_isHitMagnet.Contains(other))
                _isHitMagnet.Remove(other);

            // 磁力範囲に誰もいない場合は停止
            if (_isHitMagnet.Count == 0 && _rigitbody != null)
                _rigitbody.velocity = Vector3.zero;
        }

        #endregion

        /// <summary>
        /// デフォルトのRigidbody制約
        /// </summary>
        private void SetDefultConstraints()
        {
            _rigitbody.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        }

        /// <summary>
        /// プレイヤーと接触中の制約(必要ならここで固定)
        /// </summary>
        private void SetHitPlayerConstraints()
        {
            //_rigitbody.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;
        }
    }
}