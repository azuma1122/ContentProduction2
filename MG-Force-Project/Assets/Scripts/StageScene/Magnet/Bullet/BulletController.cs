using Game.GameSystem;
using UnityEngine;

namespace Game.StageScene.Magnet
{
    /// <summary>
    /// 弾（Bullet）の動作を制御するクラス
    /// - 発射方向の計算
    /// - 移動（物理挙動）
    /// - 一定時間後の自動削除
    /// - オブジェクト衝突時の破壊・置き換え処理（UI状態によってPrefabを切り替え）
    /// - チャージレベルに応じたMovingブロックへの磁力付与制御
    /// </summary>
    public class BulletController : MonoBehaviour
    {
        // ===== タグ定義 =====
        private const string FIXED_TAG = GameConstants.Tag.FIXED;
        private const string MOVING_TAG = GameConstants.Tag.MOVING;

        // ===== 定数 =====
        private const float INIT_SPEED = 10.0f;
        private const float LIFE_TIME = 12.0f;

        // チャージレベルの閾値
        private const float CHARGE_LEVEL_1 = 0f;    // 緑(0-33%) → Moving_1対応
        private const float CHARGE_LEVEL_2 = 33f;   // 黄(33-66%) → Moving_2対応
        private const float CHARGE_LEVEL_3 = 66f;   // 赤(66-100%) → Moving_3対応

        // ===== 参照 =====
        [Header("物理挙動用"), SerializeField] private Rigidbody _rigidbody = null;
        [Header("アニメーション制御"), SerializeField] private Animator _animator;

        [Header("置き換え用Prefab(Fixed)")]
        [SerializeField] private GameObject _fixedSPrefab;
        [SerializeField] private GameObject _fixedNPrefab;

        [Header("置き換え用Prefab(Moving)")]
        [SerializeField] private GameObject _movingSPrefab;
        [SerializeField] private GameObject _movingNPrefab;

        [Header("磁石UI参照（オプション: 自動検索も可能）")]
        [SerializeField] private GameObject _uiNMagnetManual;
        [SerializeField] private GameObject _uiSMagnetManual;

        // UIを自動検出（N / S 切り替え用）
        private GameObject _uiNMagnet;
        private GameObject _uiSMagnet;

        // 代替案: MagnetManagerから直接極性を取得
        private MagnetManager _magnetManager;
        private bool _useAlternativeMethod = false;

        // 発射関連
        private Vector3 _shootDirection = Vector3.zero;
        private bool _isDirectionSet = false;
        private float _timer = 0f;

        // チャージパワー
        private float _chargePower = 0f;

        // 衝突処理フラグ（一度だけ処理を行う）
        private bool _hasCollided = false;

        /// <summary>
        /// 初期化処理
        /// Rigidbody、Animator、MagnetManagerを取得し、移動SEを再生
        /// </summary>
        private void Start()
        {
            Debug.Log("=== [BulletController] Start() 開始 ===");

            // Rigidbodyを取得
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody == null)
            {
                Debug.LogError("[BulletController] Rigidbodyが設定されていません！");
            }
            else
            {
                Debug.Log($"[BulletController] Rigidbody取得成功 - CollisionDetection: {_rigidbody.collisionDetectionMode}");
            }

            // Animatorを取得
            _animator = GetComponent<Animator>();

            // MagnetManagerを取得
            _magnetManager = FindObjectOfType<MagnetManager>();
            if (_magnetManager == null)
            {
                Debug.LogWarning("[BulletController] MagnetManagerが見つかりません");
            }

            // SE弾発射移動中
            try
            {
                if (SEManager.instance != null)
                {
                    SEManager.instance.PlaySE(SEManager.Bullet.BULLET_MOVE);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[BulletController] SE再生エラー: {e.Message}");
            }

            Debug.Log("=== [BulletController] Start() 完了 ===");
        }

