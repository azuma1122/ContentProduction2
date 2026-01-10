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
        private const float ADD_POWER = 0.1f;

        private bool _isCharging;
        private bool _canShooting;
        private float _currentPower;

        public bool IsCharging => _isCharging;
        public bool IsShooting => _canShooting;
        public float CurrentChargePower => _currentPower;

        private InputHandler _inputHandler;
        private MagnetManager _magnet;
        private PlayerStateController _playerState;
        private MagnetUIManager _uiManager;
        private GlobalUIManager _uiManagerGlobal;
        private UnityEngine.Camera _mainCamera;

        [Header("UI")]
        [SerializeField] private GameObject _chargeGageObj;
        [SerializeField] private Image _chargeGage;
        [SerializeField] private GameObject _powerEffectObj;
        private ParticleSystem _particleSystem;
        private Image _bulletGage;

        [Header("Bullet")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private float bulletSpeed = 20f;

        [Header("Aim")]
        [SerializeField] private bool useMouseAim = true;
        [SerializeField] private LayerMask aimLayerMask = -1;

        [Header("Debug Visualization")]
        [SerializeField] private bool showDebugLine = true;
        [SerializeField] private float debugLineLength = 10f;
        private LineRenderer _debugLineRenderer;

        private void Start()
        {
            _inputHandler = InputHandler.Instance;
            _magnet = GameObject.Find(GameConstants.MAGNET_MANAGER_OBJ)
                                .GetComponent<MagnetManager>();
            _playerState = GetComponent<PlayerStateController>();
            _uiManager = FindObjectOfType<MagnetUIManager>();
            _uiManagerGlobal = FindObjectOfType<GlobalUIManager>();
            _mainCamera = UnityEngine.Camera.main;

            _bulletGage = GameObject.Find("EnergyGage").GetComponent<Image>();
            _particleSystem = _powerEffectObj.GetComponent<ParticleSystem>();

            _chargeGageObj.SetActive(false);
            _powerEffectObj.SetActive(false);

            SetupDebugLine();
            DisableLineRenderersInChildren();
        }

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

        private void Update()
        {
            if (_uiManagerGlobal != null && Time.timeScale == 0f)
            {
                if (_isCharging) ResetCharge();
                if (_debugLineRenderer != null) _debugLineRenderer.enabled = false;
                return;
            }

            // ===== ここが修正された磁力Boot判定 =====
            if (_magnet.IsMagnetBoot)
            {
                // チャージ中 or 発射待機中なら強制リセット（＝射撃アニメも止まる）
                if (_isCharging || _canShooting)
                {
                    ResetCharge();
                }

                _playerState.RemoveState(State.SHOOT);

                if (_debugLineRenderer != null)
                    _debugLineRenderer.enabled = false;

                return;
            }
            // =======================================

            if (_canShooting)
            {
                ShootBullet();
                _playerState.ForceSetState(State.STILLNESS);
                return;
            }

            if (_inputHandler.IsActionPressing(InputConstants.Action.SHOOT))
            {
                if (!_isCharging && _bulletGage.fillAmount > 0f)
                {
                    _isCharging = true;
                    _currentPower = 0f;
                    _chargeGageObj.SetActive(true);
                    _powerEffectObj.SetActive(true);
                    _playerState.AddState(State.SHOOT);
                }

                ChargeUpdate();
                UpdateDebugLine();
            }

            if (!_inputHandler.IsActionPressing(InputConstants.Action.SHOOT) && _isCharging)
            {
                _isCharging = false;
                _chargeGageObj.SetActive(false);
                _canShooting = true;

                if (_debugLineRenderer != null)
                    _debugLineRenderer.enabled = false;
            }
        }

        private void UpdateDebugLine()
        {
            if (!showDebugLine || _debugLineRenderer == null || _mainCamera == null) return;

            Vector3 shootPosition = transform.position + Vector3.up;
            Vector3 direction = GetShootDirection(shootPosition);

            _debugLineRenderer.SetPosition(0, shootPosition);
            _debugLineRenderer.SetPosition(1, shootPosition + direction * debugLineLength);
            _debugLineRenderer.enabled = true;
        }

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

            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.velocity = direction * bulletSpeed;

            if (bulletObj.TryGetComponent(out BulletController bulletController))
            {
                bulletController.SetChargePower(_currentPower);
            }

            _canShooting = false;
            _powerEffectObj.SetActive(false);
            _bulletGage.fillAmount -= 0.1f;

            if (_debugLineRenderer != null)
                _debugLineRenderer.enabled = false;
        }

        private Vector3 GetShootDirection(Vector3 shootPosition)
        {
            if (!useMouseAim || _mainCamera == null)
                return transform.forward;

            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, aimLayerMask))
            {
                Vector3 dir = hit.point - shootPosition;
                return dir.sqrMagnitude < 0.01f ? transform.forward : dir.normalized;
            }

            Plane plane = new Plane(Vector3.up, shootPosition);
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 targetPoint = ray.GetPoint(enter);
                return (targetPoint - shootPosition).normalized;
            }

            return transform.forward;
        }

        private void ChargeUpdate()
        {
            if (_currentPower < 100f)
                _currentPower += ADD_POWER;

            _chargeGage.fillAmount = _currentPower / 100f;

            var main = _particleSystem.main;
            if (_currentPower < 33f) main.startColor = Color.green;
            else if (_currentPower < 66f) main.startColor = Color.yellow;
            else main.startColor = Color.red;
        }

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
