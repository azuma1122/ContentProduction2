using Game.GameSystem;
using Game.StageScene.Player;
using UnityEngine;
using UnityEngine.UI;
using static Game.StageScene.Player.PlayerControllerBase;

namespace Game.StageScene.Magnet
{
    /// <summary>
    /// プレイヤーの弾発射制御クラス
    /// 
    /// 【主な役割】
    /// ・射撃ボタン長押しによるチャージ処理
    /// ・チャージ量に応じたUI・エフェクト更新
    /// ・マウス位置を基準にした発射方向計算
    /// ・弾の生成・発射処理
    /// ・磁力モード中やポーズ中の射撃制御
    /// ・デバッグ用の発射方向ライン表示
    /// </summary>
    public class BulletShootController : MonoBehaviour
    {
        /// <summary>
        /// 1フレームごとに加算されるチャージ量
        /// </summary>
        private const float ADD_POWER = 0.1f;

        // =========================
        // 状態管理用変数
        // =========================

        /// <summary>現在チャージ中かどうか</summary>
        private bool _isCharging;

        /// <summary>発射可能状態かどうか（ボタン離した直後）</summary>
        private bool _canShooting;

        /// <summary>現在のチャージ量（0～100）</summary>
        private float _currentPower;

        // 外部参照用プロパティ
        public bool IsCharging => _isCharging;
        public bool IsShooting => _canShooting;
        public float CurrentChargePower => _currentPower;

        // =========================
        // 外部コンポーネント参照
        // =========================

        private InputHandler _inputHandler;
        private MagnetManager _magnet;
        private PlayerStateController _playerState;
        private MagnetUIManager _uiManager;
        private GlobalUIManager _uiManagerGlobal;
        private UnityEngine.Camera _mainCamera;

        // =========================
        // UI関連
        // =========================

        [Header("UI")]
        [SerializeField] private GameObject _chargeGageObj;   // チャージゲージ全体
        [SerializeField] private Image _chargeGage;           // チャージゲージImage
        [SerializeField] private GameObject _powerEffectObj;  // チャージエフェクト
        private ParticleSystem _particleSystem;               // エフェクト制御用
        private Image _bulletGage;                             // 残弾ゲージ

        // =========================
        // 弾関連
        // =========================

        [Header("Bullet")]
        [SerializeField] private GameObject bulletPrefab;     // 弾Prefab
        [SerializeField] private float bulletSpeed = 20f;     // 発射速度

        // =========================
        // エイム設定
        // =========================

        [Header("Aim")]
        [SerializeField] private bool useMouseAim = true;     // マウスエイム使用可否
        [SerializeField] private LayerMask aimLayerMask = -1; // レイ判定対象レイヤー

        // =========================
        // デバッグ表示
        // =========================

        [Header("Debug Visualization")]
        [SerializeField] private bool showDebugLine = true;   // 発射方向ライン表示
        [SerializeField] private float debugLineLength = 10f; // ライン長
        private LineRenderer _debugLineRenderer;

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Start()
        {
            // 各種マネージャ・コンポーネント取得
            _inputHandler = InputHandler.Instance;
            _magnet = GameObject.Find(GameConstants.MAGNET_MANAGER_OBJ)
                                .GetComponent<MagnetManager>();
            _playerState = GetComponent<PlayerStateController>();
            _uiManager = FindObjectOfType<MagnetUIManager>();
            _uiManagerGlobal = FindObjectOfType<GlobalUIManager>();
            _mainCamera = UnityEngine.Camera.main;

            // UI関連取得
            _bulletGage = GameObject.Find("EnergyGage").GetComponent<Image>();
            _particleSystem = _powerEffectObj.GetComponent<ParticleSystem>();

            // 初期状態では非表示
            _chargeGageObj.SetActive(false);
            _powerEffectObj.SetActive(false);

            // デバッグ用発射方向ラインの初期化
            SetupDebugLine();

            // UI内に含まれるLineRendererを無効化（意図しない表示防止）
            DisableLineRenderersInChildren();
        }

        /// <summary>
        /// ChargeGage / PowerEffect 内に含まれる LineRenderer を無効化
        /// </summary>
        private void DisableLineRenderersInChildren()
        {
            if (_chargeGageObj != null)
            {
                foreach (var line in _chargeGageObj.GetComponentsInChildren<LineRenderer>(true))
                {
                    line.enabled = false;
                }
            }

            if (_powerEffectObj != null)
            {
                foreach (var line in _powerEffectObj.GetComponentsInChildren<LineRenderer>(true))
                {
                    line.enabled = false;
                }
            }
        }

        /// <summary>
        /// デバッグ用の発射方向LineRendererを生成・設定
        /// </summary>
        private void SetupDebugLine()
        {
            if (!showDebugLine) return;

            GameObject lineObj = new GameObject("DebugShootLine");
            lineObj.transform.SetParent(transform);
            lineObj.transform.localPosition = Vector3.zero;

            _debugLineRenderer = lineObj.AddComponent<LineRenderer>();
            _debugLineRenderer.positionCount = 2;
            _debugLineRenderer.startWidth = 0.05f;
            _debugLineRenderer.endWidth = 0.05f;
            _debugLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _debugLineRenderer.startColor = Color.red;
            _debugLineRenderer.endColor = Color.red;
            _debugLineRenderer.enabled = false;
        }

