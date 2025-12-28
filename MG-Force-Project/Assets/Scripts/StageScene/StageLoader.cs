using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

namespace Game.StageScene
{
    /// <summary>
    /// 現在のシーン名に対応した JSON ステージを読み込み、
    /// StageCreater に渡してステージを生成するクラス
    /// </summary>
    public class StageLoader : MonoBehaviour
    {
        [Header("JSON ステージを使用するか")]
        [SerializeField]
        private bool _useJsonStage = true;

        [Header("ステージ生成クラス")]
        [SerializeField]
        private StageCreater _stageCreater;

        // 多重生成防止フラグ
        private bool _loaded = false;

        private void Awake()
        {
            // StageCreater が未設定なら自動取得
            if (_stageCreater == null)
            {
                _stageCreater = GetComponentInChildren<StageCreater>();

                if (_stageCreater != null)
                {
                    Debug.Log("[StageLoader] StageCreater を自動取得しました");
                }
                else
                {
                    Debug.LogError("[StageLoader] StageCreater が見つかりません（子オブジェクト含む）");
                }
            }

            // シーン開始時は未ロード状態にする
            _loaded = false;

            Debug.Log($"[StageLoader] Awake - シーン: {SceneManager.GetActiveScene().name}");
        }

        private void Start()
        {
            Debug.Log($"[StageLoader] Start - シーン: {SceneManager.GetActiveScene().name}");

            // ステージ選択画面では読み込まない
            if (SceneManager.GetActiveScene().name == "StageSelect")
            {
                Debug.Log("[StageLoader] StageSelectシーンのため読み込みをスキップ");
                return;
            }

            // JSON を使わない設定なら何もしない
            if (!_useJsonStage)
            {
                Debug.Log("[StageLoader] JSONステージ使用がOFFのため読み込みをスキップ");
                return;
            }

            // 既にロード済みなら処理しない
            if (_loaded)
            {
                Debug.Log("[StageLoader] 既にロード済みのため読み込みをスキップ");
                return;
            }

            LoadStageJson();
        }

        /// <summary>
        /// シーン名に対応した JSON を読み込み、ステージを生成する
        /// </summary>
        private void LoadStageJson()
        {
            // StageCreater が未設定なら中断
            if (_stageCreater == null)
            {
                Debug.LogError("[StageLoader] StageCreater が設定されていません");
                return;
            }

            // シーン名から JSON ファイル名を決定
            string sceneName = SceneManager.GetActiveScene().name;
            string jsonName = GetJsonFileName(sceneName);

            if (string.IsNullOrEmpty(jsonName))
            {
                Debug.LogError($"[StageLoader] シーン名から JSON ファイル名を取得できません: {sceneName}");
                return;
            }

            Debug.Log($"[StageLoader] 読み込むJSONファイル: {jsonName}");

            // JSON ファイルのパスを作成
            string filePath = Path.Combine(
                Application.streamingAssetsPath,
                "StageData",
                jsonName
            );

            Debug.Log($"[StageLoader] JSONファイルパス: {filePath}");

            // ファイル存在チェック
            if (!File.Exists(filePath))
            {
                Debug.LogError($"[StageLoader] JSON が見つかりません → {filePath}");
                Debug.LogError($"[StageLoader] StreamingAssetsPath: {Application.streamingAssetsPath}");

                // 代替パスも試す
                string alternativePath = Path.Combine(
                    Application.dataPath,
                    "StreamingAssets",
                    "StageData",
                    jsonName
                );

                Debug.Log($"[StageLoader] 代替パスを確認: {alternativePath}");

                if (File.Exists(alternativePath))
                {
                    filePath = alternativePath;
                    Debug.Log("[StageLoader] 代替パスで発見しました");
                }
                else
                {
                    return;
                }
            }

            // JSON 読み込み
            string json = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[StageLoader] JSON の読み込みに失敗しました（空のファイル）");
                return;
            }

            Debug.Log($"[StageLoader] JSON読み込み成功 (長さ: {json.Length} 文字)");
            Debug.Log($"[StageLoader] JSON内容（最初の200文字）: {json.Substring(0, Mathf.Min(200, json.Length))}");

            // ステージ生成
            _stageCreater.SetJsonAndCreate(json);

            _loaded = true;
            Debug.Log($"[StageLoader] ステージ生成完了: {jsonName}");
        }

        /// <summary>
        /// シーン名からJSONファイル名を取得
        /// </summary>
        private string GetJsonFileName(string sceneName)
        {
            switch (sceneName)
            {
                case "Stage1":
                    return "Stage_5.json";
                case "Stage2":
                    return "Stage_6.json";
                case "Stage3":
                    return "Stage_7.json";
                default:
                    string number = System.Text.RegularExpressions.Regex
                        .Match(sceneName, @"\d+").Value;

                    if (!string.IsNullOrEmpty(number))
                    {
                        return $"Stage_{number}.json";
                    }
                    return null;
            }
        }

        /// <summary>
        /// 外部から強制的にステージをリロードする
        /// </summary>
        public void ForceReloadStage()
        {
            Debug.Log("[StageLoader] ForceReloadStage - 強制リロード開始");

            _loaded = false;
            LoadStageJson();
        }
    }
}
