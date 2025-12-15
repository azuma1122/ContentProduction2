using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

namespace Game.StageScene
{
    /// <summary>
    /// Scene 名から対応する JSON ステージデータを読み込み、
    /// StageCreater に渡してステージを生成するクラス（修正版）
    /// - シーン遷移時の初期化順序を改善
    /// - 重複削除処理を防止
    /// - シーンリロード時の再生成に対応
    /// </summary>
    public class StageLoader : MonoBehaviour
    {
        [Header("ステージを JSON から読み込むかどうか")]
        [SerializeField]
        private bool _useJsonStage = true;

        [Header("ステージ生成担当（StageCreater）")]
        [SerializeField]
        private StageCreater _stageCreater;

        // JSON 読み込み済みかどうか（多重生成防止用）
        // ★★★ 修正：static削除（シーンリロード時にリセットされるように） ★★★
        private bool _loaded = false;

        private void Awake()
        {
            Debug.Log("StageLoader: Awake - シーン初期化開始");

            // ★★★ 追加：Awake時に必ずリセット ★★★
            _loaded = false;
        }

        private void Start()
        {
            if (SceneManager.GetActiveScene().name == "StageSelect")
            {
                Debug.Log("StageLoader: StageSelect のため JSON 読み込みをスキップします");
                return;
            }

            if (!_useJsonStage)
            {
                Debug.Log("StageLoader: JSON を使わず手置きステージを使用します");
                return;
            }

            if (_loaded)
            {
                Debug.LogWarning("StageLoader: 既にロード済みのため処理をスキップします");
                return;
            }

            LoadStageJson();
        }

        private void LoadStageJson()
        {
            if (_stageCreater == null)
            {
                Debug.LogError("StageLoader: StageCreater が Inspector に設定されていません");
                return;
            }

            string sceneName = SceneManager.GetActiveScene().name;
            string jsonName = "";

            // Stage1 は固定
            if (sceneName == "Stage1")
            {
                jsonName = "Stage_1.json";
                Debug.Log($"StageLoader: Stage1 のため JSON を {jsonName} に強制変更しました");
            }
            // Stage2 は Stage_2.json
            else if (sceneName == "Stage2")
            {
                jsonName = "Stage_2.json";
            }
            // Stage3 は Stage_3.json
            else if (sceneName == "Stage3")
            {
                jsonName = "Stage_3.json";
            }
            // それ以外の数字シーン（Stage4 以降）も対応
            else
            {
                string number = System.Text.RegularExpressions.Regex.Match(sceneName, @"\d+").Value;
                if (string.IsNullOrEmpty(number))
                {
                    Debug.LogError($"StageLoader: シーン名から数字を取得できません: {sceneName}");
                    return;
                }
                jsonName = $"Stage_{number}.json";
            }

            string directory = Path.Combine(Application.streamingAssetsPath, "StageData");
            string filePath = Path.Combine(directory, jsonName);

            if (!File.Exists(filePath))
            {
                Debug.LogError($"StageLoader: JSON ファイルが見つかりません → {filePath}");
                return;
            }

            string json = File.ReadAllText(filePath);

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("StageLoader: JSON が空です（読み込み失敗）");
                return;
            }

            // StageCreater側の_hasCreatedフラグをリセット
            Debug.Log("StageLoader: ステージ生成を開始します");

            _stageCreater.SetJsonAndCreate(json);
            _stageCreater.BGCreate();
            _loaded = true;

            Debug.Log($"StageLoader: {jsonName} からステージ生成が完了しました");
        }
    }
}