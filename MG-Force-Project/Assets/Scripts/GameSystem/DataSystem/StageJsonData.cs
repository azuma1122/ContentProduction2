using UnityEngine;

namespace Game.StageScene
{
    /// <summary>
    /// JSON ファイルのデータ構造に対応するクラス
    /// </summary>
    [System.Serializable]
    public class StageJsonData
    {
        public BlockData[] Blocks;     // ブロック一覧
        public Vector2 PlayerSpawn;    // プレイヤーの初期位置
        public Vector2 Goal;           // ゴールの位置
    }

    /// <summary>
    /// 各ブロックの JSON 情報
    /// </summary>
    [System.Serializable]
    public class BlockData
    {
        public string prefabName; // Resources/StageBlocks/ からロードする名前
        public float x;           // ブロックの X 座標
        public float y;           // ブロックの Y 座標
    }
}
