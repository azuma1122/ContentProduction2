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
        private enum S_ObjectType
        {
            Main = 0,
            Player = -1,
            Goal = -2,
            CanUp = -3,
            P_Gimmick = -4,
            None = -5,
        }

        #endregion

        #region ===== Inspector =====

        private GameDataManager gameDataManager = GameDataManager.Instance;

        [SerializeField] private GameObject[] Objects;
        [SerializeField] private GameObject[] _specialObjects;
        [SerializeField] private GameObject[] _bgObjects;

        [Header("Player")]
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private Transform _playerSpawnPoint;

        [Header("Button")]
        [SerializeField] private float _buttonYOffset = 0.3f;

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

        private int _row;
        private int _col;

        private int[,] colorArray = new int[MAX_ROWS, MAX_COLS];
        private int[,] powerArray = new int[MAX_ROWS, MAX_COLS];

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

        private bool isPlayerCreate;
        private bool _hasCreated;

        private string _jsonData;

        private Vector3 _playerJsonPosition;
        private bool _playerWasCreatedFromJson;
        private GameObject _createdPlayer;

        // 生成されたオブジェクトを保持
        private List<GameObject> _createdObjects = new List<GameObject>();

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
            CleanupPreviousStageObjects();
            _hasCreated = false;
        }

        #endregion

        #region ===== Cleanup =====

        private void CleanupPreviousStageObjects()
        {
            foreach (var obj in GameObject.FindGameObjectsWithTag("MainStage"))
                Destroy(obj);

            foreach (var obj in GameObject.FindGameObjectsWithTag("Player"))
                Destroy(obj);
        }

        #endregion

        #region ===== 外部呼び出し =====

        public void SetJsonAndCreate(string json)
        {
            if (_hasCreated || string.IsNullOrEmpty(json)) return;

            _jsonData = json;

            StartCoroutine(StageCreateCoroutine());

            _hasCreated = true;
        }

        #endregion

        #region ===== 背景生成 =====

        private void BGCreate()
        {
            int stageIndex = gameDataManager.GetCurrentStageIndex();

            GameObject camObj = GameObject.Find(GameConstants.MAIN_CAMERA);
            if (camObj == null) return;

            if (_bgObjects == null || stageIndex < 0 || stageIndex >= _bgObjects.Length)
                return;

            Transform cam = camObj.transform;

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

        private void ResetInternalState()
        {
            _row = MAX_ROWS - 1;
            _col = 0;

            isPlayerCreate = false;
            _playerWasCreatedFromJson = false;
            _createdPlayer = null;
            _createdObjects.Clear();

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

        /// <summary>
        /// コルーチンでステージ生成を行い、Physics停止を最小限にする
        /// </summary>
        private IEnumerator StageCreateCoroutine()
        {
            ResetInternalState();
            GetStageDataFromJson();

            // メイン親オブジェクト
            GameObject main = Instantiate(_specialObjects[(int)S_ObjectType.Main]);
            main.tag = "MainStage";
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

                    // 生成したオブジェクトをリストに保存
                    _createdObjects.Add(obj);
                }
            }

            // 背景生成
            BGCreate();

            // 1フレーム待機してから初期化
            yield return null;

            // 生成されたオブジェクトのコンポーネントを初期化
            InitializeCreatedObjects();

            // Player初期化
            yield return StartCoroutine(InitializePlayerAfterCreation());
        }

        /// <summary>
        /// 生成されたオブジェクトのコンポーネントを初期化
        /// </summary>
        private void InitializeCreatedObjects()
        {
            foreach (GameObject obj in _createdObjects)
            {
                if (obj == null) continue;

                // Rigidbodyの初期化
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;

                    // スリープ状態から復帰
                    if (rb.IsSleeping())
                    {
                        rb.WakeUp();
                    }
                }

                // ObstaclesObjectControllerの再初期化
                ObstaclesObjectController obstacleController = obj.GetComponent<ObstaclesObjectController>();
                if (obstacleController != null)
                {
                    // コンポーネントを有効化（Start()を再実行させる）
                    obstacleController.enabled = false;
                    obstacleController.enabled = true;
                }

                // MovingObjectControllerの再初期化
                MovingObjectController movingController = obj.GetComponent<MovingObjectController>();
                if (movingController != null)
                {
                    movingController.enabled = false;
                    movingController.enabled = true;
                }

                // MovingMagnetBlockの再初期化
                MovingMagnetBlock magnetBlock = obj.GetComponent<MovingMagnetBlock>();
                if (magnetBlock != null)
                {
                    magnetBlock.enabled = false;
                    magnetBlock.enabled = true;
                }
            }
        }

        #endregion

        #region ===== Player初期化 =====

        private IEnumerator InitializePlayerAfterCreation()
        {
            if (!_playerWasCreatedFromJson || _createdPlayer == null)
            {
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

            // Player管理追加
            _createdPlayer.AddComponent<PlayerManager>();
        }

        #endregion

        #region ===== オブジェクト生成 =====

        private int GetSpecialObjectIndex(int color)
        {
            switch (color)
            {
                case (int)S_ObjectType.Player:
                    return -1;
                case (int)S_ObjectType.Goal:
                    return 2;
                case (int)S_ObjectType.CanUp:
                    return 3;
                case (int)S_ObjectType.P_Gimmick:
                    return 4;
                default:
                    return -1;
            }
        }

        private GameObject ObjectCreater(int color, int power)
        {
            if (color == (int)ObjectType.NotObject) return null;

            // Player生成
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

                if (index < 0 || index >= _specialObjects.Length)
                {
                    return null;
                }

                if (_specialObjects[index] == null)
                {
                    return null;
                }

                return Instantiate(_specialObjects[index]);
            }

            // 通常ブロック
            if (color < 1 || color > Objects.Length)
            {
                return null;
            }

            GameObject obj = Instantiate(Objects[color - 1]);
            PowerSet(obj, power);
            return obj;
        }

        private void PowerSet(GameObject obj, int power)
        {
            MagnetObjectManager magnet = obj.GetComponent<MagnetObjectManager>();
            if (magnet != null)
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
    }
}