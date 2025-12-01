using UnityEngine;
using UnityEngine.SceneManagement;
using Game.GameSystem;

public class StageInitializer : MonoBehaviour
{
    [Header("手動設定ステージインデックス")]
    [SerializeField] private int manualStageIndex = -1;

    private void Awake()
    {
        int stageIndex = manualStageIndex;

        // -1の場合はシーン名から自動取得
        if (stageIndex < 0)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            string number = System.Text.RegularExpressions.Regex.Match(sceneName, @"\d+").Value;

            if (!string.IsNullOrEmpty(number))
            {
                stageIndex = int.Parse(number) - 1; // Stage1 = index 0, Stage2 = index 1
                Debug.Log($"StageInitializer: シーン名 '{sceneName}' から StageIndex={stageIndex} を自動取得");
            }
            else
            {
                stageIndex = 0;
            }
        }

        // GameDataManager にステージインデックスを設定（これだけ）
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SetCurrentStageIndex(stageIndex);
            Debug.Log($"StageInitializer: StageIndex={stageIndex} を設定完了");
        }
    }
}