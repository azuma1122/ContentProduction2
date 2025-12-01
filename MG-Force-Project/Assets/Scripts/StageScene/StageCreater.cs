using Newtonsoft.Json;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Game.StageScene.Magnet;
using Game.GameSystem;
using Game.StageScene.Player;

namespace Game.StageScene
{
    /// <summary>
    /// JSON データからステージを生成するクラス（詳細コメント版）
    /// 
    /// 【主な機能】
    /// 1. JSONファイルからステージデータを読み込み
    /// 2. ブロック、ゴール、ギミック、Playerなどを配置
    /// 3. Playerを指定されたSpawnPointに正しく配置
    /// 4. 物理シミュレーションを適切に管理
    /// 
    /// 【処理の流れ】
    /// Awake() → 前のシーンのオブジェクトをクリーンアップ
    /// SetJsonAndCreate() → JSONを受け取ってステージ生成開始
    ///   ↓
    /// StageCreate() → ブロックとPlayerを生成（物理停止中）
    ///   ↓
    /// InitializePlayerAfterCreation() → Playerを正しい位置に配置して初期化
    ///   ↓
    /// 物理シミュレーション再開 → ゲーム開始
    /// </summary>
    public class StageCreater : MonoBehaviour
    {
        #region ===== 列挙型定義 =====

        /// <summary>通常ブロックのタイプ</summary>
        private enum ObjectType
        {
            NotObject,      // 0: 何もない
            NotFixed,       // 1: 固定されていない
            NFixed,         // 2: N極固定
            SFixed,         // 3: S極固定
            CanFixed,       // 4: 固定可能
            NotMoving_1,    // 5: 動かないブロック1
            NotMoving_2,    // 6: 動かないブロック2
            NotMoving_3,    // 7: 動かないブロック3
            CanMoving,      // 8: 動かせる
            NMoving,        // 9: N極で動く
            SMoving,        // 10: S極で動く
        }

        /// <summary>特殊オブジェクトのタイプ（負の値）</summary>
        private enum S_ObjectType
        {
            Main,           // 0: ステージの親
            Player = -1,    // -1: プレイヤー
            Goal = -2,      // -2: ゴール
            Gimmick = -3,   // -3: ギミック
            P_Gimmick = -4, // -4: プレイヤー用ギミック
            Moving_Floor = -11, // -11: 動く床
            CanUp = -12,    // -12: 上昇可能（Button）
        }

        #endregion

        #region ===== Inspector設定項目 =====

        private GameDataManager gameDataManager = GameDataManager.Instance;

        [NamedSerializeField(
            new string[]
            {
                "NotFixed","NFixed","SFixed","CanFixed","NotMoving_1","NotMoving_2","NotMoving_3","CanMoving","NMoving","SMoving",
            }
        )]
        [SerializeField] private GameObject[] Objects;
        [SerializeField] private GameObject[] _specialObjects;
        [SerializeField] private GameObject[] _bgObjects;

        [Header("=== Player設定 ===")]
        [Tooltip("生成するPlayerのPrefabを直接指定してください")]
        [SerializeField] private GameObject _playerPrefab;

        [Header("Player 初期位置（SpawnPoint指定時のみ使用）")]
        [Tooltip("Playerを配置するSpawnPointオブジェクトを指定")]
        [SerializeField] private Transform _playerSpawnPoint;

        [Header("=== Button位置調整 ===")]
        [Tooltip("Buttonを下げる量（デフォルト: 0.5f）")]
        [SerializeField] private float _buttonYOffset = 0.5f;

        #endregion

        #region ===== 定数定義 =====

        private const int MAX_ROWS = 25;
        private const int MAX_COLS = 38;
        private const float INIT_X = 1.0f;
        private const float INIT_Y = 1.0f;
        private const float INIT_Z = 0.0f;

        #endregion

        #region ===== 内部変数 =====

        private int _row;
        private int _col;
        private int[,] colorArray = new int[MAX_ROWS, MAX_COLS];
        private int[,] powerArray = new int[MAX_ROWS, MAX_COLS];

