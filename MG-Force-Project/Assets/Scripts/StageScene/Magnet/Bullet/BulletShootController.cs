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
    /// ・射撃ボタン長押しによるチャージ処理
    /// ・チャージ量に応じたUIとエフェクトの更新
    /// ・マウス位置を基準にした発射方向の計算
    /// ・弾の生成と発射
    /// ・エネルギー不足時、磁力Boot中、ポーズ中、ゴール時の射撃の強制停止
    /// ・デバッグ用の発射方向ライン表示
    /// </summary>
    public class BulletShootController : MonoBehaviour
    {
        // 1フレームごとに増えるチャージ量
        private const float ADD_POWER = 0.1f;

        // ===== 射撃の内部状態 =====
        private bool _isCharging;     // チャージ中かどうか
        private bool _canShooting;   // ボタンを離して発射待機状態か
        private float _currentPower; // 現在のチャージ量

        // 外部から参照する用
        public bool IsCharging => _isCharging;
        public bool IsShooting => _canShooting;
        public float CurrentChargePower => _currentPower;

        // ===== 他システム参照 =====
        private InputHandler _inputHandler;     // 入力管理
        private MagnetManager _magnet;          // 磁力システム
        private PlayerStateController _playerState; // プレイヤーの状態管理
        private MagnetUIManager _uiManager;     // 磁力UI
        private GlobalUIManager _uiManagerGlobal; // ポーズ等のUI
        private UnityEngine.Camera _mainCamera; // メインカメラ（マウス照準用）

        // ===== UI =====
        [Header("UI")]
        //[SerializeField] private GameObject _chargeGageObj; // チャージゲージの親オブジェクト
        //[SerializeField] private Image _chargeGage;        // チャージ量表示
        //[SerializeField] private GameObject _powerEffectObj; // チャージエフェクト
        private ParticleSystem _particleSystem;            // パワーエフェクト用
        private Image _bulletGage;                          // 弾エネルギーゲージ

        // ===== 弾 =====
        [Header("Bullet")]
        [SerializeField] private GameObject bulletPrefab; // 発射する弾
        [SerializeField] private float bulletSpeed = 20f; // 弾の初速

        // ===== 照準 =====
        [Header("Aim")]
        [SerializeField] private bool useMouseAim = true; // マウスで照準するか
        [SerializeField] private LayerMask aimLayerMask = -1; // レイが当たるレイヤー

        // ===== デバッグ用の発射方向表示 =====
        [Header("Debug Visualization")]
        [SerializeField] private bool showDebugLine = true;
        [SerializeField] private float debugLineLength = 10f;
        private LineRenderer _debugLineRenderer;

        /// <summary>
        /// 初期化処理
        /// ・各マネージャー取得
        /// ・UIとエフェクトの初期非表示
        /// ・デバッグラインの準備
        /// </summary>
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
            //_particleSystem = _powerEffectObj.GetComponent<ParticleSystem>();

            // 初期状態ではゲージとエフェクトは非表示
            //_chargeGageObj.SetActive(false);
            //_powerEffectObj.SetActive(false);

            SetupDebugLine();
            DisableLineRenderersInChildren();
        }

        /// <summary>
        /// UIの子オブジェクトに含まれるLineRendererを全て無効化する
        /// （誤ってデバッグ線が表示されるのを防ぐ）
        /// </summary>
        private void DisableLineRenderersInChildren()
        {
            //if (_chargeGageObj != null)
            //{
            //    foreach (var line in _chargeGageObj.GetComponentsInChildren<LineRenderer>(true))
            //        line.enabled = false;
            //}

            //if (_powerEffectObj != null)
            //{
            //    foreach (var line in _powerEffectObj.GetComponentsInChildren<LineRenderer>(true))
            //        line.enabled = false;
            //}
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
            // ===== デバッグラインの状態を常に監視 =====
            // 条件を満たさない場合は必ずラインを消す
            bool shouldShowLine = false;

            // ===== ゴール状態の場合は射撃を完全に無効化 =====
            if (_playerState != null && PlayerControllerBase.currentState.HasFlag(State.GOAL))
            {
                // チャージや発射待機を強制リセット
                if (_isCharging || _canShooting)
                {
                    ResetCharge();
                    Debug.Log("[BulletShoot] ゴール状態：射撃を完全停止");
                }

                // エフェクトとデバッグラインを強制非表示
                ForceHideShootingEffects();
                return; // 以降の処理をスキップ
            }

            // ===== エネルギー不足の場合は射撃を無効化 =====
            if (_bulletGage != null && _bulletGage.fillAmount <= 0f)
            {
                // チャージや発射待機を強制リセット
                if (_isCharging || _canShooting)
                {
                    ResetCharge();
                    Debug.Log("[BulletShoot] エネルギー不足：射撃を停止");
                }

                // エフェクトとデバッグラインを強制非表示
                ForceHideShootingEffects();
                return; // 以降の処理をスキップ
            }

            // ===== ポーズ中は射撃を強制停止 =====
            if (_uiManagerGlobal != null && Time.timeScale == 0f)
            {
                if (_isCharging) ResetCharge();
                ForceHideShootingEffects();
                return;
            }

            // ===== 磁力Boot中は射撃禁止 =====
            if (_magnet != null && _magnet.IsMagnetBoot)
            {
                // チャージや発射待機を強制リセット
                if (_isCharging || _canShooting)
                    ResetCharge();

                // 射撃状態を解除（アニメ用）
                if (_playerState != null)
                    _playerState.RemoveState(State.SHOOT);

                ForceHideShootingEffects();
                return;
            }

            // ===== 発射待機中なら弾を撃つ =====
            if (_canShooting)
            {
                ShootBullet();
                if (_playerState != null)
                    _playerState.ForceSetState(State.STILLNESS);
                return;
            }

            // ===== ボタン押下中：チャージ処理 =====
            if (_inputHandler != null && _inputHandler.IsActionPressing(InputConstants.Action.SHOOT))
            {
                // エネルギーが残っている場合のみチャージ開始
                if (!_isCharging && _bulletGage != null && _bulletGage.fillAmount > 0f)
                {
                    _isCharging = true;
                    _currentPower = 0f;
                    //_chargeGageObj.SetActive(true);
                    //_powerEffectObj.SetActive(true);
                    if (_playerState != null)
                        _playerState.AddState(State.SHOOT);
                }

                // チャージ中でもエネルギーをチェック
                if (_isCharging && _bulletGage != null && _bulletGage.fillAmount <= 0f)
                {
                    ResetCharge();
                    Debug.Log("[BulletShoot] チャージ中にエネルギー切れ");
                    return;
                }

                if (_isCharging)
                {
                    //ChargeUpdate();
                    UpdateDebugLine();
                    shouldShowLine = true; // チャージ中はラインを表示
                }
            }
            // ===== ボタンを離したら発射準備 =====
            else if (_isCharging)
            {
                _isCharging = false;
                //_chargeGageObj.SetActive(false);
                _canShooting = true;

                if (_debugLineRenderer != null)
                    _debugLineRenderer.enabled = false;
            }

            // ===== 最終チェック: ラインを表示すべきでない場合は強制的に消す =====
            if (!shouldShowLine && _debugLineRenderer != null && _debugLineRenderer.enabled)
            {
                _debugLineRenderer.enabled = false;
            }
        }

        /// <summary>
        /// 射撃関連のエフェクトを強制的に非表示にする
        /// </summary>
        private void ForceHideShootingEffects()
        {
            //if (_chargeGageObj != null && _chargeGageObj.activeSelf)
            //    _chargeGageObj.SetActive(false);

            //if (_powerEffectObj != null && _powerEffectObj.activeSelf)
            //    _powerEffectObj.SetActive(false);

            if (_debugLineRenderer != null && _debugLineRenderer.enabled)
                _debugLineRenderer.enabled = false;
        }

        /// <summary>
        /// 発射方向をLineRendererで表示
        /// </summary>
        private void UpdateDebugLine()
        {
            if (!showDebugLine || _debugLineRenderer == null || _mainCamera == null)
            {
                if (_debugLineRenderer != null)
                    _debugLineRenderer.enabled = false;
                return;
            }

            // チャージ中のみラインを表示
            if (!_isCharging)
            {
                _debugLineRenderer.enabled = false;
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
                bulletController.SetChargePower(_currentPower);

            _canShooting = false;
            //_powerEffectObj.SetActive(false);
            _bulletGage.fillAmount -= 0.1f;

            if (_debugLineRenderer != null)
                _debugLineRenderer.enabled = false;

            Debug.Log($"[BulletShoot] 弾発射 - 残りエネルギー: {_bulletGage.fillAmount:F2}");
        }

        /// <summary>
        /// マウス位置を基準に発射方向を計算
        /// </summary>
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

        /// <summary>
        /// チャージ量を増やし、UIとエフェクトを更新
        /// </summary>
        //private void ChargeUpdate()
        //{
        //    if (_currentPower < 100f)
        //        _currentPower += ADD_POWER;

        //    _chargeGage.fillAmount = _currentPower / 100f;

        //    var main = _particleSystem.main;
        //    if (_currentPower < 33f) main.startColor = Color.green;
        //    else if (_currentPower < 66f) main.startColor = Color.yellow;
        //    else main.startColor = Color.red;
        //}

        /// <summary>
        /// チャージ・発射状態を完全リセットする
        /// </summary>
        private void ResetCharge()
        {
            _isCharging = false;
            _canShooting = false;
            _currentPower = 0f;
           // _chargeGageObj.SetActive(false);
            //_powerEffectObj.SetActive(false);

            if (_playerState != null)
                _playerState.RemoveState(State.SHOOT);

            if (_debugLineRenderer != null)
                _debugLineRenderer.enabled = false;

            //Debug.Log("[BulletShoot] 射撃状態を完全リセット");
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
            if (_bulletGage != null && _bulletGage.fillAmount <= 0f)
                return;

            Vector3 shootPosition = transform.position + Vector3.up;
            Vector3 direction = GetShootDirection(shootPosition);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(shootPosition, direction * 10f);
        }
#endif
    }
}