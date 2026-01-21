using Game.StageScene.Magnet;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Game.StageScene
{
    /// <summary>
    /// 障害物オブジェクトの動作を管理するクラス。
    /// 通常時：プレイヤーや「Moving」タグの物体と接触している間は動かない
    /// Boot起動時：磁力の影響を受けて動く（上下左右のみ）
    /// </summary>
    public class ObstaclesObjectController : MagnetObjectManager
    {
        // オブジェクトが動ける状態かどうか（true=動ける / false=動けない）
        private bool _canMove;
        // Rigidbodyコンポーネント（物理演算・移動に関係する）
        private Rigidbody _rigidbody;
        // 磁力範囲に入っているコライダーのリスト
        private List<Collider> _isHitMagnet = new();

        // Unity Inspectorで設定する磁力データ
        [Header("障害物磁力設定")]
        [SerializeField] private bool isFixedMagnet = true;
        [SerializeField] private MagnetData.MagnetPower fixedPower = MagnetData.MagnetPower.Weak;

        [Header("移動制限設定")]
        [SerializeField] private bool restrictToFourDirections = true;
        [SerializeField] private float directionThreshold = 0.3f;

        // 初期化完了フラグ
        private bool _isInitialized = false;

        protected override void Start()
        {
            // ===== 先にmagnetFixedを設定 =====
            magnetFixed = isFixedMagnet;
            magnetFixedPower = fixedPower;

            // 親クラスのStart()を呼び出してMyDataを初期化
            base.Start();

            // ===== MyDataがnullの場合は手動で初期化 =====
            if (MyData == null)
            {
                string objectType = gameObject.tag;
                MagnetData.MagnetType magnetType = (MagnetData.MagnetType)gameObject.layer;
                MyData = new MagnetData(objectType, magnetType, fixedPower);
            }

            // ===== MagnetManagerを自動取得 =====
            if (magnetManager == null)
            {
                magnetManager = FindObjectOfType<MagnetManager>();
            }

            // ===== MagnetControllerを自動初期化 =====
            if (magnetController == null)
            {
                magnetController = new MagnetController();
            }

            // ===== Rigidbodyの取得と設定 =====
            _rigidbody = GetComponent<Rigidbody>();

            if (_rigidbody != null)
            {
                // 磁力で動かすためにisKinematicをfalseに設定
                _rigidbody.isKinematic = false;

                // 物理演算の最適化設定
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
                _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

                // 初期状態で完全停止
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }

            _canMove = true;
            _isInitialized = true;
        }

        protected override void Update()
        {
            base.Update();

            if (_rigidbody == null || !_isInitialized) return;

            // ===== MagnetManagerが途中でnullになった場合の自動再取得 =====
            if (magnetManager == null)
            {
                magnetManager = FindObjectOfType<MagnetManager>();
                if (magnetManager == null) return;
            }

            // Boot起動中かどうかで処理を分岐
            if (magnetManager.IsMagnetBoot)
            {
                HandleMagneticMovement();
            }
            else
            {
                HandleNormalMovement();
            }
        }

        /// <summary>
        /// 磁力起動時の移動処理
        /// </summary>
        private void HandleMagneticMovement()
        {
            SetDefultConstraints();

            if (restrictToFourDirections)
            {
                RestrictVelocityToFourDirections();
            }
        }

        /// <summary>
        /// 通常時の移動処理
        /// </summary>
        private void HandleNormalMovement()
        {
            if (_canMove)
            {
                SetDefultConstraints();
            }
            else
            {
                SetHitPlayerConstraints();
            }

            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        /// <summary>
        /// 速度を上下左右のみに制限する
        /// </summary>
        private void RestrictVelocityToFourDirections()
        {
            Vector3 velocity = _rigidbody.velocity;
            float absX = Mathf.Abs(velocity.x);
            float absY = Mathf.Abs(velocity.y);

            if (absX < 0.01f && absY < 0.01f)
            {
                return;
            }

            float total = absX + absY;
            float xRatio = absX / total;
            float yRatio = absY / total;

            if (xRatio > (1f - directionThreshold))
            {
                _rigidbody.velocity = new Vector3(velocity.x, 0f, 0f);
            }
            else if (yRatio > (1f - directionThreshold))
            {
                _rigidbody.velocity = new Vector3(0f, velocity.y, 0f);
            }
            else
            {
                if (absX > absY)
                {
                    _rigidbody.velocity = new Vector3(velocity.x, 0f, 0f);
                }
                else
                {
                    _rigidbody.velocity = new Vector3(0f, velocity.y, 0f);
                }
            }
        }

        /// <summary>
        /// Player または Moving タグを持つ物体かどうかを判定する
        /// </summary>
        private bool IsPusher(GameObject obj)
        {
            return obj.CompareTag("Player") || obj.CompareTag("Moving");
        }

        /// <summary>
        /// 何かと衝突した瞬間に呼ばれるメソッド
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            if (magnetManager != null && magnetManager.IsMagnetBoot)
            {
                return;
            }

            if (!IsPusher(collision.gameObject)) return;

            _canMove = false;
        }

        /// <summary>
        /// 衝突していた物体が離れた時に呼ばれるメソッド
        /// </summary>
        private void OnCollisionExit(Collision collision)
        {
            if (magnetManager != null && magnetManager.IsMagnetBoot) return;

            if (!IsPusher(collision.gameObject)) return;

            if (magnetFixed) return;

            _canMove = true;
        }

        /// <summary>
        /// 磁力範囲に入った時の処理
        /// </summary>
        protected override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);

            if (other.gameObject.layer != (int)GameConstants.Layer.MAGNET_RANGE) return;

            if (!_isHitMagnet.Contains(other))
            {
                _isHitMagnet.Add(other);
            }
        }

        /// <summary>
        /// 磁力範囲内にいる間の処理
        /// </summary>
        private void OnTriggerStay(Collider other)
        {
            // 初期化完了まで磁力処理をスキップ
            if (!_isInitialized) return;

            if (magnetManager == null || !magnetManager.IsMagnetBoot) return;

            if (other.gameObject.layer != (int)GameConstants.Layer.N_MAGNET &&
                other.gameObject.layer != (int)GameConstants.Layer.S_MAGNET) return;

            if (magnetController == null)
            {
                magnetController = new MagnetController();
            }

            var otherManager = other.gameObject.GetComponent<MagnetObjectManager>();
            if (otherManager == null) return;

            if (otherManager.MyData == null) return;

            magnetController.MagnetUpdate(gameObject, other.gameObject);
        }

        /// <summary>
        /// 磁力範囲から出た時の処理
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer != (int)GameConstants.Layer.MAGNET_RANGE) return;

            if (_isHitMagnet.Contains(other))
            {
                _isHitMagnet.Remove(other);
            }

            if (_isHitMagnet.Count == 0 && _rigidbody != null)
            {
                _rigidbody.velocity = Vector3.zero;
            }
        }

        /// <summary>
        /// デフォルト時のRigidbody制約（基本的な動きのみ許可）
        /// Z方向の移動と回転を固定
        /// </summary>
        private void SetDefultConstraints()
        {
            _rigidbody.constraints =
                RigidbodyConstraints.FreezePositionZ |
                RigidbodyConstraints.FreezeRotation;
        }

        /// <summary>
        /// Player または Moving と接触している間の制約
        /// </summary>
        private void SetHitPlayerConstraints()
        {
            _rigidbody.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;
        }
    }
}