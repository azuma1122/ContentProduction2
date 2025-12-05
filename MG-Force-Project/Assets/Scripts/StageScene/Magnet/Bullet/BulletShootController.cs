using Game.GameSystem;
using Game.StageScene.Player;
using UnityEngine;
using UnityEngine.UI;
using static Game.StageScene.Player.PlayerControllerBase;

namespace Game.StageScene.Magnet
{
    /// <summary>
    /// プレイヤーのチャージ式射撃処理
    /// - マウスクリック / 入力システム対応
    /// - チャージに応じた射撃エフェクトとUI連動
    /// - 磁力モード中は射撃不可
    /// - 弾が衝突すると、現在の磁極に応じたPrefab（N/Sブロック）を生成
    /// - チャージレベルに応じてMovingブロックの磁力付与可能範囲を変更
    /// </summary>
    public class BulletShootController : MonoBehaviour
    {
        private const float ADD_POWER = 0.1f; // 1フレームあたりのチャージ増加量

        // ======= 内部状態 =======
        private bool _isCharging;
        private bool _canShooting;
        private float _currentPower;

        // ======= 外部アクセス用プロパティ =======
        public bool IsCharging => _isCharging;
        public bool IsShooting => _canShooting;
        public float CurrentChargePower => _currentPower; // チャージ量を外部から取得可能に

        // ======= コンポーネント参照 =======
        private InputHandler _inputHandler;
        private MagnetManager _magnet;
        private PlayerStateController _playerState;
        private MagnetUIManager _uiManager;
        private UnityEngine.Camera _mainCamera;

        // ======= UI・エフェクト関連 =======
        [Header("=== UI & エフェクト ===")]
        [SerializeField] private GameObject _chargeGageObj;   // チャージUI親
        [SerializeField] private Image _chargeGage;           // チャージゲージ
        [SerializeField] private GameObject _powerEffectObj;  // チャージ中エフェクト
        private ParticleSystem _particleSystem;
        private Image _bulletGage;                            // エネルギーゲージUI

        // ======= 弾関係 =======
        [Header("=== 弾関連 ===")]
        [SerializeField] private GameObject bulletPrefab;     // 発射弾
        [SerializeField] private float bulletSpeed = 20f;     // 弾速

        [Header("=== ブロックPrefab ===")]
        [SerializeField] private GameObject fixedNBlockPrefab; // N極ブロック
        [SerializeField] private GameObject fixedSBlockPrefab; // S極ブロック

        private void Start()
        {
            // 必要コンポーネント取得
            _inputHandler = InputHandler.Instance;
            _magnet = GameObject.Find(GameConstants.MAGNET_MANAGER_OBJ)
                               .GetComponent<MagnetManager>();
            _playerState = GetComponent<PlayerStateController>();
            _uiManager = FindObjectOfType<MagnetUIManager>();
            _mainCamera = UnityEngine.Camera.main;

            _bulletGage = GameObject.Find("EnergyGage").GetComponent<Image>();
            _particleSystem = _powerEffectObj.GetComponent<ParticleSystem>();

            // 初期状態で非表示
            _chargeGageObj.SetActive(false);
            _powerEffectObj.SetActive(false);
        }

        private void Update()
        {
            // === チャージUIが常にカメラを向くようにする ===
            if (_chargeGageObj.activeSelf && _mainCamera != null)
            {
                _chargeGageObj.transform.LookAt(
                    _chargeGageObj.transform.position + _mainCamera.transform.rotation * Vector3.forward,
                    _mainCamera.transform.rotation * Vector3.up
                );
            }

            // === 磁力使用中は射撃無効 ===
            if (_magnet.IsMagnetBoot) return;

            // === 射撃処理 ===
            if (_canShooting)
            {
                //SE弾射撃はこの一行（必要時にコメントアウト(

                SEManager.instance.PlaySE(SEManager.Bullet.BULLET_SHOT);
                //ここまで


                ShootBullet();
                _playerState.ForceSetState(State.STILLNESS);
                return;
            }

            // === チャージ中処理 ===
            if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT))
            {
                if (!_isCharging && _bulletGage.fillAmount > 0f)
                {
                    // チャージ開始
                    _isCharging = true;
                    _currentPower = 0f;
                    _chargeGageObj.SetActive(true);
                    _powerEffectObj.SetActive(true);

                    //SE弾発射チャージ中はこの一行（必要時にコメントアウト（チャージのSEの音源長すぎるかも？弾射撃と合わせて要検証）

                    SEManager.instance.PlaySE(SEManager.Bullet.BULLET_CHARGE);
                    //ここまで

                    _playerState.AddState(State.SHOOT);
                }

                ChargeUpdate();
            }

            // === ボタンを離した瞬間に射撃予約 ===
            if (!_inputHandler.IsActionPressing(InputConstants.Action.SHOOT) && _isCharging)
            {
                _isCharging = false;
                _chargeGageObj.SetActive(false);
                _canShooting = true;
            }
        }

        /// <summary>
        /// 弾を生成・発射
        /// </summary>
        private void ShootBullet()
        {
            if (bulletPrefab == null) return;

            // 弾生成位置（プレイヤーの少し上）
            Vector3 pos = transform.position + Vector3.up;

            // 弾インスタンス生成
            GameObject gb = Instantiate(bulletPrefab, pos, Quaternion.identity);
            Rigidbody rb = gb.GetComponent<Rigidbody>();

            // 射撃方向（マウス位置 or 前方）
            Vector3 targetDirection = transform.forward;
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, 100f))
                targetDirection = (hitInfo.point - pos).normalized;

            if (rb != null)
                rb.velocity = targetDirection * bulletSpeed;

            // BulletControllerにチャージパワーを渡す
            var bulletController = gb.GetComponent<BulletController>();
            if (bulletController != null)
            {
                bulletController.SetChargePower(_currentPower);
            }

            // 射撃後処理
            _canShooting = false;
            _powerEffectObj.SetActive(false);
            _bulletGage.fillAmount -= 0.1f; // エネルギー消費
        }

        /// <summary>
        /// チャージ中のUI更新とエフェクト制御
        /// </summary>
        private void ChargeUpdate()
        {
            if (_currentPower < 100f)
                _currentPower += ADD_POWER;

            _chargeGage.fillAmount = _currentPower / 100f;

            // エフェクト色をチャージ量で変更
            var main = _particleSystem.main;
            if (_currentPower < 33f) main.startColor = Color.green;
            else if (_currentPower < 66f) main.startColor = Color.yellow;
            else main.startColor = Color.red;
        }
    }
}