        /// <summary>
        /// 毎フレームの更新処理
        /// 生存時間を管理し、一定時間経過後に自動削除
        /// </summary>
        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer > LIFE_TIME)
            {
                Debug.Log($"[BulletController] 生存時間超過 ({_timer:F2}秒) → 削除");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// UIオブジェクトを遅延初期化（必要になった時に検索）
        /// 手動設定 → 自動検索 → 代替方法の順に試行
        /// </summary>
        private void EnsureUIInitialized()
        {
            // 既に両方見つかっていればスキップ
            if (_uiNMagnet != null && _uiSMagnet != null)
                return;

            // 既に代替方法を使用していればスキップ
            if (_useAlternativeMethod)
                return;

            // まず手動設定をチェック
            if (_uiNMagnetManual != null && _uiNMagnet == null)
            {
                _uiNMagnet = _uiNMagnetManual;
                // Debug.Log($"[BulletController] N磁石UI設定完了（手動）: {_uiNMagnet.name}");
            }

            if (_uiSMagnetManual != null && _uiSMagnet == null)
            {
                _uiSMagnet = _uiSMagnetManual;
                // Debug.Log($"[BulletController] S磁石UI設定完了（手動）: {_uiSMagnet.name}");
            }

            // 手動設定がない場合は自動検索
            if (_uiNMagnet == null)
            {
                _uiNMagnet = FindUIObject(new string[] {
                    "N_Magnet_UI", "N_Magnet", "NMagnetUI", "NMagnet",
                    "N Magnet UI", "N Magnet", "MagnetUI_N", "UI_N_Magnet",
                    "Button_N", "ButtonN", "N_Button", "NButton"
                });
                // if (_uiNMagnet != null)
                // {
                //     Debug.Log($"[BulletController] N磁石UI検出（自動）: {_uiNMagnet.name}");
                // }
            }

            if (_uiSMagnet == null)
            {
                _uiSMagnet = FindUIObject(new string[] {
                    "S_Magnet_UI", "S_Magnet", "SMagnetUI", "SMagnet",
                    "S Magnet UI", "S Magnet", "MagnetUI_S", "UI_S_Magnet",
                    "Button_S", "ButtonS", "S_Button", "SButton"
                });
                // if (_uiSMagnet != null)
                // {
                //     Debug.Log($"[BulletController] S磁石UI検出（自動）: {_uiSMagnet.name}");
                // }
            }

            // どちらも見つからない場合は代替方法を使用
            if (_uiNMagnet == null && _uiSMagnet == null)
            {
                Debug.LogWarning("[BulletController] 磁石UIが見つかりません。MagnetManagerから直接極性を取得します。");
                // Debug.Log("[BulletController] ヒント: BulletControllerのInspectorで「磁石UI参照」に手動でUIオブジェクトをドラッグ&ドロップしてください");
                // SearchAllUIObjects(); // デバッグ用：一度だけ全オブジェクトを検索
                _useAlternativeMethod = true;
            }
        }

        /// <summary>
        /// シーン内の全UIオブジェクトを検索してログ出力（デバッグ用）
        /// </summary>
        private void SearchAllUIObjects()
        {
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            // Debug.Log("[BulletController] === シーン内の全オブジェクト検索 ===");

            int count = 0;
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.ToLower().Contains("magnet"))
                {
                    // Debug.Log($"  - {obj.name} (Layer: {obj.layer}, Tag: {obj.tag}, Active: {obj.activeSelf})");
                    count++;
                }
            }