        public struct Scale
        {
            public int _row;
            public int _col;
            public Scale(int row, int col)
            {
                _row = row;
                _col = col;
            }
        }

        private Scale[,] scaleArray = new Scale[MAX_ROWS, MAX_COLS];
        private bool isPlayerCreate = false;
        private Scale zero = new Scale(0, 0);
        private string _jsonData = null;
        private bool _hasCreated = false;
        private Vector3 _playerJsonPosition;
        private bool _playerWasCreatedFromJson = false;
        private SimulationMode _originalSimulationMode;
        private GameObject _createdPlayer = null;

        #endregion

        #region ===== JSONデータ用クラス =====

        [System.Serializable]
        public class Item
        {
            public int color;
            public int power;
        }

        [System.Serializable]
        public class ItemWrapper
        {
            public string key;
            public Item value;
        }

        [System.Serializable]
        public class RootObject
        {
            [JsonProperty("items")]
            public List<ItemWrapper> items;
        }

        #endregion

        #region ===== Unityライフサイクルメソッド =====

        private void Awake()
        {
            Debug.Log($"StageCreater: Awake - シーン名={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            CleanupPreviousStageObjects();
            _hasCreated = false;
        }

        #endregion

        #region ===== クリーンアップ処理 =====

        private void CleanupPreviousStageObjects()
        {
            GameObject[] mainStages = GameObject.FindGameObjectsWithTag("MainStage");
            foreach (GameObject obj in mainStages)
            {
                Debug.Log($"StageCreater: MainStageタグのオブジェクトを削除 -> {obj.name}");
                Destroy(obj);
            }

            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject obj in players)
            {
                Debug.Log($"StageCreater: Playerタグのオブジェクトを削除 -> {obj.name}");
                Destroy(obj);
            }

            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj == null) continue;
                if (obj.scene.name == "DontDestroyOnLoad") continue;
                if (obj.GetComponent<Canvas>() != null || obj.GetComponentInParent<Canvas>() != null)
                {
                    continue;
                }
                if (obj.GetComponent<StageCreater>() != null)
                {
                    continue;
                }
                if (obj.name.Contains("MainStage"))
                {
                    Debug.Log($"StageCreater: MainStage名を含むオブジェクトを削除 -> {obj.name}");
                    Destroy(obj);
                }
            }

