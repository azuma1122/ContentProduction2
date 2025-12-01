using UnityEngine;
using System.Collections.Generic;

namespace Game.StageScene
{
    /// <summary>
    /// JSONステージデータからブロックを生成するためのデータベース
    /// </summary>
    public class BlockDatabase : MonoBehaviour
    {
        /// <summary>
        /// Unity上で登録する1つのブロック情報
        /// </summary>
        [System.Serializable]
        public class BlockData
        {
            [Header("JSON の \"name\" と対応する名前")]
            public string name; // JSON: entry.value.name に対応

            [Header("JSON の \"color\" と対応する番号")]
            public int color;   // JSON: entry.value.color に対応

            [Header("このブロック名/色で生成されるプレハブ")]
            public GameObject prefab; // 実際に生成したいブロックオブジェクト
        }

        [Header("登録するブロック一覧")]
        public List<BlockData> blockList = new List<BlockData>();

        // name → prefab 変換テーブル
        private Dictionary<string, GameObject> _blockDictByName;

        // color → prefab 変換テーブル
        private Dictionary<int, GameObject> _blockDictByColor;

        private void Awake()
        {
            // 起動時に辞書を初期化
            _blockDictByName = new Dictionary<string, GameObject>();
            _blockDictByColor = new Dictionary<int, GameObject>();

            // blockList（Unity Inspectorで登録）から辞書に登録
            foreach (var block in blockList)
            {
                // name が空でない場合のみ、name → prefab を登録
                if (!string.IsNullOrEmpty(block.name) && !_blockDictByName.ContainsKey(block.name))
                {
                    _blockDictByName.Add(block.name, block.prefab);
                }

                // color → prefab を登録
                if (!_blockDictByColor.ContainsKey(block.color))
                {
                    _blockDictByColor.Add(block.color, block.prefab);
                }
            }
        }

        /// <summary>
        /// JSON の "name" を使ってプレハブを取得する
        /// </summary>
        public GameObject GetPrefab(string blockName)
        {
            if (_blockDictByName != null && _blockDictByName.ContainsKey(blockName))
            {
                // 登録されていればそのまま返す
                return _blockDictByName[blockName];
            }

            // なければ警告
            Debug.LogWarning($"BlockDatabase に name='{blockName}' が登録されていません");
            return null;
        }

        /// <summary>
        /// JSON の "color" を使ってプレハブを取得する
        /// </summary>
        public GameObject GetPrefabByColor(int color)
        {
            if (_blockDictByColor != null && _blockDictByColor.ContainsKey(color))
            {
                // 登録されていれば返す
                return _blockDictByColor[color];
            }

            // なければ警告
            Debug.LogWarning($"BlockDatabase に color={color} が登録されていません");
            return null;
        }
    }
}
