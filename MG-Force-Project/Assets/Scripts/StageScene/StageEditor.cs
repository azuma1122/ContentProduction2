using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace Game.StageScene
{
    /// <summary>
    /// ステージエディタ
    /// - Hierarchy 上のブロック配置を JSON として保存
    /// - ブロックの「名前・位置」だけ出力し、StageCreater と互換性のある形式にする
    /// </summary>
    public class StageEditor : MonoBehaviour
    {
        [Header("ブロックの親オブジェクト")]
        [SerializeField] private Transform parentOfBlocks;

        [Header("保存するJSONファイル名 (例: Stage_1.json)")]
        [SerializeField] private string fileName = "Stage_1.json";

        private string filePath;

        private void Awake()
        {
            // StreamingAssets に保存するよう統一
            filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        }

#if UNITY_EDITOR
        /// <summary>
        /// ステージデータを JSON 形式で保存
        /// </summary>
        [ContextMenu("Save Stage JSON")]
        public void SaveStage()
        {
            if (parentOfBlocks == null)
            {
                Debug.LogWarning("Parent Of Blocks（ブロックの親）が設定されていません");
                return;
            }

            List<BlockData> blocks = new List<BlockData>();

            foreach (Transform block in parentOfBlocks)
            {
                blocks.Add(new BlockData
                {
                    prefabName = block.name,       // プレハブ名
                    x = block.position.x,
                    y = block.position.y
                });
            }

            // StageJsonData にまとめる
            StageJsonData jsonData = new StageJsonData
            {
                Blocks = blocks.ToArray(),
                PlayerSpawn = Vector2.zero, // エディタでは未設定（必要なら配置可能）
                Goal = Vector2.zero
            };

            string json = JsonUtility.ToJson(jsonData, true);

            Directory.CreateDirectory(Application.streamingAssetsPath);
            File.WriteAllText(filePath, json);

            Debug.Log($"ステージデータを保存しました：{filePath}");
        }
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// StageEditor 用のカスタムInspector
    /// </summary>
    [CustomEditor(typeof(StageEditor))]
    public class StageEditorInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);
            GUI.backgroundColor = Color.cyan;

            if (GUILayout.Button("ステージデータを保存"))
            {
                StageEditor editor = (StageEditor)target;
                editor.SaveStage();
            }
        }
    }
#endif
}
