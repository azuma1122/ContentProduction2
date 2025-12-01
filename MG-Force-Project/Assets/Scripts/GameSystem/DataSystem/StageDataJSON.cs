using UnityEngine;

/// <summary>
/// ステージ情報を保存する JSON 用データ構造
/// </summary>
[System.Serializable]
public class StageDataJSON
{
    public int stageIndex;          // ステージ番号
    public string stageObjectName;  // ステージのメインオブジェクト名
    public string stageBGName;      // 背景名
    public Vector2 topRight;        // ステージ右上座標（ステージサイズ）

    /// <summary>
    /// コンストラクタ（手動作成時用）
    /// </summary>
    public StageDataJSON(int index, string objName, string bgName, Vector2 size)
    {
        stageIndex = index;
        stageObjectName = objName;
        stageBGName = bgName;
        topRight = size;
    }
}
