using Game.GameSystem;
using Game.StageScene.Player;
using UnityEngine;
using UnityEngine.UI;
using static Game.StageScene.Player.PlayerControllerBase;

namespace Game.StageScene.Magnet
{
    /// <summary>
    /// プレイヤーの弾発射（射撃）を制御するクラス
    /// 
    /// 【このクラスの役割】
    /// ・右クリック押下中：発射方向ラインを表示
    /// ・右クリックリリース時：弾を発射
    /// ・マウス位置を基準にした発射方向の計算
    /// ・弾の生成と発射
    /// ・エネルギー不足時、磁力Boot中、ポーズ中、ゴール時の射撃の強制停止
    /// </summary>
    public class BulletShootController : MonoBehaviour
    {
        // ===== 射撃状態（PlayerStateControllerとの連携用） =====
        public bool IsCharging => _isHolding; // クリック中かどうか
        public bool IsShooting => false; // リリース瞬間の判定なので常にfalse
        public float CurrentChargePower => 0f; // チャージ機能は削除したため常に0

        private bool _isHolding; // 現在射撃ボタンが押されているか

        // ===== 他システム参照 =====
        private InputHandler _inputHandler;
        private MagnetManager _magnet;
        private PlayerStateController _playerState;
        private GlobalUIManager _uiManagerGlobal;
        private UnityEngine.Camera _mainCamera;

        // ===== UI =====
        [Header("UI")]
        private Image _bulletGage; // 弾エネルギーゲージ

        // ===== 弾 =====
        [Header("Bullet")]
        [SerializeField] private GameObject bulletPrefab; // 発射する弾
        [SerializeField] private float bulletSpeed = 20f; // 弾の初速
        [SerializeField] private float energyCost = 0.1f; // 1発あたりのエネルギー消費量

        // ===== 照準 =====
        [Header("Aim")]
        [SerializeField] private bool useMouseAim = true; // マウスで照準するか
        [SerializeField] private LayerMask aimLayerMask = -1; // レイが当たるレイヤー

        // ===== デバッグ用の発射方向表示 =====
        [Header("Debug Visualization")]
        [SerializeField] private bool showDebugLine = true;
        [SerializeField] private float debugLineLength = 10f;
        [SerializeField] private bool enableDebugLog = false;
        private LineRenderer _debugLineRenderer;

        // ===== 連射制御 =====
        [Header("Fire Rate")]
        [SerializeField] private float fireRate = 0.2f; // 連射間隔（秒）
        private float _lastFireTime = -1f; // 最後に発射した時刻

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Start()
        {
            _inputHandler = InputHandler.Instance;

            var magnetObj = GameObject.Find(GameConstants.MAGNET_MANAGER_OBJ);
            if (magnetObj != null)
                _magnet = magnetObj.GetComponent<MagnetManager>();

            _playerState = GetComponent<PlayerStateController>();
            _uiManagerGlobal = FindObjectOfType<GlobalUIManager>();
            _mainCamera = UnityEngine.Camera.main;

            var gageObj = GameObject.Find("EnergyGage");
            if (gageObj != null)
                _bulletGage = gageObj.GetComponent<Image>();

            SetupDebugLine();

            if (enableDebugLog)
            {
                Debug.Log("=== BulletShootController 初期化 ===");
                Debug.Log($"InputHandler: {(_inputHandler != null ? "OK" : "NULL")}");
                Debug.Log($"MagnetManager: {(_magnet != null ? "OK" : "NULL")}");
                Debug.Log($"BulletGage: {(_bulletGage != null ? "OK" : "NULL")}");
                if (_bulletGage != null)
                    Debug.Log($"初期エネルギー: {_bulletGage.fillAmount}");
                Debug.Log($"BulletPrefab: {(bulletPrefab != null ? "OK" : "NULL")}");
            }
        }

