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
            CanUp = -3,      // ボタン
            P_Gimmick = -4,  // ギミック対象ブロック
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
        [SerializeField] private float _buttonYOffset = 0.5f;

        [Header("Crystal (Goal)")]
        [SerializeField] private Vector3 _crystalOffset = Vector3.zero;

        [Header("Stage Position")]
        [SerializeField] private Vector3 _stageOffset = Vector3.zero;

        [Header("Gimmick")]
        [SerializeField] private GameObject _gimmickPrefab; // Gimmickプレハブ

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
        private int[,] pointArray = new int[MAX_ROWS, MAX_COLS];

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

        private SimulationMode _originalSimulationMode;

        // ギミック管理用（複数ブロック対応）
        private Dictionary<int, List<ButtonController>> _gimmickButtons =
            new Dictionary<int, List<ButtonController>>();

        private Dictionary<int, List<GameObject>> _gimmickTargetBlocks =
            new Dictionary<int, List<GameObject>>();

        #endregion

        #region ===== JSON用 =====

        [Serializable]
        public class Item
        {
            public int color;
            public int power;
            public int point;
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
            if (_hasCreated || string.IsNullOrEmpty(json))
                return;

            _jsonData = json;
            StageCreate();
            StartCoroutine(InitializePlayerAfterCreation());
            _hasCreated = true;
        }

        #endregion

        #region ===== 背景生成 =====

        private void BGCreate()
        {
            int stageIndex = gameDataManager.GetCurrentStageIndex();

            GameObject camObj = GameObject.Find(GameConstants.MAIN_CAMERA);
            if (camObj == null)
                return;

            if (_bgObjects == null || stageIndex < 0 || stageIndex >= _bgObjects.Length)
                return;

            Transform cam = camObj.transform;

            Vector3 bgPos = new Vector3(
                cam.position.x,
                cam.position.y + 1.0f,
                1.0f
            );

            Instantiate(_bgObjects[stageIndex], bgPos, Quaternion.identity, cam);
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

            _gimmickButtons.Clear();
            _gimmickTargetBlocks.Clear();

            for (int i = 0; i < MAX_ROWS; i++)
            {
                for (int j = 0; j < MAX_COLS; j++)
                {
                    colorArray[i, j] = (int)ObjectType.NotObject;
                    powerArray[i, j] = 0;
                    pointArray[i, j] = 0;
                    scaleArray[i, j] = new Scale(0, 0);
                }
            }
        }

        private void GetStageDataFromJson()
        {
            RootObject root = JsonConvert.DeserializeObject<RootObject>(_jsonData);
            if (root == null || root.items == null)
                return;

            foreach (var item in root.items)
            {
                if (_row < 0)
                    break;

                colorArray[_row, _col] = item.value.color;
                powerArray[_row, _col] = item.value.power;
                pointArray[_row, _col] = item.value.point;
                scaleArray[_row, _col] = new Scale(1, 1);

                _col++;
                if (_col >= MAX_COLS)
                {
                    _col = 0;
                    _row--;
                }
            }
        }

        public void StageCreate()
        {
            _originalSimulationMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;

            ResetInternalState();
            GetStageDataFromJson();

            GameObject main = Instantiate(_specialObjects[(int)S_ObjectType.Main]);
            main.tag = "MainStage";
            main.transform.position = _stageOffset;

            isPlayerCreate = true;

            // オブジェクト生成
            for (int i = 0; i < MAX_ROWS; i++)
            {
                for (int j = 0; j < MAX_COLS; j++)
                {
                    if (scaleArray[i, j].row <= 0)
                        continue;

                    int color = colorArray[i, j];
                    int power = powerArray[i, j];
                    int point = pointArray[i, j];

                    GameObject obj = ObjectCreater(color, power);
                    if (obj == null)
                        continue;

                    if (color == (int)S_ObjectType.Player)
                    {
                        _createdPlayer = obj;
                        _playerWasCreatedFromJson = true;
                        _playerJsonPosition = obj.transform.position;
                        continue;
                    }

                    float x = INIT_X * j;
                    float y = INIT_Y * i;

                    // ボタンの場合、ギミックIDと一緒に保存
                    if (color == (int)S_ObjectType.CanUp)
                    {
                        y += _buttonYOffset;

                        if (point > 0)
                        {
                            ButtonController btn = obj.GetComponent<ButtonController>();
                            if (btn != null)
                            {
                                btn.gimmickId = $"gimmick_{point}";

                                if (!_gimmickButtons.ContainsKey(point))
                                    _gimmickButtons[point] = new List<ButtonController>();

                                _gimmickButtons[point].Add(btn);

                                Debug.Log(
                                    $"[StageCreater] ボタン登録: GimmickID={point}, " +
                                    $"GameObject={obj.name}, 位置=({i},{j})"
                                );
                            }
                        }
                    }

                    // ギミック対象ブロックの場合（複数対応）
                    if (color == (int)S_ObjectType.P_Gimmick)
                    {
                        if (point > 0)
                        {
                            if (!_gimmickTargetBlocks.ContainsKey(point))
                                _gimmickTargetBlocks[point] = new List<GameObject>();

                            _gimmickTargetBlocks[point].Add(obj);

                            Debug.Log(
                                $"[StageCreater] ターゲットブロック登録: GimmickID={point}, " +
                                $"GameObject={obj.name}, 位置=({i},{j}), " +
                                $"合計={_gimmickTargetBlocks[point].Count}個"
                            );
                        }
                    }

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

            // ギミック生成
            CreateGimmicks(main.transform);
            BGCreate();

            // Physics復帰
            Physics.simulationMode = _originalSimulationMode;
        }

        #endregion

        #region ===== ギミック生成処理 =====

        private void CreateGimmicks(Transform parent)
        {
            Debug.Log("[StageCreater] ========== ギミック生成開始 ==========");
            Debug.Log($"[StageCreater] 登録されているギミックID数: {_gimmickButtons.Count}");

            foreach (var gimmickId in _gimmickButtons.Keys)
            {
                if (!_gimmickTargetBlocks.ContainsKey(gimmickId))
                {
                    Debug.LogWarning(
                        $"[StageCreater] ギミックID {gimmickId} に対応するターゲットブロックがありません"
                    );
                    continue;
                }

                List<ButtonController> buttons = _gimmickButtons[gimmickId];
                List<GameObject> targetBlocks = _gimmickTargetBlocks[gimmickId];

                Debug.Log("[StageCreater] ----------------------------------------");
                Debug.Log($"[StageCreater] ギミックID {gimmickId}:");
                Debug.Log($"[StageCreater] - ボタン数: {buttons.Count}");
                Debug.Log($"[StageCreater] - ターゲットブロック数: {targetBlocks.Count}");

                // ★ギミックIDに応じて異なるコントローラーを生成
                if (gimmickId == 1)
                {
                    // ギミックID=1 → ReverseGimmick（押すと表示）
                    Debug.Log("[StageCreater] - ReverseGimmickController を使用");

                    GameObject gimmickObj = new GameObject($"ReverseGimmick_{gimmickId}");
                    gimmickObj.transform.SetParent(parent, false);

                    ReverseGimmickController controller = gimmickObj.AddComponent<ReverseGimmickController>();

                    if (buttons.Count > 0)
                    {
                        ButtonController button = buttons[0];
                        controller.SetButton(button);

                        // ★重要: ReverseGimmickの場合、各ブロックを最初から非表示に設定
                        foreach (GameObject block in targetBlocks)
                        {
                            block.SetActive(false); // 最初は非表示
                            controller.SetFixedBox(block);
                            Debug.Log($"[StageCreater] - ReverseGimmick ターゲット設定（初期非表示）: {block.name}");
                        }

                        Debug.Log($"[StageCreater] ✓ ReverseGimmick設定完了: ボタン={button.gameObject.name}");
                    }
                    else
                    {
                        Debug.LogError($"[StageCreater] ギミックID {gimmickId} にボタンがありません");
                    }
                }
                else if (gimmickId == 2)
                {
                    // ギミックID=2 → 通常Gimmick（押すと非表示）
                    Debug.Log("[StageCreater] - GimmickController を使用");

                    GameObject gimmickObj;

                    if (_gimmickPrefab != null)
                    {
                        gimmickObj = Instantiate(_gimmickPrefab, parent);
                        gimmickObj.name = $"Gimmick_{gimmickId}";
                    }
                    else
                    {
                        gimmickObj = new GameObject($"Gimmick_{gimmickId}");
                        gimmickObj.transform.SetParent(parent, false);
                        gimmickObj.AddComponent<GimmickController>();
                    }

                    GimmickController controller = gimmickObj.GetComponent<GimmickController>();

                    if (controller != null && buttons.Count > 0)
                    {
                        ButtonController button = buttons[0];
                        string gimmickIdString = $"gimmick_{gimmickId}";

                        controller.SetGimmickId(gimmickIdString);
                        controller.SetButton(button);

                        foreach (GameObject block in targetBlocks)
                        {
                            controller.AddFixedBox(block);
                            Debug.Log($"[StageCreater] - GimmickController ターゲット追加: {block.name}");
                        }

                        Debug.Log($"[StageCreater] ✓ GimmickController設定完了: ボタン={button.gameObject.name}");
                    }
                    else
                    {
                        Debug.LogError($"[StageCreater] GimmickController取得失敗 or ボタンなし");
                    }
                }
                else if (buttons.Count > 1)
                {
                    // ボタンが複数 → MultiButtonGimmick
                    Debug.Log("[StageCreater] - MultiButtonGimmickController を使用");

                    GameObject gimmickObj = new GameObject($"MultiButtonGimmick_{gimmickId}");
                    gimmickObj.transform.SetParent(parent, false);

                    MultiButtonGimmickController controller =
                        gimmickObj.AddComponent<MultiButtonGimmickController>();

                    controller.SetGimmickId($"gimmick_{gimmickId}");
                    controller.SetButtons(buttons);
                    controller.SetRequiredButtonCount(buttons.Count);

                    foreach (GameObject block in targetBlocks)
                    {
                        controller.AddTargetBlock(block);
                        Debug.Log($"[StageCreater] - MultiButtonGimmick ターゲット追加: {block.name}");
                    }

                    Debug.Log($"[StageCreater] ✓ MultiButtonGimmickController設定完了");
                }
            }

            Debug.Log("[StageCreater] ========== ギミック生成完了 ==========");
        }

        #endregion

        #region ===== Player初期化 =====

        private IEnumerator InitializePlayerAfterCreation()
        {
            if (!_playerWasCreatedFromJson || _createdPlayer == null)
            {
                Physics.simulationMode = _originalSimulationMode;
                yield break;
            }

            yield return null;

            Vector3 pos = _playerSpawnPoint
                ? _playerSpawnPoint.position
                : _playerJsonPosition;

            _createdPlayer.transform.position = pos;

            Rigidbody rb = _createdPlayer.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Physics.simulationMode = _originalSimulationMode;

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
                    Debug.LogWarning(
                        $"[StageCreater] 未定義の特殊オブジェクト: color={color}"
                    );
                    return -1;
            }
        }

        private GameObject ObjectCreater(int color, int power)
        {
            if (color == (int)ObjectType.NotObject)
                return null;

            if (color == (int)S_ObjectType.Player)
            {
                if (!CanPlayerCreate())
                    return null;

                GameObject player = Instantiate(_playerPrefab);
                player.tag = "Player";
                return player;
            }

            if (color < 0)
            {
                int index = GetSpecialObjectIndex(color);
                if (index < 0)
                {
                    Debug.LogError(
                        $"[StageCreater] エラー: color={color} に対応する特殊オブジェクトが見つかりません"
                    );
                    return null;
                }

                if (index >= _specialObjects.Length)
                {
                    Debug.LogError(
                        $"[StageCreater] エラー: index={index} は配列サイズ {_specialObjects.Length} を超えています!"
                    );
                    return null;
                }

                if (_specialObjects[index] == null)
                {
                    Debug.LogError(
                        $"[StageCreater] エラー: _specialObjects[{index}] (color={color}) が null です!"
                    );
                    return null;
                }

                return Instantiate(_specialObjects[index]);
            }

            if (color < 1 || color > Objects.Length)
            {
                Debug.LogError(
                    $"[StageCreater] エラー: 通常ブロックcolor={color} は範囲外です（1～{Objects.Length}）"
                );
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