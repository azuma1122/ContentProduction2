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
        [HideInInspector]
        [SerializeField]
        private bool isBootActive = false;

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

        // Rigidbodyキャッシュ
        private Rigidbody _rigidbody;

        // MagnetManagerへの参照
        private MagnetManager magnetManager;

        // 最大磁力値（UI正規化用）
        private const float MAX_FORCE = 100f;

        private void Start()
        {
            // Rigidbodyを取得してキャッシュ化（毎フレーム GetComponent を避けるため）
            _rigidbody = GetComponent<Rigidbody>();

            // MagnetManagerを取得
            magnetManager = FindObjectOfType<MagnetManager>();
            if (magnetManager == null)
            {
                Debug.LogError($"[MovingMagnetBlock] {gameObject.name} で MagnetManager が見つかりません！");
            }

            // 開始時にUI更新
            UpdateMagneticForceUI();
        }

        private void FixedUpdate()
        {
            // MagnetManagerからBoot状態を取得
            if (magnetManager != null)
            {
                isBootActive = magnetManager.IsMagnetBoot;
            }

            if (!isBootActive)
            {
                return; //Bootが押された時だけ
            }

            // 指定範囲内にある全てのColliderを取得
            Collider[] colliders = Physics.OverlapSphere(transform.position, magneticRange);

            foreach (Collider collider in colliders)
            {
                // 自分以外の MovingMagnetBlock を探す
                MovingMagnetBlock other = collider.GetComponent<MovingMagnetBlock>();

                // 自分自身以外の磁石が見つかった場合のみ処理
                if (other != null && other != this)
                {
                    ApplyMagneticForce(other);
                }

                // 重要：ObstaclesObjectControllerへの磁力適用は削除
                // MagnetControllerがOnTriggerStayで処理するため、ここでは何もしない
            }
        }

        /// <summary>
        /// 他の磁石ブロックに対して磁力（引力または反発力）を加える
        /// </summary>
        /// <param name="other">磁力を及ぼす対象のブロック</param>
        private void ApplyMagneticForce(MovingMagnetBlock other)
        {
            // 対象がRigidbodyを持っていなければ何もしない
            Rigidbody otherRb = other.GetComponent<Rigidbody>();
            if (otherRb == null) return;

            // 自分 → 相手方向のベクトルを計算
            Vector3 direction = transform.position - other.transform.position;
            float distance = direction.magnitude;

            // 範囲外 or 距離が極端に近すぎる場合はスキップ
            if (distance > magneticRange || distance <= 0.01f) return;

            // 同じ極なら反発（-1）、異なる極なら引き寄せ（+1）
            float forceMultiplier = (poleType == other.poleType) ? -1f : 1f;

            // 距離に応じて減衰する磁力を計算（距離が近いほど強い）
            float force = (magneticForce * 10f / distance) * forceMultiplier;

            // 相手のRigidbodyに力を加える
            otherRb.AddForce(direction.normalized * force, ForceMode.Force);
        }

        /// <summary>
        /// 外部から磁力の強さを変更（例：デバッグUIなどで調整する場合）
        /// </summary>
        /// <param name="newForce">新しい磁力の値</param>
        public void SetMagneticForce(float newForce)
        {
            // 0〜MAX_FORCEの範囲に制限して設定
            magneticForce = Mathf.Clamp(newForce, 0f, MAX_FORCE);

            // UIに反映
            UpdateMagneticForceUI();
        }

        /// <summary>
        /// UIのFillAmountに磁力の強さを反映させる
        /// </summary>
        private void UpdateMagneticForceUI()
        {
            if (magneticForceImage != null)
            {
                // 0〜1の範囲に正規化してUI更新
                magneticForceImage.fillAmount = Mathf.Clamp01(magneticForce / MAX_FORCE);
            }
        }

        /// <summary>
        /// 磁石の極性を反転（N ↔ S）
        /// 弾が当たったときなどに呼び出す想定
        /// </summary>
        public void TogglePolarity()
        {
            // 現在の極性を切り替え
            poleType = (poleType == MagnetPoleType.North) ? MagnetPoleType.South : MagnetPoleType.North;

            // 極性に応じて色を変更（N=赤, S=青）
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = (poleType == MagnetPoleType.North) ? Color.red : Color.blue;
            }

            // デバッグログ出力
            Debug.Log($"{gameObject.name} の極性を {poleType} に切り替えました。");
        }

        /// <summary>
        /// シーンビューで磁力範囲を可視化
        /// </summary>
        private void OnDrawGizmos()
        {
            // N極＝赤 / S極＝青 で磁力範囲を描画
            Gizmos.color = (poleType == MagnetPoleType.North) ? Color.red : Color.blue;
            Gizmos.DrawWireSphere(transform.position, magneticRange);
        }
    }
}