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
    /// JSON データからステージを生成するクラス
    /// </summary>
    public class StageCreater : MonoBehaviour
    {
        #region ===== 列挙型 =====

        // 通常ブロックの種類
        private enum ObjectType
        {
            NotObject,
            NotFixed,
            NFixed,
            SFixed,
            CanFixed,
            NotMoving_1,
            NotMoving_2,
            NotMoving_3,
            CanMoving,
            NMoving,
            SMoving,
        }

        // 特殊オブジェクト（負の値）
        // Inspector の Special Objects 配列に対応
        // [0]=MainStage, [1]=MagForce_Prefab, [2]=Crystal_Model_Prefab, [3]=Button, [4]=Gimmick
        private enum S_ObjectType
        {
            Main = 0,           // Element 0: MainStage
            Player = -1,        // Playerは別管理（_playerPrefab使用）
            Goal = -2,          // Element 2: Crystal_Model_Prefab
            CanUp = -3,         // Element 3: Button
            P_Gimmick = -4,     // Element 4: Gimmick
            None = -5,
        }

        #endregion

        #region ===== Inspector =====

        // ゲーム全体データ
        private GameDataManager gameDataManager = GameDataManager.Instance;

        // 通常ブロック
        [SerializeField] private GameObject[] Objects;
        // 特殊オブジェクト
        [SerializeField] private GameObject[] _specialObjects;
        // 背景
        [SerializeField] private GameObject[] _bgObjects;

        [Header("Player")]
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private Transform _playerSpawnPoint;

        [Header("Button")]
        [SerializeField] private float _buttonYOffset = 0.5f;

        [Header("Crystal (Goal)")]
        [SerializeField] private Vector3 _crystalOffset = Vector3.zero;

        [Header("Stage Position")]
        [SerializeField] private Vector3 _stageOffset = Vector3.zero;

        #endregion

        #region ===== 定数 =====

        private const int MAX_ROWS = 25;
        private const int MAX_COLS = 38;
        private const float INIT_X = 1.0f;
        private const float INIT_Y = 1.0f;
        private const float INIT_Z = 0.0f;

        #endregion

        #region ===== 内部変数 =====

        // JSON読み込み用インデックス
        private int _row;
        private int _col;

        // 色（オブジェクト種別）
        private int[,] colorArray = new int[MAX_ROWS, MAX_COLS];
        // 磁力パワー
        private int[,] powerArray = new int[MAX_ROWS, MAX_COLS];

        // スケール情報（現状は使用数管理）
        private struct Scale
        {
            public int row;
            public int col;
            public Scale(int r, int c)
            {
                row = r;
                col = c;
            }
        }

        private Scale[,] scaleArray = new Scale[MAX_ROWS, MAX_COLS];

        // Player生成管理
        private bool isPlayerCreate;
        private bool _hasCreated;

        // JSONデータ
        private string _jsonData;

        // JSONから生成されたPlayer用
        private Vector3 _playerJsonPosition;
        private bool _playerWasCreatedFromJson;
        private GameObject _createdPlayer;

        // Physics状態保存用
        private SimulationMode _originalSimulationMode;

        #endregion

        #region ===== JSON用 =====

        [Serializable]
        public class Item
        {
            public int color;
            public int power;
        }

        [Serializable]
        public class ItemWrapper
        {
            public string key;
            public Item value;
        }

        [Serializable]
        public class RootObject
        {
            [JsonProperty("items")]
            public List<ItemWrapper> items;
        }

        #endregion

        #region ===== Unity =====

        private void Awake()
        {
            // 前ステージの残骸を削除
            CleanupPreviousStageObjects();
            _hasCreated = false;
        }

        #endregion

        #region ===== Cleanup =====

        // 既存ステージ・Playerを削除
        private void CleanupPreviousStageObjects()
        {
            foreach (var obj in GameObject.FindGameObjectsWithTag("MainStage"))
                Destroy(obj);

            foreach (var obj in GameObject.FindGameObjectsWithTag("Player"))
                Destroy(obj);
        }

        #endregion

        #region ===== 外部呼び出し =====

        // JSONを受け取ってステージ生成
        public void SetJsonAndCreate(string json)
        {
            if (_hasCreated || string.IsNullOrEmpty(json)) return;

            _jsonData = json;

            // ステージ生成
            StageCreate();
            // Player初期化待ち
            StartCoroutine(InitializePlayerAfterCreation());

            _hasCreated = true;
        }

        #endregion

        #region ===== 背景生成 =====

        /// <summary>
        /// ステージ番号に応じた背景を生成（カメラ追従）
        /// </summary>
        private void BGCreate()
        {
            int stageIndex = gameDataManager.GetCurrentStageIndex();

            // メインカメラ取得
            GameObject camObj = GameObject.Find(GameConstants.MAIN_CAMERA);
            if (camObj == null) return;

            // 範囲チェック
            if (_bgObjects == null || stageIndex < 0 || stageIndex >= _bgObjects.Length)
                return;

            Transform cam = camObj.transform;

            // カメラ位置基準で配置
            Vector3 bgPos = new Vector3(
                cam.position.x,
                cam.position.y + 1.0f,
                1.0f
            );

            Instantiate(
                _bgObjects[stageIndex],
                bgPos,
                Quaternion.identity,
                cam
            );
        }

        #endregion

        #region ===== ステージ生成 =====

        // 内部状態初期化
        private void ResetInternalState()
        {
            _row = MAX_ROWS - 1;
            _col = 0;

            isPlayerCreate = false;
            _playerWasCreatedFromJson = false;
            _createdPlayer = null;

            // 配列リセット
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

        // JSONからステージデータ取得
        private void GetStageDataFromJson()
        {
            RootObject root = JsonConvert.DeserializeObject<RootObject>(_jsonData);
            if (root == null || root.items == null) return;
            foreach (var item in root.items)
            {
                if (_row < 0) break;

                colorArray[_row, _col] = item.value.color;
                powerArray[_row, _col] = item.value.power;
                scaleArray[_row, _col] = new Scale(1, 1);

                _col++;
                if (_col >= MAX_COLS)
                {
                    _col = 0;
                    _row--;
                }
            }
        }

        // ステージ生成本体
        public void StageCreate()
        {
            // Physics停止
            _originalSimulationMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;

            ResetInternalState();
            GetStageDataFromJson();

            // メイン親オブジェクト（Element 0: MainStage）
            GameObject main = Instantiate(_specialObjects[(int)S_ObjectType.Main]);
            main.tag = "MainStage";

            // ステージ全体の位置を設定
            main.transform.position = _stageOffset;

            isPlayerCreate = true;

            // オブジェクト生成
            for (int i = 0; i < MAX_ROWS; i++)
            {
                for (int j = 0; j < MAX_COLS; j++)
                {
                    if (scaleArray[i, j].row <= 0) continue;

                    int color = colorArray[i, j];
                    int power = powerArray[i, j];

                    GameObject obj = ObjectCreater(color, power);
                    if (obj == null) continue;

                    // Playerは後処理
                    if (color == (int)S_ObjectType.Player)
                    {
                        _createdPlayer = obj;
                        _playerWasCreatedFromJson = true;
                        _playerJsonPosition = obj.transform.position;
                        continue;
                    }

                    float x = INIT_X * j;
                    float y = INIT_Y * i;

                    // ボタン位置補正
                    if (color == (int)S_ObjectType.CanUp)
                    {
                        y += _buttonYOffset;
                    }

                    // クリスタル（Goal）位置補正
                    if (color == (int)S_ObjectType.Goal)
                    {
                        obj.transform.position = new Vector3(
                            x + _crystalOffset.x,
                            y + _crystalOffset.y,
                            INIT_Z + _crystalOffset.z
                        );
                    }
                    else
                    {
                        obj.transform.position = new Vector3(x, y, INIT_Z);
                    }

                    obj.transform.SetParent(main.transform, false);
                }
            }

            // 背景生成
            BGCreate();
        }

        #endregion

        #region ===== Player初期化 =====

        // Player生成後の初期化処理
        private IEnumerator InitializePlayerAfterCreation()
        {
            if (!_playerWasCreatedFromJson || _createdPlayer == null)
            {
                Physics.simulationMode = _originalSimulationMode;
                yield break;
            }

            // 1フレーム待機
            yield return null;

            // スポーン位置決定
            Vector3 pos = _playerSpawnPoint
                ? _playerSpawnPoint.position
                : _playerJsonPosition;

            _createdPlayer.transform.position = pos;

            // Rigidbodyリセット
            Rigidbody rb = _createdPlayer.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Physics復帰
            Physics.simulationMode = _originalSimulationMode;

            // Player管理追加
            _createdPlayer.AddComponent<PlayerManager>();
        }

        #endregion

        #region ===== オブジェクト生成 =====

        /// <summary>
        /// color値から配列インデックスへのマッピング
        /// </summary>
        private int GetSpecialObjectIndex(int color)
        {
            // Inspector配列: [0]=MainStage, [1]=MagForce, [2]=Crystal, [3]=Button, [4]=Gimmick
            switch (color)
            {
                case (int)S_ObjectType.Player:    // -1 → Playerは_playerPrefab使用
                    return -1;
                case (int)S_ObjectType.Goal:      // -2 → Element 2: Crystal_Model_Prefab
                    return 2;
                case (int)S_ObjectType.CanUp:     // -3 → Element 3: Button
                    return 3;
                case (int)S_ObjectType.P_Gimmick: // -4 → Element 4: Gimmick
                    return 4;
                default:
                    Debug.LogWarning($"[StageCreater] 未定義の特殊オブジェクト: color={color}");
                    return -1;
            }
        }

        // オブジェクト生成処理
        private GameObject ObjectCreater(int color, int power)
        {
            if (color == (int)ObjectType.NotObject) return null;

            // Player生成（_playerPrefab使用）
            if (color == (int)S_ObjectType.Player)
            {
                if (!CanPlayerCreate()) return null;
                GameObject player = Instantiate(_playerPrefab);
                player.tag = "Player";
                return player;
            }

            // 特殊オブジェクト
            if (color < 0)
            {
                int index = GetSpecialObjectIndex(color);

                if (index < 0)
                {
                    Debug.LogError($"[StageCreater] エラー: color={color} に対応する特殊オブジェクトが見つかりません");
                    return null;
                }

                // 配列範囲チェック
                if (index >= _specialObjects.Length)
                {
                    Debug.LogError($"[StageCreater] エラー: index={index} は配列サイズ {_specialObjects.Length} を超えています!");
                    Debug.LogError($"[StageCreater] color={color} に対応するプレハブをInspectorに設定してください");
                    return null;
                }

                // null チェック
                if (_specialObjects[index] == null)
                {
                    Debug.LogError($"[StageCreater] エラー: _specialObjects[{index}] (color={color}) が null です!");
                    return null;
                }

                return Instantiate(_specialObjects[index]);
            }

            // 通常ブロック
            if (color < 1 || color > Objects.Length)
            {
                Debug.LogError($"[StageCreater] エラー: 通常ブロックcolor={color} は範囲外です（1～{Objects.Length}）");
                return null;
            }

            GameObject obj = Instantiate(Objects[color - 1]);
            PowerSet(obj, power);
            return obj;
        }

        // 磁力設定
        private void PowerSet(GameObject obj, int power)
        {
            MagnetObjectManager magnet = obj.GetComponent<MagnetObjectManager>();
            if (magnet != null)
                magnet.SetObjectPower(power);
        }

        // Playerは1体のみ生成
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
    }
}