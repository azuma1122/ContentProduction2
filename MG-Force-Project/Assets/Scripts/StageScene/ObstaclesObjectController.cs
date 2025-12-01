using Game.StageScene.Magnet;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.StageScene
{
    /// <summary>
    /// 障害物オブジェクトの動作を管理するクラス。
    /// 通常時：プレイヤーや「Moving」タグの物体と接触している間は動かない
    /// Boot起動時：磁力の影響を受けて動く
    /// </summary>
    public class ObstaclesObjectController : MagnetObjectManager
    {
        // オブジェクトが動ける状態かどうか（true=動ける / false=動けない）
        private bool _canMove;
        // Rigidbodyコンポーネント（物理演算・移動に関係する）
        private Rigidbody _rigidbody;

        protected override void Start()
        {
            // 親クラスのStart()を呼び出してMyDataなどを初期化
            base.Start();

            // ゲーム開始時にRigidbodyを取得して記憶しておく
            _rigidbody = GetComponent<Rigidbody>();

            if (_rigidbody == null)
            {
                Debug.LogError($"[ObstaclesObjectController] {name} に Rigidbody が見つかりません！");
            }
            else
            {
                // 磁力で動かすためにisKinematicをfalseに設定
                _rigidbody.isKinematic = false;
                Debug.Log($"[ObstaclesObjectController] {name} の isKinematic を false に設定");
            }

            _canMove = true;

            // magnetManager が null の場合は自動取得
            if (magnetManager == null)
            {
                magnetManager = FindObjectOfType<MagnetManager>();
                if (magnetManager == null)
                {
                    Debug.LogError($"[ObstaclesObjectController] {name} で MagnetManager が見つかりません！");
                }
                else
                {
                    Debug.Log($"[ObstaclesObjectController] {name} で MagnetManager を取得しました");
                }
            }

            // magnetController が null の場合は初期化
            if (magnetController == null)
            {
                magnetController = new MagnetController();
                Debug.Log($"[ObstaclesObjectController] {name} で MagnetController を初期化しました");
            }

            // MyDataの内容をログ出力
            if (MyData != null)
            {
                Debug.Log($"[ObstaclesObjectController] {name} 初期化完了 - ObjectType={MyData.MyObjectType}, MagnetType={MyData.MyMangetType}, Power={MyData.MyMagnetPower}");
            }
            else
            {
                Debug.LogError($"[ObstaclesObjectController] {name} 初期化失敗 - MyDataがnullです。Inspectorで設定を確認してください。");
            }
        }

        protected override void Update()
        {
            base.Update();

            if (_rigidbody == null) return;

            // Boot起動中かどうかで処理を分岐
            if (magnetManager != null && magnetManager.IsMagnetBoot)
            {
                // Boot起動中：磁力で動く
                HandleMagneticMovement();
            }
            else
            {
                // Boot非起動時：通常の制約処理
                HandleNormalMovement();
            }
        }

        /// <summary>
        /// 磁力起動時の移動処理
        /// </summary>
        private void HandleMagneticMovement()
        {
            // 磁力起動中は基本的な制約のみ適用
            SetDefultConstraints();

            // 速度制限（上下左右のみ）
            RestrictVelocityToFourDirections();
        }

        /// <summary>
        /// 通常時の移動処理
        /// </summary>
        private void HandleNormalMovement()
        {
            // 現在の状態によってRigidbodyの制約（Constraints）を切り替える
            if (_canMove)
            {
                // 通常時の動きが可能な制約
                SetDefultConstraints();
            }
            else
            {
                // Player または Moving と接触中の制約（動かなくする）
                SetHitPlayerConstraints();
            }

            // 重要：通常時は必ず速度を0にして静止させる
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        /// <summary>
        /// 速度を上下左右のみに制限する
        /// </summary>
        private void RestrictVelocityToFourDirections()
        {
            Vector3 velocity = _rigidbody.velocity;

            // X軸とY軸の絶対値を比較して、大きい方向のみを残す
            if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y))
            {
                // 左右移動のみ
                _rigidbody.velocity = new Vector3(velocity.x, 0f, 0f);
            }
            else
            {
                // 上下移動のみ
                _rigidbody.velocity = new Vector3(0f, velocity.y, 0f);
            }
        }

        /// <summary>
        /// Player または Moving タグを持つ物体かどうかを判定する
        /// </summary>
        private bool IsPusher(GameObject obj)
        {
            return
                obj.CompareTag("Player") ||  // プレイヤー
                obj.CompareTag("Moving");    // Moving タグ
        }

        /// <summary>
        /// 何かと衝突した瞬間に呼ばれるメソッド
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            // Boot起動中は通常の衝突判定を無視
            if (magnetManager != null && magnetManager.IsMagnetBoot)
            {
                return;
            }

            // Player でも Moving でもなければ終了
            if (!IsPusher(collision.gameObject)) return;

            // 押されたら動かなくする
            _canMove = false;
        }

        /// <summary>
        /// 衝突していた物体が離れた時に呼ばれるメソッド
        /// </summary>
        private void OnCollisionExit(Collision collision)
        {
            // Boot起動中は通常の衝突判定を無視
            if (magnetManager != null && magnetManager.IsMagnetBoot) return;

            // Player でも Moving でもなければ終了
            if (!IsPusher(collision.gameObject)) return;

            // 固定状態なら無視
            if (magnetFixed) return;

            // 離れたら再び動けるようにする
            _canMove = true;
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
            // 完全に固定
            _rigidbody.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;
        }
    }
}