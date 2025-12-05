using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.StageScene.Magnet
{
    /// <summary>
    /// 移動可能な磁石ブロック
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class MovingMagnetBlock : MonoBehaviour
    {
        [Header("磁石の極タイプ"), Tooltip("N極(赤)か S極(青)を設定する")]
        [SerializeField] public MagnetPoleType poleType = MagnetPoleType.North;

        [Header("磁力の設定値"), Range(0f, 100f), Tooltip("磁力の強さ。数値が大きいほど引き寄せ／反発が強くなる")]
        [SerializeField]
        private float magneticForce = 10f;

        [Tooltip("磁力の影響範囲。この距離内にある磁石と干渉する"), SerializeField]
        private float magneticRange = 5f;

        [Header("磁力表示用UI（Image）"), Tooltip("磁力強度を表示するUI Image（FillAmountを使用）")]
        [SerializeField]
        private Image magneticForceImage;

        [Header("デバッグ設定")]
        [SerializeField] private bool enableDebugLog = false; // デバッグログの有効/無効

        // Rigidbodyキャッシュ
        private Rigidbody _rigidbody;

        // MagnetManagerへの参照
        private MagnetManager magnetManager;

        // 最大磁力値（UI正規化用）
        private const float MAX_FORCE = 100f;

        // 前フレームのBoot状態を記録
        private bool _wasBoot = false;

        // Boot ON時の初期化が完了したか
        private bool _isBootInitialized = false;

        // 最後にBootがOFFになった時刻
        private float _lastBootOffTime = -1f;

        // Boot OFF後の待機時間（秒）
        private const float BOOT_OFF_COOLDOWN = 0.1f;

        private void Start()
        {
            // Rigidbodyを取得してキャッシュ化
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody == null)
            {
                Debug.LogError($"[MovingMagnetBlock] {gameObject.name} に Rigidbody が見つかりません！");
                return;
            }

            // 重要：物理演算の設定を最適化
            _rigidbody.isKinematic = false;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            // Constraintsを設定（Z軸と回転を固定）
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;

            // Collision Detection を Continuous に設定（連続衝突検出で安定性向上）
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

            if (enableDebugLog)
            {
                Debug.Log($"[MovingMagnetBlock] {gameObject.name} 初期化完了");
                Debug.Log($"  - isKinematic: {_rigidbody.isKinematic}");
                Debug.Log($"  - collisionDetectionMode: {_rigidbody.collisionDetectionMode}");
            }

            // MagnetManagerを取得
            magnetManager = FindObjectOfType<MagnetManager>();
            if (magnetManager == null)
            {
                Debug.LogError($"[MovingMagnetBlock] {gameObject.name} で MagnetManager が見つかりません！");
            }

            // 開始時にUI更新
            UpdateMagneticForceUI();
        }

        private void Update()
        {
            // デバッグ用：Bキー押下時の状態確認
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
                }
                Debug.Log($"=======================================");
            }
        }

        private void FixedUpdate()
        {
            if (_rigidbody == null) return;

            // MagnetManagerが取得できていない場合は再取得を試みる
            if (magnetManager == null)
            {
                magnetManager = FindObjectOfType<MagnetManager>();
                if (magnetManager == null)
                {
                    return;
                }
            }

            // Boot状態を取得
            bool isBootActive = magnetManager.IsMagnetBoot;

            // Boot状態が変化した時の処理
            if (isBootActive != _wasBoot)
            {
                if (isBootActive)
                {
                    // --- Boot ON になった ---

                    // クールダウン期間中なら初期化をスキップ
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

                    // 確実に物理状態をリセット
                    _rigidbody.velocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;

                    // スリープ状態から確実に復帰
                    if (_rigidbody.IsSleeping())
                    {
                        _rigidbody.WakeUp();
                    }

                    // Constraintsを再設定
                    _rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;

                    // isKinematic が true になっていないか確認
                    if (_rigidbody.isKinematic)
                    {
                        Debug.LogWarning($"[MovingMagnetBlock] {gameObject.name} の isKinematic が true になっていたため false に修正");
                        _rigidbody.isKinematic = false;
                    }

                    _isBootInitialized = true;
                }
                else
                {
                    // --- Boot OFF になった ---

                    if (enableDebugLog)
                    {
                        Debug.Log($"[MovingMagnetBlock] {gameObject.name} - Boot OFF");
                    }

                    // 完全停止
                    _rigidbody.velocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;

                    _isBootInitialized = false;
                    _lastBootOffTime = Time.time;
                }

                _wasBoot = isBootActive;
            }

            // Bootがオフの場合は停止状態を維持
            if (!isBootActive)
            {
                // 毎フレーム確実に停止
                if (_rigidbody.velocity != Vector3.zero || _rigidbody.angularVelocity != Vector3.zero)
                {
                    _rigidbody.velocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;
                }
                return;
            }

            // Boot初期化が完了していない場合はスキップ
            if (!_isBootInitialized)
            {
                return;
            }

            // --- ここから磁力処理（Bootがオンの時のみ実行） ---

            // 指定範囲内にある全てのColliderを取得
            Collider[] colliders = Physics.OverlapSphere(transform.position, magneticRange);

            foreach (Collider collider in colliders)
            {
                // 自分以外の MovingMagnetBlock を探す
                MovingMagnetBlock other = collider.GetComponent<MovingMagnetBlock>();

                if (other != null && other != this)
                {
                    ApplyMagneticForce(other);
                }
            }
        }

        /// <summary>
        /// 他の磁石ブロックに対して磁力（引力または反発力）を加える
        /// </summary>
        private void ApplyMagneticForce(MovingMagnetBlock other)
        {
            if (other == null) return;

            Rigidbody otherRb = other.GetComponent<Rigidbody>();
            if (otherRb == null) return;

            // 相手もBoot初期化が完了していない場合はスキップ
            if (!other._isBootInitialized)
            {
                return;
            }

            // 方向と距離を計算
            Vector3 direction = transform.position - other.transform.position;
            float distance = direction.magnitude;

            // 範囲チェック
            if (distance > magneticRange || distance <= 0.01f) return;

            // 極性判定
            float forceMultiplier = (poleType == other.poleType) ? -1f : 1f;

            // 力を計算
            float force = (magneticForce * 10f / distance) * forceMultiplier;

            // 力を加える
            otherRb.AddForce(direction.normalized * force, ForceMode.Force);
        }

        public void SetMagneticForce(float newForce)
        {
            magneticForce = Mathf.Clamp(newForce, 0f, MAX_FORCE);
            UpdateMagneticForceUI();
        }

        private void UpdateMagneticForceUI()
        {
            if (magneticForceImage != null)
            {
                magneticForceImage.fillAmount = Mathf.Clamp01(magneticForce / MAX_FORCE);
            }
        }

        public void TogglePolarity()
        {
            poleType = (poleType == MagnetPoleType.North) ? MagnetPoleType.South : MagnetPoleType.North;

            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = (poleType == MagnetPoleType.North) ? Color.red : Color.blue;
            }

            Debug.Log($"{gameObject.name} の極性を {poleType} に切り替えました。");
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = (poleType == MagnetPoleType.North) ? Color.red : Color.blue;
            Gizmos.DrawWireSphere(transform.position, magneticRange);
        }
    }
}