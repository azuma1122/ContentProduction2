using UnityEngine;

namespace Game.StageScene.Magnet
{
    /// <summary>
    /// 弾の衝突処理を担当するクラス
    /// - 他オブジェクトとの衝突を検知
    /// - 現在の磁極（N/S）に応じて、対応するブロックPrefabを生成
    /// - 衝突エフェクトの再生・弾の削除を行う
    /// </summary>
    public class BulletCollisionHandler : MonoBehaviour
    {
        [SerializeField] private GameObject _impactEffect; // 衝突時のエフェクトPrefab
        [SerializeField] private float _destroyDelay = 0.05f; // 衝突後に弾を消すまでの遅延時間

        // ====== 外部参照 ======
        private MagnetUIManager _uiManager;   // UIを介して磁力情報を取得
        private GameObject _fixedNBlockPrefab; // N極ブロックPrefab
        private GameObject _fixedSBlockPrefab; // S極ブロックPrefab

        /// <summary>
        /// 初期化処理
        /// - 弾生成時にBulletShootControllerから呼び出される
        /// </summary>
        public void Initialize(MagnetUIManager uiManager, GameObject nPrefab, GameObject sPrefab)
        {
            _uiManager = uiManager;
            _fixedNBlockPrefab = nPrefab;
            _fixedSBlockPrefab = sPrefab;
        }

        /// <summary>
        /// 衝突時に呼ばれるUnityイベント
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            // 衝突エフェクト再生
            if (_impactEffect != null)
            {
                Instantiate(_impactEffect, transform.position, Quaternion.identity);
            }

            // 現在の磁極を取得（enum型で受け取る）
            GameConstants.Layer currentType =
                _uiManager != null ? _uiManager.GetCurrentMagnetType() : GameConstants.Layer.N_MAGNET;

            // 衝突地点に対応するブロックを生成
            GameObject prefabToSpawn = null;

            if (currentType == GameConstants.Layer.N_MAGNET)
            {
                prefabToSpawn = _fixedNBlockPrefab;
            }
            else if (currentType == GameConstants.Layer.S_MAGNET)
            {
                prefabToSpawn = _fixedSBlockPrefab;
            }

            if (prefabToSpawn != null)
            {
                Instantiate(prefabToSpawn, transform.position, Quaternion.identity);
            }

            // 弾を削除
            Destroy(gameObject, _destroyDelay);
        }
    }
}