            Debug.Log("StageCreater: クリーンアップ完了");
        }

        #endregion

        #region ===== 外部から呼ばれる公開メソッド =====

        public void SetJsonAndCreate(string json)
        {
            if (_hasCreated)
            {
                Debug.LogWarning("StageCreater: 既に生成済みなので処理をスキップします");
                return;
            }

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("StageCreater: 渡された JSON が無効です");
                return;
            }

            _jsonData = json;
            StageCreate();
            StartCoroutine(InitializePlayerAfterCreation());
            _hasCreated = true;
        }

        public void BGCreate()
        {
            int current_index = gameDataManager.GetCurrentStageIndex();
            Transform cameraTransform = GameObject.Find(GameConstants.MAIN_CAMERA).transform;

            Vector3 bg_position = new Vector3(
                cameraTransform.position.x,
                cameraTransform.position.y + 1.0f,
                1.0f
            );

            Instantiate(_bgObjects[current_index], bg_position, Quaternion.identity, cameraTransform);
            Debug.Log($"StageCreater: 背景生成 Position={bg_position}");
        }

        #endregion

        #region ===== 内部状態の初期化 =====

        private void ResetInternalState()
        {
            _row = MAX_ROWS - 1;
            _col = 0;
            isPlayerCreate = false;
            _jsonData = _jsonData ?? string.Empty;
            _playerWasCreatedFromJson = false;
            _createdPlayer = null;

            for (int i = 0; i < MAX_ROWS; i++)
            {
                for (int j = 0; j < MAX_COLS; j++)
                {
                    colorArray[i, j] = (int)ObjectType.NotObject;
                    powerArray[i, j] = 0;
                    scaleArray[i, j] = new Scale(0, 0);
                }
            }
        }

        #endregion

        #region ===== JSONパース処理 =====

        private void GetStageDataFromJson()
        {
            if (string.IsNullOrEmpty(_jsonData))
            {
                return;
            }

            RootObject rootObject;
            try
            {
                rootObject = JsonConvert.DeserializeObject<RootObject>(_jsonData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"StageCreater: JSON のパースに失敗しました: {ex.Message}");
                return;
            }

            if (rootObject == null || rootObject.items == null)
            {
                return;
            }

            foreach (var itemWrapper in rootObject.items)
            {
                if (string.IsNullOrEmpty(itemWrapper?.key)) continue;

                try
                {
                    if (_row < 0)
                    {
                        Debug.LogWarning("StageCreater: 配列の行数を超えたため処理を中断");
                        break;
                    }

                    colorArray[_row, _col] = itemWrapper.value.color;
                    powerArray[_row, _col] = itemWrapper.value.power;
                    scaleArray[_row, _col] = new Scale(1, 1);

                    _col++;
                    if (_col >= MAX_COLS)
                    {
                        _col = 0;
                        _row--;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"StageCreater: 例外が発生しました {itemWrapper.key}: {ex.Message}");
                }
            }
        }

        #endregion

        #region ===== ステージ生成メイン処理 =====

        public void StageCreate()
        {
            Debug.Log("StageCreater: ステージ生成開始");

            _originalSimulationMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            Debug.Log($"StageCreater: 物理シミュレーションを一時停止 OriginalMode={_originalSimulationMode}");

            ResetInternalState();
            GetStageDataFromJson();

            Vector3 init_pos = GameConstants.LowerLeft;

            GameObject main_object = Instantiate(
                _specialObjects[(int)S_ObjectType.Main],
                init_pos,
                Quaternion.identity
            );
            main_object.tag = "MainStage";

            isPlayerCreate = true;

            Transform[,] transforms = new Transform[MAX_ROWS, MAX_COLS];

            for (int i = 0; i < MAX_ROWS; i++)
            {
                for (int j = 0; j < MAX_COLS; j++)
                {
                    if (scaleArray[i, j]._col == zero._col && scaleArray[i, j]._row == zero._row)
                        continue;

                    int currentColor = colorArray[i, j];

                    // Buttonオブジェクトのcolor値をログ出力
                    if (currentColor < 0)
                    {
                        Debug.Log($"特殊オブジェクト検出: color={currentColor}, 位置=({i},{j}), CanUpの値={(int)S_ObjectType.CanUp}");
                    }

                    GameObject obj = ObjectCreater(currentColor, powerArray[i, j]);

                    if (obj != null)
                    {
                        // Player が生成された場合は特別処理
                        if (currentColor == (int)S_ObjectType.Player)
                        {
                            _playerJsonPosition = obj.transform.position;
                            _playerWasCreatedFromJson = true;
                            _createdPlayer = obj;
                            Debug.Log($"StageCreater: Player を生成（位置調整前） Position={_playerJsonPosition}");
                            continue;
                        }

                        // 配置とスケール調整
                        obj.transform.localScale = new Vector3(scaleArray[i, j]._col, scaleArray[i, j]._row, 1.0f);

                        // 基本位置計算
                        float posX = INIT_X * j + ((obj.transform.localScale.x - 1) * 0.5f);
                        float posY = INIT_Y * i + ((obj.transform.localScale.y - 1) * 0.5f);
                        float posZ = INIT_Z;

                        // Button(CanUp)の場合は高さを調整
                        if (currentColor == (int)S_ObjectType.CanUp)
                        {
                            float originalY = posY;
                            posY -= _buttonYOffset;
                            Debug.Log($"StageCreater: Button位置調整実行！ color={currentColor}, 元Y={originalY} → 調整後Y={posY}, offset={_buttonYOffset}");
                        }

                        obj.transform.position = new Vector3(posX, posY, posZ);
                        obj.transform.SetParent(main_object.transform, false);
                        transforms[i, j] = obj.transform;
                    }

                    if (obj == null) continue;

                    // 特殊オブジェクトの追加調整
                    if (colorArray[i, j] == (int)S_ObjectType.Goal)
                    {
                        Vector3 obj_pos = obj.transform.position;
                        obj_pos.y += 0.5f;
                        obj.transform.position = obj_pos;
                    }
                    else if (colorArray[i, j] == (int)S_ObjectType.Gimmick)
                    {
                        Vector3 obj_pos = obj.transform.position;
                        obj_pos.y += 0.5f;
                        obj.transform.position = obj_pos;
                        obj.transform.localScale = new Vector3(10.0f, 10.0f, 10.0f);
                    }
                    else if (colorArray[i, j] == (int)S_ObjectType.CanUp)
                    {
                        // 生成後の追加調整（必要に応じて）
                        Vector3 obj_pos = obj.transform.position;
                        Debug.Log($"StageCreater: Button生成後の最終位置確認 Position={obj_pos}");
                    }
                }
            }

            Debug.Log("StageCreater: ステージ生成完了（物理シミュレーションは一時停止中）");
        }

        #endregion

        #region ===== Player初期化処理 =====

        private IEnumerator InitializePlayerAfterCreation()
        {
            Debug.Log($"StageCreater: InitializePlayerAfterCreation 開始 - シーン={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");

            if (!_playerWasCreatedFromJson || _createdPlayer == null)
            {
                Debug.LogWarning("StageCreater: Player が生成されていません");
                Physics.simulationMode = _originalSimulationMode;
                yield break;
            }

            yield return null;
            yield return null;
            yield return null;

            GameObject player = _createdPlayer;
            Debug.Log($"StageCreater: Player初期化開始 - {player.name}");

            // 既存コントローラーをすべて削除
            PlayerManager existingManager = player.GetComponent<PlayerManager>();
            PlayerStateController existingState = player.GetComponent<PlayerStateController>();
            PlayerMoveController existingMove = player.GetComponent<PlayerMoveController>();
            PlayerAnimationController existingAnim = player.GetComponent<PlayerAnimationController>();

            if (existingManager != null)
            {
                Debug.Log("StageCreater: 既存のPlayerManagerを削除");
                Destroy(existingManager);
            }
            if (existingState != null)
            {
                Debug.Log("StageCreater: 既存のPlayerStateControllerを削除");
                Destroy(existingState);
            }
            if (existingMove != null)
            {
                Debug.Log("StageCreater: 既存のPlayerMoveControllerを削除");
                Destroy(existingMove);
            }
            if (existingAnim != null)
            {
                Debug.Log("StageCreater: 既存のPlayerAnimationControllerを削除");
                Destroy(existingAnim);
            }

            yield return null;
            yield return null;

            // SpawnPoint検索
            Vector3 targetPosition;
            Quaternion targetRotation;

            if (_playerSpawnPoint == null)
            {
                GameObject spawnPointObj = GameObject.Find("SpawnPoint");
                if (spawnPointObj != null)
                {
                    _playerSpawnPoint = spawnPointObj.transform;
                    Debug.Log($"StageCreater: SpawnPoint を自動検索で発見 Position={_playerSpawnPoint.position}");
                }
            }

            if (_playerSpawnPoint != null)
            {
                targetPosition = _playerSpawnPoint.position;
                targetRotation = _playerSpawnPoint.rotation;
                Debug.Log($"StageCreater: SpawnPoint を使用 Position={targetPosition}, Rotation={targetRotation}");
            }
            else
            {
                targetPosition = _playerJsonPosition;
                targetRotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);
                Debug.LogWarning($"StageCreater: SpawnPoint が見つからないため JSON 位置を使用 Position={targetPosition}");
            }

            // Playerの位置を設定
            player.transform.position = targetPosition;
            player.transform.rotation = targetRotation;
            Debug.Log($"StageCreater: Transform で初期配置 Position={player.transform.position}");

            yield return null;

            // Rigidbodyの初期化
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = targetPosition;
                rb.rotation = targetRotation;
                rb.isKinematic = false;
                Debug.Log($"StageCreater: Rigidbody初期化完了 Position={rb.position}, IsKinematic={rb.isKinematic}");
            }
            else
            {
                Debug.LogWarning("StageCreater: Rigidbody が見つかりません");
            }

            // 子オブジェクトのRigidbodyもリセット
            Rigidbody[] childRigidbodies = player.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody childRb in childRigidbodies)
            {
                if (childRb != null && childRb != rb)
                {
                    childRb.velocity = Vector3.zero;
                    childRb.angularVelocity = Vector3.zero;
                }
            }

            yield return null;

            // 物理シミュレーションを再開
            Physics.simulationMode = _originalSimulationMode;
            Debug.Log($"StageCreater: 物理シミュレーションを再開 Mode={_originalSimulationMode}");

            yield return new WaitForFixedUpdate();

            // 位置確認・補正
            if (rb != null)
            {
                float distance = Vector3.Distance(rb.position, targetPosition);
                if (distance > 0.1f)
                {
                    Debug.LogWarning($"StageCreater: 位置がずれたため補正 distance={distance}");
                    rb.position = targetPosition;
                    rb.rotation = targetRotation;
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }

            // PlayerManagerを新規追加
            Debug.Log("StageCreater: 新しいPlayerManagerを追加します");
            PlayerManager playerManager = player.AddComponent<PlayerManager>();
            playerManager.enabled = true;

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            // 最終確認
            Debug.Log($"=== Player 初期化完了 ===");
            Debug.Log($"  Position: {player.transform.position}");
            Debug.Log($"  Rotation: {player.transform.rotation.eulerAngles}");
            Debug.Log($"  SpawnPoint Position: {targetPosition}");

            if (rb != null)
            {
                Debug.Log($"  Rigidbody Velocity: {rb.velocity}");
                Debug.Log($"  Rigidbody IsKinematic: {rb.isKinematic}");
                Debug.Log($"  Rigidbody UseGravity: {rb.useGravity}");
            }

            Debug.Log("=== Player のコンポーネント一覧 ===");
            MonoBehaviour[] allComponents = player.GetComponents<MonoBehaviour>();
            foreach (var comp in allComponents)
            {
                if (comp != null)
                    Debug.Log($"  - {comp.GetType().Name}: enabled={comp.enabled}");
            }
        }

        #endregion

        #region ===== オブジェクト生成処理 =====

        private GameObject ObjectCreater(int color, int power)
        {
            if (color == (int)ObjectType.NotObject) return null;
            if (color <= -13 || color > (int)ObjectType.SMoving) return null;

            switch (color)
            {
                case (int)ObjectType.NFixed:
                    GameObject n_fixed = Instantiate(Objects[color - 1]);
                    PowerSet(n_fixed, power);
                    return n_fixed;

                case (int)ObjectType.SFixed:
                    GameObject s_fixed = Instantiate(Objects[color - 1]);
                    PowerSet(s_fixed, power);
                    return s_fixed;

                case (int)S_ObjectType.Player:
                    if (CanPlayerCreate())
                    {
                        GameObject player;

                        if (_playerPrefab != null)
                        {
                            player = Instantiate(_playerPrefab);
                            Debug.Log($"StageCreater: _playerPrefab から Player を生成 ({_playerPrefab.name})");
                        }
                        else
                        {
                            int player_value = (int)S_ObjectType.Player * (int)GameConstants.INVERSION;
                            player = Instantiate(_specialObjects[player_value]);
                            Debug.LogWarning("StageCreater: _playerPrefab が未設定のため _specialObjects から Player を生成");
                        }

                        player.tag = "Player";
                        PowerSet(player, power);
                        return player;
                    }
                    return null;

                case (int)S_ObjectType.Goal:
                    int goal_value = (int)S_ObjectType.Goal * (int)GameConstants.INVERSION;
                    GameObject goal = Instantiate(_specialObjects[goal_value]);
                    return goal;

                case (int)S_ObjectType.Gimmick:
                    int gimmick_value = (int)S_ObjectType.Gimmick * (int)GameConstants.INVERSION;
                    GameObject gimmick = Instantiate(_specialObjects[gimmick_value]);
                    return gimmick;

                case (int)S_ObjectType.P_Gimmick:
                    int p_gimmick_value = (int)S_ObjectType.P_Gimmick * (int)GameConstants.INVERSION;
                    GameObject p_gimmick = Instantiate(_specialObjects[p_gimmick_value]);
                    return p_gimmick;

                case (int)S_ObjectType.Moving_Floor:
                    int moving_floor_value = (int)S_ObjectType.Moving_Floor * (int)GameConstants.INVERSION;
                    GameObject moving_floor = Instantiate(_specialObjects[moving_floor_value]);
                    return moving_floor;

                case (int)S_ObjectType.CanUp:
                    int canup_value = (int)S_ObjectType.CanUp * (int)GameConstants.INVERSION;
                    GameObject canup = Instantiate(_specialObjects[canup_value]);
                    Debug.Log($"★★★ StageCreater: Button(CanUp)を生成 color={color}, canup_value={canup_value}");
                    return canup;

                default:
                    GameObject obj = Instantiate(Objects[color - 1]);
                    return obj;
            }
        }

        private void PowerSet(GameObject obj, int power)
        {
            MagnetObjectManager magnet = obj.GetComponent<MagnetObjectManager>();
            if (magnet == null) return;
            magnet.SetObjectPower(power);
        }

        private bool CanPlayerCreate()
        {
            if (isPlayerCreate)
            {
                isPlayerCreate = false;
                return true;
            }
            return false;
        }

        #endregion

        #region ===== ブロックのグループ化（未使用） =====

        private void GroupingBlocks(ref Transform[,] transforms, Transform main_object)
        {
            for (int i = 0; i < MAX_ROWS; i++)
            {
                for (int j = 0; j < MAX_COLS; j++)
                {
                    if (transforms[i, j] != null)
                    {
                        GameObject parent_object = Instantiate(_specialObjects[(int)S_ObjectType.Main], main_object);
                        transforms[i, j].SetParent(parent_object.transform, false);
                        transforms[i, j] = null;

                        int num = AddRightBlockToGroup(ref transforms, parent_object.transform, i, j + 1) - j + 1;
                        AddUpBlocksToGroup(ref transforms, parent_object.transform, i + 1, j, num);
                    }
                }
            }
        }

        private int AddRightBlockToGroup(ref Transform[,] transforms, Transform parent_transform, int row, int col)
        {
            if (col >= MAX_COLS || transforms[row, col] == null) return col - 1;
            if (colorArray[row, col] == colorArray[row, col - 1])
            {
                transforms[row, col].SetParent(parent_transform, false);
                transforms[row, col] = null;
                if (col + 1 < MAX_COLS) return AddRightBlockToGroup(ref transforms, parent_transform, row, col + 1);
                return col;
            }
            return col - 1;
        }

        private void AddUpBlocksToGroup(ref Transform[,] transforms, Transform parent_transform, int row, int col, int num)
        {
            if (row >= MAX_ROWS) return;

            for (int i = 0; i < num; i++)
            {
                if (colorArray[row, col + i] != colorArray[row - 1, col + i]) return;
            }
            for (int i = 0; i < num; i++)
            {
                if (transforms[row, col + i] == null) continue;
                transforms[row, col + i].SetParent(parent_transform, false);
                transforms[row, col + i] = null;
            }

            AddUpBlocksToGroup(ref transforms, parent_transform, row + 1, col, num);
        }

        #endregion
    }
}