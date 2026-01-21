using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.StageScene.Magnet
{
    /// <summary>
    /// 移動可能な磁石ブロック
    /// 
    /// ・Boot（磁力モード）ON の間だけ磁力を発生させる
    /// ・他の MovingMagnetBlock に対して引力／斥力を与える
    /// ・Boot 切替時の Rigidbody 状態を安定させる
    /// ・磁力強度を UI（Image.fillAmount）で表示
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class MovingMagnetBlock : MonoBehaviour
    {
        // ===== インスペクタ設定 =====

        [Header("磁石の極タイプ"), Tooltip("N極(赤)か S極(青)を設定する")]
        [SerializeField] public MagnetPoleType poleType = MagnetPoleType.North;

        [Header("磁力の設定値"), Range(0f, 100f), Tooltip("磁力の強さ。数値が大きいほど引き寄せ／反発が強くなる")]
        [SerializeField]
        private float magneticForce = 10f;

        [Tooltip("磁力の影響範囲。この距離内にある磁石と干渉する")]
        [SerializeField]
        private float magneticRange = 5f;

        [Header("磁力表示用UI（Image）"), Tooltip("磁力強度を表示するUI Image（FillAmountを使用）")]
        [SerializeField]
        private Image magneticForceImage;

        [Header("デバッグ設定")]
        [SerializeField] private bool enableDebugLog = false;

        // ===== 内部参照 =====

        // 自分の Rigidbody
        private Rigidbody _rigidbody;

        // 磁力モード（Boot）状態を管理するマネージャ
        private MagnetManager magnetManager;

        // UI 正規化用の最大磁力値
        private const float MAX_FORCE = 100f;

        // ===== Boot 状態管理用 =====

        // 前フレームの Boot 状態
        private bool _wasBoot = false;

        // Boot ON 時の初期化が完了しているか
        private bool _isBootInitialized = false;

        // 最後に Boot が OFF になった時刻
        private float _lastBootOffTime = -1f;

        // Boot OFF 直後に再 ON された場合のクールダウン時間
        private const float BOOT_OFF_COOLDOWN = 0.1f;

        // =====================================================================

        private void Start()
        {
            // Rigidbody を取得してキャッシュ
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody == null)
            {
                Debug.LogError($"[MovingMagnetBlock] {gameObject.name} に Rigidbody が見つかりません！");
                return;
            }

            // Rigidbody の初期物理設定
            _rigidbody.isKinematic = false; // 物理演算を有効
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate; // 見た目のカクつき防止
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ; // 回転 & Z移動を固定
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous; // 高速移動時のすり抜け防止

            if (enableDebugLog)
            {
                Debug.Log($"[MovingMagnetBlock] {gameObject.name} 初期化完了");
                Debug.Log($"  - isKinematic: {_rigidbody.isKinematic}");
                Debug.Log($"  - collisionDetectionMode: {_rigidbody.collisionDetectionMode}");
            }

            // MagnetManager をシーンから取得
            magnetManager = FindObjectOfType<MagnetManager>();
            if (magnetManager == null)
            {
                Debug.LogError($"[MovingMagnetBlock] {gameObject.name} で MagnetManager が見つかりません！");
            }

            // 磁力 UI を初期更新
            UpdateMagneticForceUI();

            // 次フレームで確実に Rigidbody を起こす（Sleep対策）
            StartCoroutine(ForceWakeUpNextFrame());
        }

        /// <summary>
        /// 次フレームで Rigidbody を強制的に起こす
        /// （生成直後に Sleep してしまう問題の対策）
        /// </summary>
        private IEnumerator ForceWakeUpNextFrame()
        {
            yield return null;

            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = false;
                _rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
                _rigidbody.WakeUp();

                if (enableDebugLog)
                {
                    Debug.Log($"[MovingMagnetBlock] {gameObject.name} - ForceWakeUpNextFrame 実行");
                }
            }
        }

        private void Update()
        {
            // デバッグ用：Bキーで現在状態をログ出力
            if (Input.GetKeyDown(KeyCode.B) && enableDebugLog)
            {
                Debug.Log($"========== Bキー押下 [{gameObject.name}] ==========");
                Debug.Log($"  magnetManager: {(magnetManager != null ? "存在" : "null")}");
                if (magnetManager != null)
                {
                    Debug.Log($"  IsMagnetBoot: {magnetManager.IsMagnetBoot}");
                }
                if (_rigidbody != null)
                {
                    Debug.Log($"  velocity: {_rigidbody.velocity}");
                    Debug.Log($"  isKinematic: {_rigidbody.isKinematic}");
                    Debug.Log($"  IsSleeping: {_rigidbody.IsSleeping()}");
                }
                Debug.Log($"=======================================");
            }
        }

        private void FixedUpdate()
        {
            if (_rigidbody == null) return;

            // ★ Rigidbody が Sleep していたら毎フレーム起こす
            if (_rigidbody.IsSleeping())
            {
                _rigidbody.WakeUp();
            }

            // ★ isKinematic / constraints の保険（他スクリプトの影響対策）
            if (_rigidbody.isKinematic)
            {
                _rigidbody.isKinematic = false;
            }
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;

            // MagnetManager を再取得（生成順のズレ対策）
            if (magnetManager == null)
            {
                magnetManager = FindObjectOfType<MagnetManager>();
                if (magnetManager == null)
                {
                    return;
                }
            }

            bool isBootActive = magnetManager.IsMagnetBoot;

            // ===== Boot 状態が切り替わった瞬間の処理 =====
            if (isBootActive != _wasBoot)
            {
                if (isBootActive)
                {
                    // Boot ON 直後がクールダウン中なら無視
                    if (Time.time - _lastBootOffTime < BOOT_OFF_COOLDOWN)
                    {
                        if (enableDebugLog)
                        {
                            Debug.LogWarning($"[MovingMagnetBlock] {gameObject.name} - Boot ON (クールダウン中)");
                        }
                        _wasBoot = isBootActive;
                        return;
                    }

                    if (enableDebugLog)
                    {
                        Debug.Log($"[MovingMagnetBlock] {gameObject.name} - Boot ON 初期化開始");
                    }

                    // 速度をリセットして安定化
                    _rigidbody.velocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;

                    if (_rigidbody.IsSleeping())
                    {
                        _rigidbody.WakeUp();
                    }

                    _rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;

                    if (_rigidbody.isKinematic)
                    {
                        _rigidbody.isKinematic = false;
                    }

                    _isBootInitialized = true;
                }
                else
                {
                    // Boot OFF 時の後処理
                    if (enableDebugLog)
                    {
                        Debug.Log($"[MovingMagnetBlock] {gameObject.name} - Boot OFF");
                    }

                    _rigidbody.velocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;

                    _isBootInitialized = false;
                    _lastBootOffTime = Time.time;
                }

                _wasBoot = isBootActive;
            }

            // ===== Boot OFF 中は物理的に完全停止 =====
            if (!isBootActive)
            {
                if (_rigidbody.velocity != Vector3.zero || _rigidbody.angularVelocity != Vector3.zero)
                {
                    _rigidbody.velocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;
                }
                return;
            }

            // Boot ON でも初期化が終わるまで何もしない
            if (!_isBootInitialized)
            {
                return;
            }

            // ===== 磁力処理 =====

            // 磁力範囲内の Collider を取得
            Collider[] colliders = Physics.OverlapSphere(transform.position, magneticRange);

            foreach (Collider collider in colliders)
            {
                MovingMagnetBlock other = collider.GetComponent<MovingMagnetBlock>();

                // 自分以外の MovingMagnetBlock にだけ作用させる
                if (other != null && other != this)
                {
                    ApplyMagneticForce(other);
                }
            }
        }

        /// <summary>
        /// 他の磁石ブロックに磁力（引力 or 斥力）を与える
        /// </summary>
        private void ApplyMagneticForce(MovingMagnetBlock other)
        {
            if (other == null) return;

            Rigidbody otherRb = other.GetComponent<Rigidbody>();
            if (otherRb == null) return;

            // 相手側の Boot 初期化が終わっていない場合は無視
            if (!other._isBootInitialized)
            {
                return;
            }

            // 距離と方向を計算
            Vector3 direction = transform.position - other.transform.position;
            float distance = direction.magnitude;

            // 範囲外 or 近すぎる場合は無視
            if (distance > magneticRange || distance <= 0.01f) return;

            // 同極：反発（-1） / 異極：引力（+1）
            float forceMultiplier = (poleType == other.poleType) ? -1f : 1f;

            // 距離に反比例した磁力
            float force = (magneticForce * 10f / distance) * forceMultiplier;

            // 力を加える
            otherRb.AddForce(direction.normalized * force, ForceMode.Force);
        }

        /// <summary>
        /// 磁力強度を外部から変更する
        /// </summary>
        public void SetMagneticForce(float newForce)
        {
            magneticForce = Mathf.Clamp(newForce, 0f, MAX_FORCE);
            UpdateMagneticForceUI();
        }

        /// <summary>
        /// 磁力 UI（Image.fillAmount）を更新
        /// </summary>
        private void UpdateMagneticForceUI()
        {
            if (magneticForceImage != null)
            {
                magneticForceImage.fillAmount = Mathf.Clamp01(magneticForce / MAX_FORCE);
            }
        }

        /// <summary>
        /// 極性（N / S）を切り替える
        /// </summary>
        public void TogglePolarity()
        {
            poleType = (poleType == MagnetPoleType.North)
                ? MagnetPoleType.South
                : MagnetPoleType.North;

            // 見た目の色も変更
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = (poleType == MagnetPoleType.North) ? Color.red : Color.blue;
            }

            Debug.Log($"{gameObject.name} の極性を {poleType} に切り替えました。");
        }

        /// <summary>
        /// シーンビューに磁力範囲を表示
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = (poleType == MagnetPoleType.North) ? Color.red : Color.blue;
            Gizmos.DrawWireSphere(transform.position, magneticRange);
        }
    }
}