            // Debug.Log($"[BulletController] === 検索完了: {count}個の磁石関連オブジェクト発見 ===");
        }

        /// <summary>
        /// 複数の候補名からUIオブジェクトを検索
        /// </summary>
        private GameObject FindUIObject(string[] candidateNames)
        {
            foreach (string name in candidateNames)
            {
                GameObject obj = GameObject.Find(name);
                if (obj != null)
                {
                    return obj;
                }
            }
            return null;
        }

        /// <summary>
        /// 発射方向を設定（BulletShootControllerから呼ばれる）
        /// </summary>
        public void SetShootDirection(Vector3 direction)
        {
            _shootDirection = direction.normalized;
            _isDirectionSet = true;
            // Debug.Log($"[BulletController] 発射方向設定: {_shootDirection}");
        }

        /// <summary>
        /// 射撃時のチャージパワーを設定（BulletShootControllerから呼ばれる）
        /// </summary>
        public void SetChargePower(float power)
        {
            _chargePower = power;
            Debug.Log($"[BulletController] チャージパワー設定: {_chargePower}%");
        }

        /// <summary>
        /// 他オブジェクトとの衝突処理
        /// OnTriggerEnterが呼ばれた時点で衝突が検出されている
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("================================================");
            Debug.Log($"[OnTriggerEnter] 衝突検出");
            Debug.Log($"  衝突相手名: {other.gameObject.name}");
            Debug.Log($"  衝突相手タグ: {other.tag}");
            Debug.Log($"  衝突相手レイヤー: {LayerMask.LayerToName(other.gameObject.layer)}");
            Debug.Log($"  弾の現在位置: {transform.position}");
            Debug.Log($"  弾の速度: {(_rigidbody != null ? _rigidbody.velocity.ToString() : "Rigidbody null")}");
            Debug.Log($"  既に衝突済みフラグ: {_hasCollided}");
            Debug.Log($"  チャージパワー: {_chargePower}%");
            Debug.Log("================================================");

            // 既に衝突処理済みなら何もしない
            if (_hasCollided)
            {
                Debug.LogWarning("[OnTriggerEnter] 既に衝突処理済み → 処理をスキップ");
                return;
            }

            // ポーズ中は衝突判定を行わない
            if (GlobalUIManager.Instance != null && GlobalUIManager.Instance.IsPaused)
            {
                Debug.LogWarning("[OnTriggerEnter] ポーズ中 → 処理をスキップ");
                return;
            }

            Debug.Log("[OnTriggerEnter] → UI初期化を実行");
            EnsureUIInitialized();

            // Fixed_Not_Block_Prefab(Clone)に当ったら消去
            if (other.gameObject.name == "Fixed_Not_Block_Prefab(Clone)")
            {
                Debug.Log("[OnTriggerEnter] → Fixed_Not_Block検出 → 弾を削除");
                _hasCollided = true;
                Destroy(gameObject);
                return;
            }

            // 名前に「Fixed」が含まれているオブジェクトを対象
            if (other.gameObject.name.Contains("Fixed"))
            {
                Debug.Log($"[OnTriggerEnter] → Fixedブロック検出: {other.gameObject.name}");
                _hasCollided = true;

                GameObject prefabToSpawn = GetPrefabBasedOnUI(isMoving: false);
                Debug.Log($"[OnTriggerEnter] → 取得したPrefab: {(prefabToSpawn != null ? prefabToSpawn.name : "null")}");

                if (prefabToSpawn != null)
                {
                    Debug.Log("[OnTriggerEnter] → ReplaceBlockSafe()を呼び出し");
                    ReplaceBlockSafe(other.gameObject, prefabToSpawn);
                }
                else
                {
                    Debug.LogError("[OnTriggerEnter] エラー Fixed用のPrefabが取得できませんでした");
                }

                Debug.Log("[OnTriggerEnter] → 弾を0.02秒後に削除");
                Destroy(gameObject, 0.02f);
                return;
            }
            // Moving系ブロックに当たった場合（チャージレベル判定追加）
            else if (other.CompareTag(MOVING_TAG) || other.gameObject.name.Contains("Moving"))
            {
                Debug.Log($"[OnTriggerEnter] → Movingブロック検出: {other.gameObject.name}");
                _hasCollided = true;

                // チャージレベルに応じて処理可能か判定
                bool canAffect = CanAffectMovingBlock(other.gameObject);
                Debug.Log($"[OnTriggerEnter] → チャージ判定結果: {(canAffect ? "OK" : "NG")}");

                if (canAffect)
                {
                    GameObject prefabToSpawn = GetPrefabBasedOnUI(isMoving: true);
                    Debug.Log($"[OnTriggerEnter] → 取得したPrefab: {(prefabToSpawn != null ? prefabToSpawn.name : "null")}");

                    if (prefabToSpawn != null)
                    {
                        Debug.Log("[OnTriggerEnter] → ReplaceBlockSafe()を呼び出し");
                        ReplaceBlockSafe(other.gameObject, prefabToSpawn);
                    }
                    else
                    {
                        Debug.LogError("[OnTriggerEnter] エラー Moving用のPrefabが取得できませんでした");
                    }
                }
                else
                {
                    Debug.LogWarning($"[OnTriggerEnter] チャージ不足 {other.gameObject.name}には影響を与えられません（現在{_chargePower}%）");
                }

                Debug.Log("[OnTriggerEnter] → 弾を0.02秒後に削除");
                Destroy(gameObject, 0.02f);
            }
            else
            {
                Debug.LogWarning($"[OnTriggerEnter] → 未対応のオブジェクト: {other.gameObject.name}");
            }
        }

        /// <summary>
        /// チャージレベルに応じてMovingブロックに影響を与えられるか判定
        /// </summary>
        private bool CanAffectMovingBlock(GameObject movingBlock)
        {
            string blockName = movingBlock.name;

            // Moving_1_Block → 緑以上(0%以上)で動かせる
            if (blockName.Contains("Moving_1_Block"))
            {
                return _chargePower >= CHARGE_LEVEL_1;
            }
            // Moving_2_Block → 黄以上(33%以上)で動かせる
            else if (blockName.Contains("Moving_2_Block"))
            {
                bool canMove = _chargePower >= CHARGE_LEVEL_2;
                // if (!canMove)
                //     Debug.Log($"Moving_2_Blockには黄色チャージ(33%以上)が必要です");
                return canMove;
            }
            // Moving_3_Block → 赤のみ(66%以上)で動かせる
            else if (blockName.Contains("Moving_3_Block"))
            {
                bool canMove = _chargePower >= CHARGE_LEVEL_3;
                // if (!canMove)
                //     Debug.Log($"Moving_3_Blockには赤チャージ(66%以上)が必要です");
                return canMove;
            }

            // デフォルトでは影響可能
            return true;
        }

        /// <summary>
        /// 現在のUI状態に応じてPrefabを決定（Fixed / Moving両対応）
        /// UIの表示状態またはMagnetManagerから極性を判定してPrefabを選択
        /// </summary>
        private GameObject GetPrefabBasedOnUI(bool isMoving)
        {
            Debug.Log($"[GetPrefabBasedOnUI] 開始 - isMoving: {isMoving}");

            // UIを再確認
            EnsureUIInitialized();

            // 代替方法を使用する場合（UIが見つからない）
            if (_useAlternativeMethod && _magnetManager != null)
            {
                bool isNorth = GetCurrentPolarityFromManager();
                Debug.Log($"[GetPrefabBasedOnUI] 代替方法使用 - 極性: {(isNorth ? "N極" : "S極")}");

                if (isMoving)
                {
                    GameObject result = isNorth ? _movingNPrefab : _movingSPrefab;
                    Debug.Log($"[GetPrefabBasedOnUI] → 結果(Moving): {(result != null ? result.name : "null")}");
                    return result;
                }
                else
                {
                    GameObject result = isNorth ? _fixedNPrefab : _fixedSPrefab;
                    Debug.Log($"[GetPrefabBasedOnUI] → 結果(Fixed): {(result != null ? result.name : "null")}");
                    return result;
                }
            }

            // 通常の方法（UIから取得）
            bool sMagnetActive = _uiSMagnet != null && _uiSMagnet.activeSelf;
            bool nMagnetActive = _uiNMagnet != null && _uiNMagnet.activeSelf;

            Debug.Log($"[GetPrefabBasedOnUI] UI状態 - S_UI: {(_uiSMagnet != null ? _uiSMagnet.activeSelf.ToString() : "null")}, N_UI: {(_uiNMagnet != null ? _uiNMagnet.activeSelf.ToString() : "null")}");

            if (isMoving)
            {
                if (sMagnetActive && !nMagnetActive)
                {
                    Debug.Log("[GetPrefabBasedOnUI] UI状態: Sアクティブ → Moving_S_Block_Prefab");
                    return _movingSPrefab;
                }
                else if (nMagnetActive && !sMagnetActive)
                {
                    Debug.Log("[GetPrefabBasedOnUI] UI状態: Nアクティブ → Moving_N_Block_Prefab");
                    return _movingNPrefab;
                }
            }
            else
            {
                if (sMagnetActive && !nMagnetActive)
                {
                    Debug.Log("[GetPrefabBasedOnUI] UI状態: Sアクティブ → Fixed_S_Block_Prefab");
                    return _fixedSPrefab;
                }
                else if (nMagnetActive && !sMagnetActive)
                {
                    Debug.Log("[GetPrefabBasedOnUI] UI状態: Nアクティブ → Fixed_N_Block_Prefab");
                    return _fixedNPrefab;
                }
            }

            Debug.LogWarning("[GetPrefabBasedOnUI] どちらのUIもアクティブではないため、置き換え対象なし");
            return null;
        }

        /// <summary>
        /// MagnetManagerから現在の極性を取得（代替方法）
        /// </summary>
        private bool GetCurrentPolarityFromManager()
        {
            if (_magnetManager == null)
            {
                Debug.LogWarning("[GetCurrentPolarityFromManager] MagnetManagerが見つからないため、デフォルトでN極を返します");
                return true;
            }

            // 仮実装: チャージパワーが50%以上ならN極、それ以外はS極
            return _chargePower >= 50f;
        }

        /// <summary>
        /// 対象ブロックをPrefabに置き換える（安全版）
        /// 先に新オブジェクトを生成してから元オブジェクトを削除することで、
        /// 置き換え処理の確実性を向上
        /// </summary>
        private void ReplaceBlockSafe(GameObject oldObj, GameObject newPrefab)
        {
            Debug.Log("[ReplaceBlockSafe] 開始");

            if (oldObj == null || newPrefab == null)
            {
                Debug.LogError($"[ReplaceBlockSafe] null参照エラー oldObj: {(oldObj != null ? "OK" : "null")}, newPrefab: {(newPrefab != null ? "OK" : "null")}");
                return;
            }

            Vector3 pos = oldObj.transform.position;
            Quaternion rot = oldObj.transform.rotation;
            Transform parent = oldObj.transform.parent;

            Debug.Log($"[ReplaceBlockSafe] 置き換え実行");
            Debug.Log($"  元オブジェクト: {oldObj.name}");
            Debug.Log($"  新Prefab: {newPrefab.name}");
            Debug.Log($"  位置: {pos}");
            Debug.Log($"  親: {(parent != null ? parent.name : "null")}");

            // 先に新しいオブジェクトを生成
            GameObject newObj = Instantiate(newPrefab, pos, rot, parent);

            if (newObj != null)
            {
                Debug.Log($"[ReplaceBlockSafe] 新オブジェクト生成成功: {newObj.name}");

                // 生成成功後に元のオブジェクトを破壊
                Debug.Log($"[ReplaceBlockSafe] 元オブジェクト削除: {oldObj.name}");
                Destroy(oldObj);
            }
            else
            {
                Debug.LogError("[ReplaceBlockSafe] 新オブジェクトの生成に失敗しました");
            }
        }
    }
}