        /// <summary>
        /// 毎フレームの入力・射撃処理
        /// </summary>
        private void Update()
        {
            // ポーズ中は射撃処理を行わない
            if (_uiManagerGlobal != null && Time.timeScale == 0f)
            {
                if (_isCharging) ResetCharge();
                if (_debugLineRenderer != null) _debugLineRenderer.enabled = false;
                return;
            }

            // 磁力モード中は射撃不可
            if (_magnet.IsMagnetBoot)
            {
                if (_debugLineRenderer != null) _debugLineRenderer.enabled = false;
                return;
            }

            // 発射フラグが立っている場合は弾を撃つ
            if (_canShooting)
            {
                ShootBullet();
                _playerState.ForceSetState(State.STILLNESS);
                return;
            }

            // 射撃ボタン押下中
            if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT))
            {
                // チャージ開始
                if (!_isCharging && _bulletGage.fillAmount > 0f)
                {
                    _isCharging = true;
                    _currentPower = 0f;
                    _chargeGageObj.SetActive(true);
                    _powerEffectObj.SetActive(true);
                    _playerState.AddState(State.SHOOT);
                }

                // チャージ更新
                ChargeUpdate();
                UpdateDebugLine();
            }

            // ボタンを離した瞬間に発射準備へ
            if (!_inputHandler.IsActionPressing(InputConstants.Action.SHOOT) && _isCharging)
            {
                _isCharging = false;
                _chargeGageObj.SetActive(false);
                _canShooting = true;

                if (_debugLineRenderer != null)
                    _debugLineRenderer.enabled = false;
            }
        }

        /// <summary>
        /// デバッグ用の発射方向ラインを更新
        /// </summary>
        private void UpdateDebugLine()
        {
            if (!showDebugLine || _debugLineRenderer == null || _mainCamera == null) return;

            Vector3 shootPosition = transform.position + Vector3.up;
            Vector3 direction = GetShootDirection(shootPosition);

            _debugLineRenderer.SetPosition(0, shootPosition);
            _debugLineRenderer.SetPosition(1, shootPosition + direction * debugLineLength);
            _debugLineRenderer.enabled = true;
        }

        /// <summary>
        /// 弾を生成して発射する処理
        /// </summary>
        private void ShootBullet()
        {
            Vector3 shootPosition = transform.position + Vector3.up;

            GameObject bulletObj = Instantiate(bulletPrefab, shootPosition, Quaternion.identity);
            Rigidbody rb = bulletObj.GetComponent<Rigidbody>();

            if (rb == null)
            {
                Debug.LogError("弾にRigidbodyが存在しません");
                Destroy(bulletObj);
                return;
            }

            Vector3 direction = GetShootDirection(shootPosition);

            // 物理挙動設定
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.velocity = direction * bulletSpeed;

            // チャージ量を弾へ渡す
            if (bulletObj.TryGetComponent(out BulletController bulletController))
            {
                bulletController.SetChargePower(_currentPower);
            }

            // 状態リセット
            _canShooting = false;
            _powerEffectObj.SetActive(false);
            _bulletGage.fillAmount -= 0.1f;

            if (_debugLineRenderer != null)
                _debugLineRenderer.enabled = false;
        }

        /// <summary>
        /// マウス位置から弾の発射方向を計算
        /// </summary>
        private Vector3 GetShootDirection(Vector3 shootPosition)
        {
            if (!useMouseAim || _mainCamera == null)
                return transform.forward;

           Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

            // レイがヒットした場合はその地点を狙う
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, aimLayerMask))
            {
                Vector3 dir = hit.point - shootPosition;
                return dir.sqrMagnitude < 0.01f ? transform.forward : dir.normalized;
            }

            // 地面平面との交点を利用
            Plane plane = new Plane(Vector3.up, shootPosition);
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 targetPoint = ray.GetPoint(enter);
                return (targetPoint - shootPosition).normalized;
            }

            return transform.forward;
        }

        /// <summary>
        /// チャージ中の処理更新
        /// </summary>
        private void ChargeUpdate()
        {
            if (_currentPower < 100f)
                _currentPower += ADD_POWER;

            _chargeGage.fillAmount = _currentPower / 100f;

            // チャージ量に応じてエフェクト色変更
            var main = _particleSystem.main;
            if (_currentPower < 33f) main.startColor = Color.green;
            else if (_currentPower < 66f) main.startColor = Color.yellow;
            else main.startColor = Color.red;
        }

        /// <summary>
        /// チャージ状態を強制リセット
        /// </summary>
        private void ResetCharge()
        {
            _isCharging = false;
            _canShooting = false;
            _currentPower = 0f;
            _chargeGageObj.SetActive(false);
            _powerEffectObj.SetActive(false);
            _playerState.RemoveState(State.SHOOT);

            if (_debugLineRenderer != null)
                _debugLineRenderer.enabled = false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Sceneビュー上に発射方向を可視化
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || _mainCamera == null) return;

            Vector3 shootPosition = transform.position + Vector3.up;
            Vector3 direction = GetShootDirection(shootPosition);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(shootPosition, direction * 10f);
        }
#endif
    }
}
