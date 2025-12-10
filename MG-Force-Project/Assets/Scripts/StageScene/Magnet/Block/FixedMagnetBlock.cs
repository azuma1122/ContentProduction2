using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.StageScene.Magnet
{
    /// <summary>
    /// 固定された磁石ブロック
    /// - 自身は動かないが、範囲内の移動可能な磁石（MovingMagnetBlock）に磁力を加える
    /// - 弾による極性反転（TogglePolarity）やUI表示にも対応
    /// - MagnetManagerのBoot状態に応じて磁力のオン・オフを制御
    /// </summary>
    public class FixedMagnetBlock : MonoBehaviour
    {
        [Header("磁石の極タイプ")]
        [Tooltip("N極(赤)か S極(青)を設定する")]
        public MagnetPoleType poleType = MagnetPoleType.North;

        [Header("磁力の設定値")]
        [Range(0f, 100f)]
        [Tooltip("磁力の強さ。数値が大きいほど引き寄せ / 反発が強くなる")]
        public float magneticForce = 10f;

        [Tooltip("磁力の影響範囲。この距離内にある磁石と干渉する")]
        public float magneticRange = 5f;

        [Header("磁力表示用UI（Image）")]
        [Tooltip("磁力の強さをUIで表示するためのImage（FillAmountを使用）")]
        public Image magneticForceImage;

        // MagnetManagerへの参照
        private MagnetManager magnetManager;

        // UI表示などのための最大値（正規化用）
        private const float MAX_FORCE = 100f;

        // 前フレームのBoot状態を記録
        private bool _wasBoot = false;

        private void Start()
        {
            // MagnetManagerを取得
            magnetManager = FindObjectOfType<MagnetManager>();
            if (magnetManager == null)
            {
                //Debug.LogError($"[FixedMagnetBlock] {gameObject.name} で MagnetManager が見つかりません！");
            }
            else
            {
                //Debug.Log($"[FixedMagnetBlock] {gameObject.name} で MagnetManager を取得しました");
            }

            // ゲーム開始時に磁力表示UIを初期化
            UpdateMagneticForceUI();
        }

        private void Update()
        {
            // デバッグ用：Bキー押下時の状態確認
            if (Input.GetKeyDown(KeyCode.B))
            {
                Debug.Log($"========== Bキー押下 [{gameObject.name}] ==========");
                Debug.Log($"  magnetManager: {(magnetManager != null ? "存在" : "null")}");
                if (magnetManager != null)
                {
                    Debug.Log($"  IsMagnetBoot: {magnetManager.IsMagnetBoot}");
                }
                Debug.Log($"=======================================");
            }
        }

        private void FixedUpdate()
        {
            // MagnetManagerが取得できていない場合は再取得を試みる
            if (magnetManager == null)
            {
                magnetManager = FindObjectOfType<MagnetManager>();
                if (magnetManager == null)
                {
                    return; // まだ見つからない場合はスキップ
                }
            }

            // Boot状態を取得
            bool isBootActive = magnetManager.IsMagnetBoot;

            // Boot状態が変化した時の処理
            if (isBootActive != _wasBoot)
            {
                if (isBootActive)
                {
                    // Bootがオンになった時
                    Debug.Log($"[FixedMagnetBlock] {gameObject.name} - Boot ON: 範囲内のオブジェクトを起動します");

                    // 範囲内の全てのMovingMagnetBlockをスリープから起こす
                    WakeUpNearbyMovingBlocks();
                }
                else
                {
                    // Bootがオフになった時
                    Debug.Log($"[FixedMagnetBlock] {gameObject.name} - Boot OFF");
                }

                _wasBoot = isBootActive;
            }

            // Bootがオフの場合は磁力処理をスキップ
            if (!isBootActive)
            {
                return;
            }

            // --- ここから磁力処理（Bootがオンの時のみ実行） ---

            // 一定範囲内にあるオブジェクトを検出
            Collider[] colliders = Physics.OverlapSphere(transform.position, magneticRange);

            foreach (Collider collider in colliders)
            {
                // MovingMagnetBlock（動く磁石）を取得
                MovingMagnetBlock movingBlock = collider.GetComponent<MovingMagnetBlock>();

                // 自分自身ではなく、磁力の影響を受ける対象にのみ処理を行う
                if (movingBlock != null)
                {
                    ApplyMagneticForce(movingBlock);
                }
            }
        }

        /// <summary>
        /// 範囲内の全てのMovingMagnetBlockをスリープから起こす
        /// </summary>
        private void WakeUpNearbyMovingBlocks()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, magneticRange);

            foreach (Collider collider in colliders)
            {
                MovingMagnetBlock movingBlock = collider.GetComponent<MovingMagnetBlock>();
                if (movingBlock != null)
                {
                    Rigidbody rb = movingBlock.GetComponent<Rigidbody>();
                    if (rb != null && rb.IsSleeping())
                    {
                        rb.WakeUp();
                        Debug.Log($"[FixedMagnetBlock] {movingBlock.gameObject.name} をスリープから復帰させました");
                    }
                }
            }
        }

        /// <summary>
        /// 移動可能な磁石に磁力を加える
        /// </summary>
        /// <param name="otherBlock">影響を与える対象の磁石</param>
        private void ApplyMagneticForce(MovingMagnetBlock otherBlock)
        {
            // 対象のRigidbodyを取得
            Rigidbody otherRb = otherBlock.GetComponent<Rigidbody>();
            if (otherRb == null) return;

            // 対象がスリープ状態なら起こす
            if (otherRb.IsSleeping())
            {
                otherRb.WakeUp();
            }

            // 自分 → 相手への方向を計算
            Vector3 direction = transform.position - otherBlock.transform.position;
            float distance = direction.magnitude;

            // 範囲外または距離が極端に近すぎる場合はスキップ
            if (distance > magneticRange || distance <= 0.01f) return;

            // 極が同じなら反発（-1）、異なれば引き寄せ（+1）
            float forceMultiplier = (poleType == otherBlock.poleType) ? -1f : 1f;

            // 距離に反比例して力を減衰（1 / distance）
            float force = (magneticForce * 10f / distance) * forceMultiplier;

            // 対象に力を加える（方向ベクトル × 磁力）
            otherRb.AddForce(direction.normalized * force, ForceMode.Force);
        }

        /// <summary>
        /// 外部またはデバッグから磁力値を変更する
        /// </summary>
        /// <param name="newForce">新しい磁力値</param>
        public void SetMagneticForce(float newForce)
        {
            // 値を安全な範囲にクランプして適用
            magneticForce = Mathf.Clamp(newForce, 0f, MAX_FORCE);
            UpdateMagneticForceUI();
        }

        /// <summary>
        /// UI（Image）に現在の磁力値を反映
        /// </summary>
        private void UpdateMagneticForceUI()
        {
            if (magneticForceImage != null)
                magneticForceImage.fillAmount = Mathf.Clamp01(magneticForce / MAX_FORCE);
        }

        /// <summary>
        /// 磁石の極性を反転する（N ↔ S）
        /// 弾などでヒットした際に呼び出される想定
        /// </summary>
        public void TogglePolarity()
        {
            // N ↔ S 切り替え
            poleType = (poleType == MagnetPoleType.North) ? MagnetPoleType.South : MagnetPoleType.North;

            // 見た目を変更（赤＝N極、青＝S極）
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = (poleType == MagnetPoleType.North) ? Color.red : Color.blue;

            Debug.Log($"{gameObject.name} の極性を {poleType} に切り替えました。");
        }

        /// <summary>
        /// シーンビューで磁力範囲を可視化
        /// </summary>
        private void OnDrawGizmos()
        {
            // N極＝赤 / S極＝青 で範囲を表示
            Gizmos.color = (poleType == MagnetPoleType.North) ? Color.red : Color.blue;
            Gizmos.DrawWireSphere(transform.position, magneticRange);
        }
    }
}