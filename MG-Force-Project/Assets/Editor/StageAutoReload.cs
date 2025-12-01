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
    // JSONファイルの監視対象パス（StageCreaterが参照しているファイル）
    private static readonly string JsonPath = Path.Combine(Application.dataPath, "StreamingAssets/StageData/Stage_1.json");

    // ファイルの監視用
    private static FileSystemWatcher _watcher;
    private static DateTime _lastUpdateTime;

    // コンストラクタ（エディタ起動時またはスクリプトリロード時に自動実行）
    static StageAutoReload()
    {
        StartWatching();
    }

    /// <summary>
    /// JSONファイルを監視して変更時に再読み込みする
    /// </summary>
    private static void StartWatching()
    {
        if (!File.Exists(JsonPath))
        {
            Debug.LogWarning($"監視対象のJSONファイルが見つからない: {JsonPath}");
            return;
        }

        // ファイル監視を初期化
        _watcher = new FileSystemWatcher
        {
            Path = Path.GetDirectoryName(JsonPath),
            Filter = Path.GetFileName(JsonPath),
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };

        _watcher.Changed += OnFileChanged;
        _watcher.EnableRaisingEvents = true;

        Debug.Log($"JSON監視を開始: {JsonPath}");
    }

    /// <summary>
    /// JSONファイルが変更されたときに呼ばれる処理
    /// </summary>
    private static void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // 短時間で複数回呼ばれないように制御
        var now = DateTime.Now;
        if ((now - _lastUpdateTime).TotalSeconds < 1) return;
        _lastUpdateTime = now;

        EditorApplication.delayCall += ReloadStage;
    }

    /// <summary>
    /// StageCreaterを探してステージを再生成
    /// </summary>
    private static void ReloadStage()
    {
        // シーン内の StageCreater を検索
        var stageCreaterObj = GameObject.Find("StageCreater");
        if (stageCreaterObj == null)
        {
            Debug.LogWarning("[StageAutoReload] StageCreater がシーン内に存在しない");
            return;
        }

        var creater = stageCreaterObj.GetComponent<Game.StageScene.StageCreater>();
        if (creater == null)
        {
            Debug.LogWarning("StageCreater コンポーネントが見つかりません");
            return;
        }

        // ステージ再生成（Excel→Json→再構築）
        creater.StageCreate();

        Debug.Log("Excelデータ更新を検知 → ステージを再生成");
    }
}
