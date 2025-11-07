using UnityEngine;

namespace Game.StageScene.Magnet
{
    /// <summary>
    /// 固定された磁石ブロックを表すクラス
    /// - このブロックは動かず、その場で「磁石のような見た目」を演出する
    /// - 実際の磁力（物理的な力）は発生させない
    /// - ゲーム中では、ギミックやステージ演出として使用する想定
    /// </summary>
    public class FixedMagnetBlock : MonoBehaviour
    {
        [Header("磁力が届く範囲（見た目用の目安）")]
        [SerializeField]
        private float magnetRange = 5f; // シーンビュー上で可視化する磁力範囲（あくまで目安）

        [Header("磁石の極タイプ")]
        public PoleType poleType; // N極かS極かを指定

        /// <summary>
        /// 磁石の極タイプ（North=青、South=赤）
        /// ※見た目だけの区別で、実際の磁力は発生しない
        /// </summary>
        public enum PoleType
        {
            North, // N極
            South  // S極
        }

        /// <summary>
        /// シーンビュー上で選択時に磁力範囲を可視化する
        /// - 実際のゲームプレイ中には表示されない
        /// - 極に応じて色を変えて区別しやすくしている
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // N極なら青っぽく、S極なら赤っぽく表示
            Gizmos.color = poleType == PoleType.North
                ? new Color(0, 0, 1, 0.25f)
                : new Color(1, 0, 0, 0.25f);

            // 磁力範囲の目安を円で描画
            Gizmos.DrawWireSphere(transform.position, magnetRange);
        }
    }
}
