using UnityEditor;
using UnityEngine;
using System.IO;
using System;

/// <summary>
/// Excel（JSON）データの変更を検知して、Unityエディタ上で自動的にステージを再生成するクラス
/// </summary>
[InitializeOnLoad]
public static class StageAutoReload
{
    // JSONファイルの監視対象パス
    private static readonly string JsonPath = Path.Combine(Application.dataPath, "StreamingAssets/StageData/Stage_5.json");

    private static FileSystemWatcher _watcher;
    private static DateTime _lastUpdateTime;

    static StageAutoReload()
    {
        StartWatching();
    }

    private static void StartWatching()
    {
        string directory = Path.GetDirectoryName(JsonPath);

        if (!Directory.Exists(directory))
        {
            Debug.LogWarning($"監視対象のディレクトリが見つからない: {directory}");
            return;
        }

        // ファイル監視を初期化
        _watcher = new FileSystemWatcher
        {
            Path = directory,
            Filter = "*.json",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };

        _watcher.Changed += OnFileChanged;
        _watcher.EnableRaisingEvents = true;

        Debug.Log($"JSON監視を開始: {directory}");
    }

    private static void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // 短時間で複数回呼ばれないように制御
        var now = DateTime.Now;
        if ((now - _lastUpdateTime).TotalSeconds < 1) return;
        _lastUpdateTime = now;

        EditorApplication.delayCall += () => ReloadStage(e.FullPath);
    }

    private static void ReloadStage(string changedFilePath)
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.Log($"再生中ではないためリロードをスキップ: {Path.GetFileName(changedFilePath)}");
            return;
        }

        var stageLoaderObj = UnityEngine.Object.FindObjectOfType<Game.StageScene.StageLoader>();
        if (stageLoaderObj == null)
        {
            Debug.LogWarning("[StageAutoReload] StageLoader が見つかりません");
            return;
        }

        // ファイルを読み込んで再生成
        if (File.Exists(changedFilePath))
        {
            string json = File.ReadAllText(changedFilePath);

            var stageCreater = UnityEngine.Object.FindObjectOfType<Game.StageScene.StageCreater>();
            if (stageCreater != null)
            {
                // 前のステージをクリーンアップ
                foreach (var obj in GameObject.FindGameObjectsWithTag("MainStage"))
                    UnityEngine.Object.DestroyImmediate(obj);
                foreach (var obj in GameObject.FindGameObjectsWithTag("Player"))
                    UnityEngine.Object.DestroyImmediate(obj);

                // 新しいステージを生成
                stageCreater.SetJsonAndCreate(json);
                Debug.Log($"JSONデータ更新を検知 → ステージを再生成: {Path.GetFileName(changedFilePath)}");
            }
        }
    }
}