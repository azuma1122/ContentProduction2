using Game.GameSystem;
using Game.StageScene.Player;
using UnityEngine;
using UnityEngine.UI;
using static Game.StageScene.Player.PlayerControllerBase;

namespace Game.StageScene.Magnet
{
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

        [Header("=== UI & エフェクト ===")]
        [SerializeField] private GameObject _chargeGageObj;
        [SerializeField] private Image _chargeGage;
        [SerializeField] private GameObject _powerEffectObj;
        private ParticleSystem _particleSystem;
        private Image _bulletGage;

        [Header("=== 弾関連 ===")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private float bulletSpeed = 20f;

        [Header("=== ブロックPrefab ===")]
        [SerializeField] private GameObject fixedNBlockPrefab;
        [SerializeField] private GameObject fixedSBlockPrefab;

        [Header("=== デバッグ設定 ===")]
        [SerializeField] private bool useMouseAim = true;  // マウスエイム使用フラグ
        [SerializeField] private LayerMask aimLayerMask = -1;  // レイキャスト対象レイヤー

        [Header("=== 発射方向可視化（Game View） ===")]
        [SerializeField] private bool showDebugLine = true;  // デバッグライン表示フラグ
        [SerializeField] private float debugLineLength = 10f;  // デバッグラインの長さ
        private LineRenderer _debugLineRenderer;  // 発射方向用LineRenderer
        private LineRenderer _mouseRayLineRenderer;  // マウスレイ用LineRenderer

        private void Start()
        {
            _inputHandler = InputHandler.Instance;
            _magnet = GameObject.Find(GameConstants.MAGNET_MANAGER_OBJ)
                               .GetComponent<MagnetManager>();
            _playerState = GetComponent<PlayerStateController>();
            _uiManager = FindObjectOfType<MagnetUIManager>();
            _uiManagerGlobal = FindObjectOfType<GlobalUIManager>();

            _mainCamera = UnityEngine.Camera.main;

            // カメラが見つからない場合の警告
            if (_mainCamera == null)
            {
                Debug.LogError("MainCameraが見つかりません!Cameraに'MainCamera'タグが設定されているか確認してください。");
            }

            _bulletGage = GameObject.Find("EnergyGage").GetComponent<Image>();
            _particleSystem = _powerEffectObj.GetComponent<ParticleSystem>();

            _chargeGageObj.SetActive(false);
            _powerEffectObj.SetActive(false);

            // デバッグライン用のLineRendererを作成
            SetupDebugLines();
        }

        /// <summary>
        /// デバッグライン用のLineRendererをセットアップ
        /// </summary>
        private void SetupDebugLines()
        {
            if (!showDebugLine) return;

            // 発射方向ライン（赤）
            GameObject shootLineObj = new GameObject("DebugShootLine");
            shootLineObj.transform.SetParent(transform);
            shootLineObj.transform.localPosition = Vector3.zero;

            _debugLineRenderer = shootLineObj.AddComponent<LineRenderer>();
            _debugLineRenderer.positionCount = 2;
            _debugLineRenderer.startWidth = 0.05f;
            _debugLineRenderer.endWidth = 0.05f;
            _debugLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _debugLineRenderer.startColor = Color.red;
            _debugLineRenderer.endColor = Color.red;

            // マウスレイライン（黄）
            GameObject mouseRayLineObj = new GameObject("DebugMouseRayLine");
            mouseRayLineObj.transform.SetParent(transform);
            mouseRayLineObj.transform.localPosition = Vector3.zero;

            _mouseRayLineRenderer = mouseRayLineObj.AddComponent<LineRenderer>();
            _mouseRayLineRenderer.positionCount = 2;
            _mouseRayLineRenderer.startWidth = 0.03f;
            _mouseRayLineRenderer.endWidth = 0.03f;
            _mouseRayLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _mouseRayLineRenderer.startColor = Color.yellow;
            _mouseRayLineRenderer.endColor = Color.yellow;

            Debug.Log("[BulletShootController] デバッグラインを作成しました");
        }

        private void Update()
        {
            if (_uiManagerGlobal != null && Time.timeScale == 0f)
            {
                if (_isCharging)
                    ResetCharge();
                return;
            }

            if (_chargeGageObj.activeSelf && _mainCamera != null)
            {
                _chargeGageObj.transform.LookAt(
                    _chargeGageObj.transform.position + _mainCamera.transform.rotation * Vector3.forward,
                    _mainCamera.transform.rotation * Vector3.up
                );
            }

            if (_magnet.IsMagnetBoot) return;

            // デバッグラインの更新（常時表示）
            UpdateDebugLines();

            if (_canShooting)
            {
                SafePlaySE(SEManager.Bullet.BULLET_SHOT);
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

                    SafePlaySE(SEManager.Bullet.BULLET_CHARGE);
                    _playerState.AddState(State.SHOOT);
                }

                ChargeUpdate();
            }

            if (!_inputHandler.IsActionPressing(InputConstants.Action.SHOOT) && _isCharging)
            {
                _isCharging = false;
                _chargeGageObj.SetActive(false);
                _canShooting = true;
            }
        }

