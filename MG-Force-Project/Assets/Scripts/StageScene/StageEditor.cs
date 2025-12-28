using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System;

namespace Game.StageScene
{
    /// <summary>
    /// ステージエディタ
    /// - Hierarchy 上のブロック配置を JSON として保存
    /// - StageCreater と互換性のある形式で出力
    /// </summary>
    public class StageEditor : MonoBehaviour
    {
        [Header("ブロックの親オブジェクト")]
        [SerializeField] private Transform parentOfBlocks;

        [Header("保存するJSONファイル名 (例: Stage_5.json)")]
        [SerializeField] private string fileName = "Stage_5.json";

        // プレハブ名とcolor値のマッピング
        private Dictionary<string, int> blockNameToColor = new Dictionary<string, int>
        {
            {"NotFixed", 1},
            {"NFixed", 2},
            {"SFixed", 3},
            {"CanFixed", 4},
            {"NotMoving_1", 5},
            {"NotMoving_2", 6},
            {"NotMoving_3", 7},
            {"CanMoving", 8},
            {"NMoving", 9},
            {"SMoving", 10},
            {"Player", -1},
            {"Goal", -2},
            {"Gimmick", -3},
            {"P_Gimmick", -4},
            {"Moving_Floor", -11},
            {"CanUp", -12}
        };

#if UNITY_EDITOR
        [ContextMenu("Save Stage JSON")]
        public void SaveStage()
        {
            if (parentOfBlocks == null)
            {
                Debug.LogWarning("Parent Of Blocks（ブロックの親）が設定されていません");
                return;
            }

            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogWarning("ファイル名が設定されていません");
                return;
            }

            // StreamingAssets のパスを構築
            string directory = Path.Combine(Application.streamingAssetsPath, "StageData");
            string filePath = Path.Combine(directory, fileName);

            // ディレクトリが存在しない場合は作成
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Debug.Log($"ディレクトリを作成: {directory}");
            }

            // ステージデータを収集（25行38列の配列として）
            const int MAX_ROWS = 25;
            const int MAX_COLS = 38;

            List<ItemWrapper> items = new List<ItemWrapper>();

            for (int row = MAX_ROWS - 1; row >= 0; row--)
            {
                for (int col = 0; col < MAX_COLS; col++)
                {
                    // この位置にブロックがあるか探す
                    GameObject blockAtPos = FindBlockAt(col, row);

                    int color = 0; // NotObject
                    int power = 0;

                    if (blockAtPos != null)
                    {
                        // ブロック名からcolor値を取得
                        string blockName = GetCleanBlockName(blockAtPos.name);

                        if (blockNameToColor.ContainsKey(blockName))
                        {
                            color = blockNameToColor[blockName];

                            // 磁力パワーを取得（通常ブロックの場合）
                            if (color > 0)
                            {
                                var magnet = blockAtPos.GetComponent<Game.StageScene.Magnet.MagnetObjectManager>();
                                if (magnet != null)
                                {
                                    // リフレクションでフィールド/プロパティから取得を試みる
                                    power = GetMagnetPower(magnet);
                                }
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"不明なブロック名: {blockName} (position: {col}, {row})");
                        }
                    }

                    string key = $"{row}-{col}";
                    items.Add(new ItemWrapper
                    {
                        key = key,
                        value = new Item
                        {
                            color = color,
                            power = power
                        }
                    });
                }
            }

            // JSON 形式に変換
            RootObject root = new RootObject { items = items };
            string json = JsonConvert.SerializeObject(root, Formatting.Indented);

            // ファイルに保存
            File.WriteAllText(filePath, json);
            Debug.Log($"ステージデータを保存しました：{filePath}");

            // Unityエディタのアセットデータベースを更新
            AssetDatabase.Refresh();
        }

        private GameObject FindBlockAt(int x, int y)
        {
            foreach (Transform child in parentOfBlocks)
            {
                Vector3 pos = child.position;
                if (Mathf.Approximately(pos.x, x) && Mathf.Approximately(pos.y, y))
                {
                    return child.gameObject;
                }
            }
            return null;
        }

        private string GetCleanBlockName(string name)
        {
            // "(Clone)" などを除去
            int parenIndex = name.IndexOf('(');
            if (parenIndex > 0)
            {
                return name.Substring(0, parenIndex).Trim();
            }
            return name.Trim();
        }

        private int GetMagnetPower(object magnetComponent)
        {
            if (magnetComponent == null) return 0;

            // リフレクションで power フィールドまたはプロパティを取得
            var type = magnetComponent.GetType();

            // フィールドを探す（private/public両方）
            var powerField = type.GetField("power", System.Reflection.BindingFlags.Instance |
                                                     System.Reflection.BindingFlags.Public |
                                                     System.Reflection.BindingFlags.NonPublic);
            if (powerField != null)
            {
                return (int)powerField.GetValue(magnetComponent);
            }

            // プロパティを探す
            var powerProperty = type.GetProperty("power", System.Reflection.BindingFlags.Instance |
                                                          System.Reflection.BindingFlags.Public |
                                                          System.Reflection.BindingFlags.NonPublic);
            if (powerProperty != null)
            {
                return (int)powerProperty.GetValue(magnetComponent);
            }

            // ObjectPower という名前の可能性も
            powerField = type.GetField("ObjectPower", System.Reflection.BindingFlags.Instance |
                                                       System.Reflection.BindingFlags.Public |
                                                       System.Reflection.BindingFlags.NonPublic);
            if (powerField != null)
            {
                return (int)powerField.GetValue(magnetComponent);
            }

            powerProperty = type.GetProperty("ObjectPower", System.Reflection.BindingFlags.Instance |
                                                            System.Reflection.BindingFlags.Public |
                                                            System.Reflection.BindingFlags.NonPublic);
            if (powerProperty != null)
            {
                return (int)powerProperty.GetValue(magnetComponent);
            }

            Debug.LogWarning($"MagnetObjectManager から power 値を取得できませんでした");
            return 0;
        }
#endif

        // StageCreater と同じJSON構造
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
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(StageEditor))]
    public class StageEditorInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);
            GUI.backgroundColor = Color.cyan;

            if (GUILayout.Button("ステージデータを保存", GUILayout.Height(30)))
            {
                StageEditor editor = (StageEditor)target;
                editor.SaveStage();
            }
        }
    }
#endif
}