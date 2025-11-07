using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.StageScene.Magnet
{
    /// <summary>
    /// 移動可能な磁石ブロックの挙動を制御するクラス
    /// - 磁力（引き寄せ・反発）のシミュレーション
    /// - 他の磁石との距離に応じた力の加算
    /// - 弾（Bullet）が当たったときに N極 / S極 を反転可能
    /// - Image UIを使って磁力の強さを表示
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class MovingMagnetBlock : MonoBehaviour
    {
        /// <summary>
        /// 磁石の極タイプ
        /// North → N極（赤） / South → S極（青）
        /// </summary>
        public enum PoleType { North, South }

        [Header("磁石の極タイプ")]
        [Tooltip("N極(赤)か S極(青)を設定する")]
        public PoleType poleType = PoleType.North;

        [Header("磁力の設定値")]
        [Tooltip("磁力の強さ。数値が大きいほど強く引き寄せ / 反発する")]
        [Range(0f, 100f)]
        public float magneticForce = 10f;

        [Tooltip("磁力の影響範囲。この距離内にある磁石と干渉する")]
        public float magneticRange = 5f;

        [Header("磁力表示用UI（Image）")]
        [Tooltip("磁力の強さを表示するImage（FillAmountを使用）")]
        public Image magneticForceImage;

        // Rigidbodyキャッシュ
        private Rigidbody _rigidbody;

        // 最大磁力（UIの正規化用）
        private const float MAX_FORCE = 100f;

        private void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();

            // 初期表示を反映
            UpdateMagneticForceUI();
        }

        private void FixedUpdate()
        {
            // 指定範囲内にあるコライダーを検出
            Collider[] colliders = Physics.OverlapSphere(transform.position, magneticRange);

            foreach (Collider collider in colliders)
            {
                // 他の磁石ブロックを取得
                MovingMagnetBlock otherBlock = collider.GetComponent<MovingMagnetBlock>();

                // 自身以外の磁石に対してのみ力を加える
                if (otherBlock != null && otherBlock != this)
                {
                    ApplyMagneticForce(otherBlock);
                }
            }
        }

        /// <summary>
        /// 磁力を外部またはデバッグ用に変更する
        /// </summary>
        public void SetMagneticForce(float newForce)
        {
            magneticForce = Mathf.Clamp(newForce, 0f, MAX_FORCE);
            UpdateMagneticForceUI();
        }

        /// <summary>
        /// ImageのfillAmountに現在の磁力を反映する
        /// </summary>
        private void UpdateMagneticForceUI()
        {
            if (magneticForceImage != null)
            {
                magneticForceImage.fillAmount = Mathf.Clamp01(magneticForce / MAX_FORCE);
            }
        }

        /// <summary>
        /// 磁力の作用を計算して、他のブロックに力を加える
        /// </summary>
        private void ApplyMagneticForce(MovingMagnetBlock otherBlock)
        {
            Rigidbody otherRb = otherBlock.GetComponent<Rigidbody>();
            if (otherRb == null) return;

            // 自分 → 相手への方向ベクトル
            Vector3 direction = otherBlock.transform.position - transform.position;
            float distance = direction.magnitude;

            // 範囲外や極端に近すぎる場合は処理をスキップ
            if (distance > magneticRange || distance <= 0.01f) return;

            // 同じ極性なら反発（-1）、異なる極性なら引き寄せ（+1）
            float forceMultiplier = (poleType == otherBlock.poleType) ? -1f : 1f;

            // 距離の二乗に反比例する磁力を算出
            float force = (magneticForce * 10f /distance) * forceMultiplier;

            // 相手に力を加える
            otherRb.AddForce(direction.normalized * force, ForceMode.Force);
        }

        /// <summary>
        /// 磁石の極性を反転する（N極 ↔ S極）
        /// 弾（Bullet）が当たったときなどに呼び出される
        /// </summary>
        public void TogglePolarity()
        {
            // 極性を反転
            poleType = (poleType == PoleType.North) ? PoleType.South : PoleType.North;

            // 見た目を色で区別（N=赤 / S=青）
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                if (poleType == PoleType.North)
                    renderer.material.color = Color.red;
                else
                    renderer.material.color = Color.blue;
            }

            // デバッグ出力
            Debug.Log($"{gameObject.name} の極性を {poleType} に切り替えました。");
        }

        /// <summary>
        /// エディタ上で磁力範囲を視覚化（デバッグ用）
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = (poleType == PoleType.North) ? Color.red : Color.blue;
            Gizmos.DrawWireSphere(transform.position, magneticRange);
        }
    }
}