        /// <summary>
        /// デバッグラインを更新（Gizmoと同じ内容）
        /// </summary>
        private void UpdateDebugLines()
        {
            if (!showDebugLine || _mainCamera == null) return;
            if (_debugLineRenderer == null || _mouseRayLineRenderer == null) return;

            Vector3 shootPosition = transform.position + Vector3.up;
            Vector3 direction = GetShootDirection(shootPosition);

            // 発射方向ライン（赤）
            _debugLineRenderer.SetPosition(0, shootPosition);
            _debugLineRenderer.SetPosition(1, shootPosition + direction * debugLineLength);

            // マウスからのレイライン（黄）
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            _mouseRayLineRenderer.SetPosition(0, ray.origin);
            _mouseRayLineRenderer.SetPosition(1, ray.origin + ray.direction * 100f);
        }

        /// <summary>
        /// 安全にSEを再生(FMODエラーを無視)
        /// </summary>
        private void SafePlaySE(SEManager.Bullet seName)
        {
            try
            {
                if (SEManager.instance != null)
                {
                    SEManager.instance.PlaySE(seName);
                }
            }
            catch (System.Exception e)
            {
                // FMODエラーが発生してもゲームを続行
                Debug.LogWarning($"SE再生エラー: {seName} - {e.Message}");
            }
        }

        private void ResetCharge()
        {
            _isCharging = false;
            _canShooting = false;
            _currentPower = 0f;
            _chargeGageObj.SetActive(false);
            _powerEffectObj.SetActive(false);

            if (_playerState != null)
            {
                _playerState.RemoveState(State.SHOOT);
            }
        }

        private void ShootBullet()
        {
            if (bulletPrefab == null || _mainCamera == null) return;

            Vector3 shootPosition = transform.position + Vector3.up;
            GameObject bulletObj = Instantiate(bulletPrefab, shootPosition, Quaternion.identity);
            Rigidbody rb = bulletObj.GetComponent<Rigidbody>();

            Vector3 targetDirection = GetShootDirection(shootPosition);

            if (rb != null)
            {
                rb.velocity = targetDirection * bulletSpeed;
            }

            var bulletController = bulletObj.GetComponent<BulletController>();
            if (bulletController != null)
            {
                bulletController.SetChargePower(_currentPower);
            }

            _canShooting = false;
            _powerEffectObj.SetActive(false);
            _bulletGage.fillAmount -= 0.1f;
        }

        /// <summary>
        /// 発射方向を計算(Build環境でも正確に動作)
        /// </summary>
        private Vector3 GetShootDirection(Vector3 shootPosition)
        {
            if (!useMouseAim)
            {
                // マウスエイムを使わない場合はキャラクターの正面方向
                return transform.forward;
            }

            // マウス位置からレイを飛ばす
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

            // レイキャストでヒットした場合
            if (Physics.Raycast(ray, out RaycastHit hitInfo, 1000f, aimLayerMask))
            {
                Vector3 direction = (hitInfo.point - shootPosition).normalized;

                // Y軸の補正(極端な上下角度を制限する場合)
                // direction.y = Mathf.Clamp(direction.y, -0.9f, 0.9f);
                // direction = direction.normalized;

                return direction;
            }
            else
            {
                // ヒットしなかった場合はレイの方向を使用
                // 発射位置からの方向に変換
                Vector3 targetPoint = ray.origin + ray.direction * 100f;
                return (targetPoint - shootPosition).normalized;
            }
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

#if UNITY_EDITOR
        // エディタでのデバッグ表示
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || _mainCamera == null) return;

            Vector3 shootPosition = transform.position + Vector3.up;
            Vector3 direction = GetShootDirection(shootPosition);

            // 発射方向を可視化
            Gizmos.color = Color.red;
            Gizmos.DrawRay(shootPosition, direction * 10f);

            // マウスからのレイを可視化
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(ray.origin, ray.direction * 100f);
        }
#endif
    }
}