using UnityEngine;

namespace Game.StageScene.Magnet
{
    /// <summary>
    /// 弾（Bullet）の動作を制御するクラス
    /// - 発射方向の計算
    /// - 移動（物理挙動）
    /// - 一定時間後の自動削除
    /// - オブジェクト衝突時の破壊・置き換え処理（UI状態によってPrefabを切り替え）
    /// </summary>
    public class BulletController : MonoBehaviour
    {
        // ===== タグ定義 =====
        private const string FIXED_TAG = GameConstants.Tag.FIXED;
        private const string MOVING_TAG = GameConstants.Tag.MOVING;

        // ===== 定数 =====
        private const float INIT_SPEED = 10.0f;
        private const float LIFE_TIME = 12.0f;

        // ===== 参照 =====
        [Header("物理挙動用"), SerializeField] private Rigidbody _rigidbody = null;
        [Header("アニメーション制御"), SerializeField] private Animator _animator;

        [Header("置き換え用Prefab(Fixed)")]
        [SerializeField] private GameObject _fixedSPrefab;
        [SerializeField] private GameObject _fixedNPrefab;

        [Header("置き換え用Prefab(Moving)")]
        [SerializeField] private GameObject _movingSPrefab;
        [SerializeField] private GameObject _movingNPrefab;

        // UIを自動検出（N / S 切り替え用）
        private GameObject _uiNMagnet;
        private GameObject _uiSMagnet;

        // 発射関連
        private Vector3 _targetPos = Vector3.zero;
        private Vector3 _direction = Vector3.zero;
        private float _bulletSpeed = INIT_SPEED;
        private float _timer = 0f;

        private void Start()
        {
            // Rigidbodyを取得
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody == null)
                Debug.LogWarning("Rigidbodyが設定されていません。");

            // Animatorを取得
            _animator = GetComponent<Animator>();

            // UIを自動検出（N/S UI）
            _uiNMagnet = GameObject.Find("N_Magnet_UI") ?? GameObject.Find("N_Magnet");
            _uiSMagnet = GameObject.Find("S_Magnet_UI") ?? GameObject.Find("S_Magnet");

            if (_uiNMagnet == null || _uiSMagnet == null)
                Debug.LogWarning("N_Magnet_UI または S_Magnet_UI がシーン内に見つかりません。");

            // 発射方向を設定
            _targetPos = BulletLineController.GetDirection();
            FiringBullet();
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer > LIFE_TIME)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 弾をターゲット方向へ発射
        /// </summary>
        private void FiringBullet()
        {
            _direction = (_targetPos - transform.position).normalized * _bulletSpeed;
            _rigidbody.AddForce(_direction, ForceMode.Impulse);
        }

        /// <summary>
        /// 他オブジェクトとの衝突処理
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            // Fixed_Not_Block_Prefab(Clone)に当ったら消去
            if (other.gameObject.name == "Fixed_Not_Block_Prefab(Clone)")
            {
                Debug.Log("弾を削除します");
                Destroy(gameObject);
                return;
            }

            // 名前に「Fixed」が含まれているオブジェクトを対象
            if (other.gameObject.name.Contains("Fixed"))
            {
                GameObject prefabToSpawn = GetPrefabBasedOnUI(isMoving: false);
                if (prefabToSpawn != null)
                {
                    ReplaceBlock(other.gameObject, prefabToSpawn);
                }

                Destroy(gameObject); // 弾を削除
            }
            // Moving系ブロックに当たった場合（新規追加）
            else if (other.CompareTag(MOVING_TAG) || other.gameObject.name.Contains("Moving"))
            {
                GameObject prefabToSpawn = GetPrefabBasedOnUI(isMoving: true);
                if (prefabToSpawn != null)
                {
                    ReplaceBlock(other.gameObject, prefabToSpawn);
                }
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 現在のUI状態に応じてPrefabを決定（Fixed / Moving両対応）
        /// </summary>
        private GameObject GetPrefabBasedOnUI(bool isMoving)
        {
            bool sMagnetActive = _uiSMagnet != null && _uiSMagnet.activeSelf;
            bool nMagnetActive = _uiNMagnet != null && _uiNMagnet.activeSelf;

            if (isMoving)
            {
                if (sMagnetActive && !nMagnetActive)
                {
                    Debug.Log("UI状態: Sアクティブ → Moving_S_Block_Prefabに置き換え");
                    return _movingSPrefab;
                }
                else if (nMagnetActive && !sMagnetActive)
                {
                    Debug.Log("UI状態: Nアクティブ → Moving_N_Block_Prefabに置き換え");
                    return _movingNPrefab;
                }
            }
            else
            {
                if (sMagnetActive && !nMagnetActive)
                {
                    Debug.Log("UI状態: Sアクティブ → Fixed_S_Block_Prefabに置き換え");
                    return _fixedSPrefab;
                }
                else if (nMagnetActive && !sMagnetActive)
                {
                    Debug.Log("UI状態: Nアクティブ → Fixed_N_Block_Prefabに置き換え");
                    return _fixedNPrefab;
                }
            }

            Debug.LogWarning("どちらのUIもアクティブではないため、置き換え対象なし");
            return null;
        }

        /// <summary>
        /// 対象ブロックをPrefabに置き換える（何度でも切り替え可能）
        /// </summary>
        private void ReplaceBlock(GameObject oldObj, GameObject newPrefab)
        {
            if (oldObj == null || newPrefab == null) return;

            Vector3 pos = oldObj.transform.position;
            Quaternion rot = oldObj.transform.rotation;
            Transform parent = oldObj.transform.parent;

            // 元のオブジェクトを削除して新しいPrefabを生成
            Destroy(oldObj);
            Instantiate(newPrefab, pos, rot, parent);
        }
    }
}