        /// <summary>
        /// 発射方向を可視化するデバッグ用のLineRendererを生成
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
        /// 毎フレーム実行される射撃制御のメイン処理
        /// </summary>
        private void Update()
        {
            // ===== UIや演出による入力ロック =====
            if (GameInputLock.IsLocked)
            {
                _isHolding = false;
                HideDebugLine();
                return;
            }

            // ===== ゴール状態の場合は射撃を完全に無効化 =====
            if (_playerState != null && PlayerControllerBase.currentState.HasFlag(State.GOAL))
            {
                _isHolding = false;
                HideDebugLine();
                if (enableDebugLog && Input.GetMouseButtonDown(1))
                    Debug.Log("[BulletShoot] ゴール状態：射撃不可");
                return;
            }

            // ===== エネルギー不足の場合は射撃を無効化 =====
            if (_bulletGage != null && _bulletGage.fillAmount < energyCost)
            {
                _isHolding = false;
                HideDebugLine();
                if (enableDebugLog && Input.GetMouseButtonDown(1))
                    Debug.Log("[BulletShoot] エネルギー不足：射撃不可");
                return;
            }

            // ===== ポーズ中は射撃を強制停止 =====
            if (_uiManagerGlobal != null && Time.timeScale == 0f)
            {
                _isHolding = false;
                HideDebugLine();
                return;
            }

            // ===== 磁力Boot中は射撃禁止 =====
            if (_magnet != null && _magnet.IsMagnetBoot)
            {
                _isHolding = false;
                HideDebugLine();
                if (enableDebugLog && Input.GetMouseButtonDown(1))
                    Debug.Log("[BulletShoot] 磁力Boot中：射撃不可");
                return;
            }

            // ===== 射撃ボタンの状態管理 =====
            bool isPressing = _inputHandler != null &&
                              _inputHandler.IsActionPressing(InputConstants.Action.SHOOT);

            // ★ クリック中：ラインを表示
            if (isPressing)
            {
                _isHolding = true;
                UpdateDebugLine();
            }
            // ★ クリックを離した瞬間：弾を発射
            else if (_isHolding)
            {
                _isHolding = false;
                HideDebugLine();

                // 連射制限チェック
                if (Time.time - _lastFireTime >= fireRate)
                {
                    ShootBullet();
                }
                else
                {
                    if (enableDebugLog)
                        Debug.Log($"[BulletShoot] 連射制限中（残り{fireRate - (Time.time - _lastFireTime):F2}秒）");
                }
            }
            else
            {
                // クリックしていない：ラインを非表示
                HideDebugLine();
            }
        }

        /// <summary>
        /// デバッグラインを非表示にする
        /// </summary>
        private void HideDebugLine()
        {
            if (_debugLineRenderer != null && _debugLineRenderer.enabled)
                _debugLineRenderer.enabled = false;
        }

        /// <summary>
        /// 発射方向をLineRendererで表示（クリック中のみ）
        /// </summary>
        private void UpdateDebugLine()
        {
            if (!showDebugLine || _debugLineRenderer == null || _mainCamera == null)
            {
                HideDebugLine();
                return;
            }

            Vector3 shootPosition = transform.position + Vector3.up;
            Vector3 direction = GetShootDirection(shootPosition);

            _debugLineRenderer.SetPosition(0, shootPosition);
            _debugLineRenderer.SetPosition(1, shootPosition + direction * debugLineLength);
            _debugLineRenderer.enabled = true;
        }

        /// <summary>
        /// 弾を生成して発射する
        /// </summary>
        private void ShootBullet()
        {
            if (bulletPrefab == null)
            {
                Debug.LogError("[BulletShoot] bulletPrefabが設定されていません！");
                return;
            }

            Vector3 shootPosition = transform.position + Vector3.up;
            Vector3 direction = GetShootDirection(shootPosition);

            // 弾を生成
            GameObject bulletObj = Instantiate(bulletPrefab, shootPosition, Quaternion.identity);
            Rigidbody rb = bulletObj.GetComponent<Rigidbody>();

            if (rb == null)
            {
                Debug.LogError("[BulletShoot] 弾にRigidbodyが存在しません");
                Destroy(bulletObj);
                return;
            }

            // 物理設定
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.velocity = direction * bulletSpeed;

            // 弾の初期化（チャージパワーは常に0）
            if (bulletObj.TryGetComponent(out BulletController bulletController))
                bulletController.SetChargePower(0f);

            // エネルギー消費
            if (_bulletGage != null)
                _bulletGage.fillAmount -= energyCost;

            // 最終発射時刻を記録
            _lastFireTime = Time.time;

            if (enableDebugLog)
                Debug.Log($"[BulletShoot] 弾発射成功！ - 残りエネルギー: {(_bulletGage != null ? _bulletGage.fillAmount.ToString("F2") : "N/A")}");
        }

        /// <summary>
        /// マウス位置を基準に発射方向を計算
        /// </summary>
        private Vector3 GetShootDirection(Vector3 shootPosition)
        {
            if (!useMouseAim || _mainCamera == null)
                return transform.forward;

            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

            // レイキャストでワールド座標を取得
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, aimLayerMask))
            {
                Vector3 dir = hit.point - shootPosition;
                return dir.sqrMagnitude < 0.01f ? transform.forward : dir.normalized;
            }

            // レイキャストが当たらない場合は地面との交点を計算
            Plane plane = new Plane(Vector3.up, shootPosition);
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 targetPoint = ray.GetPoint(enter);
                return (targetPoint - shootPosition).normalized;
            }

            return transform.forward;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Sceneビュー用の発射方向可視化
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || _mainCamera == null) return;

            // ゴール状態ではGizmosも表示しない
            if (_playerState != null && PlayerControllerBase.currentState.HasFlag(State.GOAL))
                return;

            // エネルギー不足の場合もGizmosを表示しない
            if (_bulletGage != null && _bulletGage.fillAmount < energyCost)
                return;

            // クリック中のみGizmosを表示
            if (!_isHolding)
                return;

            Vector3 shootPosition = transform.position + Vector3.up;
            Vector3 direction = GetShootDirection(shootPosition);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(shootPosition, direction * 10f);
        }
#endif
    }
}