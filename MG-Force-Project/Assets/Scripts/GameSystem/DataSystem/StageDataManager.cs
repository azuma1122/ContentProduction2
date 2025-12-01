using UnityEngine;
using System.IO;

namespace Game.StageScene
{
    public class StageDataManager : MonoBehaviour
    {
        private string _filePath;

        [SerializeField] private int currentStageIndex = 0;

        private void Awake()
        {
            _filePath = Path.Combine(Application.persistentDataPath, "stageData.json");

            SetCurrentStageIndex(currentStageIndex);
        }

        public int GetCurrentStageIndex() => currentStageIndex;

        public void SetCurrentStageIndex(int index)
        {
            currentStageIndex = index;
            Debug.Log($"ステージインデックスを設定: {index}");
        }

        public void SaveStageData(StageDataJSON data)
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(_filePath, json);
            Debug.Log($"ステージデータを保存: {_filePath}");
        }

        public StageDataJSON LoadStageData()
        {
            if (!File.Exists(_filePath))
            {
                Debug.LogWarning("ステージデータファイルが存在しない");
                return null;
            }

            string json = File.ReadAllText(_filePath);
            StageDataJSON data = JsonUtility.FromJson<StageDataJSON>(json);
            Debug.Log($"ステージデータを読み込んだ: Index={data.stageIndex}");
            return data;
        }

        [ContextMenu("Test Save & Load")]
        private void TestSaveLoad()
        {
            // 既存の StageDataJSON を使う
            StageDataJSON sample = new StageDataJSON(1, "Stage01", "Background01", new Vector2(25, 10));
            SaveStageData(sample);

            var loaded = LoadStageData();
            if (loaded != null)
                Debug.Log($"読み込み結果 → {loaded.stageObjectName}, {loaded.stageBGName}, {loaded.topRight}");
        }
    }
}
