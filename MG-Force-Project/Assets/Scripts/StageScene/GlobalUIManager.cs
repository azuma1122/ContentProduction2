using System.Collections;
using System.Collections.Generic;
using Game;
using UnityEngine;

/// <summary>
/// グローバルUI管理クラス（修正版）
/// - 設定メニューの表示/非表示
/// - ゲームのポーズ/再開制御
/// - 複数インスタンス問題の解決
/// </summary>
public class GlobalUIManager : MonoBehaviour
{
    [SerializeField] GameObject configMenu;

    private static GlobalUIManager _instance;
    public static GlobalUIManager Instance
    {
        get
        {
            if (_instance == null || _instance.gameObject == null)
            {
                _instance = FindObjectOfType<GlobalUIManager>();
            }
            return _instance;
        }
    }

    public bool IsPaused { get; private set; } = false;

    private void Awake()
    {
        Debug.Log($"[GlobalUIManager] Awake() 開始 - GameObject: {gameObject.name}");

        // ★★★ 修正：シングルトンチェックの改善 ★★★
        var allManagers = FindObjectsOfType<GlobalUIManager>();
        if (allManagers.Length > 1)
        {
            Debug.LogWarning($"[GlobalUIManager] 複数インスタンス検出({allManagers.Length}個)");

            // _instanceが既に設定されている場合、自分が重複インスタンス
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[GlobalUIManager] 重複インスタンスを即座に破棄: {gameObject.name}");
                DestroyImmediate(gameObject);
                return;
            }

            // _instanceがまだない場合、他のインスタンスを削除
            foreach (var manager in allManagers)
            {
                if (manager != this)
                {
                    Debug.LogWarning($"[GlobalUIManager] 他のインスタンスを破棄: {manager.gameObject.name}");
                    DestroyImmediate(manager.gameObject);
                }
            }
        }

        _instance = this;

        // ★★★ 重要：初期化時に必ず状態をリセット ★★★
        ForceResetGameState();

        Debug.Log($"[GlobalUIManager] 初期化完了: Time.timeScale = {Time.timeScale}, IsPaused = {IsPaused}");
    }

    /// <summary>
    /// ゲームの状態を強制的にリセット
    /// </summary>
    public void ForceResetGameState()
    {
        Debug.Log("[GlobalUIManager] ForceResetGameState() 実行");

        Time.timeScale = 1f;
        IsPaused = false;

        // 物理シミュレーションモードもリセット
        if (Physics.simulationMode != SimulationMode.FixedUpdate)
        {
            Debug.LogWarning($"[GlobalUIManager] 物理シミュレーションモードが異常: {Physics.simulationMode} → FixedUpdate に変更");
            Physics.simulationMode = SimulationMode.FixedUpdate;
        }

        // Physics2D も確認
        if (Physics2D.simulationMode != SimulationMode2D.FixedUpdate)
        {
            Debug.LogWarning($"[GlobalUIManager] Physics2D.simulationMode が異常 → FixedUpdate に変更");
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
        }

        // ConfigMenuが開いている場合は閉じる
        if (configMenu != null && configMenu.activeSelf)
        {
            Debug.Log("[GlobalUIManager] ForceResetGameState: ConfigMenuを閉じます");
            configMenu.SetActive(false);
        }

        Debug.Log($"[GlobalUIManager] ForceResetGameState完了: Time.timeScale={Time.timeScale}, IsPaused={IsPaused}");
    }

    void Update()
    {
        // Mキーで設定メニューを開く/閉じる
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleConfigMenu();
        }

        // デバッグ用
        if (Input.GetKeyDown(KeyCode.L))
        {
            DebugStatus();
        }

        // ★★★ 緊急リセットキー（F9キー） ★★★
        if (Input.GetKeyDown(KeyCode.F9))
        {
            Debug.LogWarning("[GlobalUIManager] 緊急リセット実行！");
            ForceResetGameState();
        }
    }

    private void DebugStatus()
    {
        Debug.Log("========== GlobalUIManager 状態確認 ==========");
        Debug.Log($"GameObject名: {gameObject.name}");
        Debug.Log($"IsPaused: {IsPaused}");
        Debug.Log($"Time.timeScale: {Time.timeScale}");
        Debug.Log($"Physics.simulationMode: {Physics.simulationMode}");
        Debug.Log($"Physics2D.simulationMode: {Physics2D.simulationMode}");
        Debug.Log($"_instance == this: {_instance == this}");

        var allManagers = FindObjectsOfType<GlobalUIManager>();
        Debug.Log($"シーン内のGlobalUIManager数: {allManagers.Length}");
        foreach (var manager in allManagers)
        {
            Debug.Log($"  - {manager.gameObject.name}, IsPaused: {manager.IsPaused}, _instance==this: {manager == _instance}");
        }

        // Canvas の状態も確認
        var canvases = FindObjectsOfType<Canvas>();
        Debug.Log($"シーン内のCanvas数: {canvases.Length}");
        foreach (var canvas in canvases)
        {
            var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            Debug.Log($"  - Canvas: {canvas.name}, enabled: {canvas.enabled}, Raycaster: {raycaster != null && raycaster.enabled}");
        }

        Debug.Log("=============================================");
    }

    public void ToggleConfigMenu()
    {
        if (configMenu == null)
        {
            Debug.LogError("[GlobalUIManager] configMenu が設定されていません！");
            return;
        }

        bool isActive = configMenu.activeSelf;
        configMenu.SetActive(!isActive);

        if (configMenu.activeSelf)
        {
            Pause();
        }
        else
        {
            Resume();
        }
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        Debug.Log($"[GlobalUIManager:{gameObject.name}] ゲームをポーズ: Time.timeScale = 0f");
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        // 物理シミュレーションも確認 
        if (Physics.simulationMode != SimulationMode.FixedUpdate)
        {
            Debug.LogWarning("[GlobalUIManager] Resume時に物理モードを修正");
            Physics.simulationMode = SimulationMode.FixedUpdate;
        }

        if (Physics2D.simulationMode != SimulationMode2D.FixedUpdate)
        {
            Debug.LogWarning("[GlobalUIManager] Resume時にPhysics2Dモードを修正");
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
        }

        Debug.Log($"[GlobalUIManager:{gameObject.name}] ゲームを再開: Time.timeScale = 1f");
    }

    private void OnDestroy()
    {
        Debug.Log($"[GlobalUIManager] OnDestroy() 呼び出し: GameObject={gameObject.name}");

        // シーン遷移時に次のシーンでリセットされるべきなので、
        // ここでは_instanceの参照だけクリア
        if (_instance == this)
        {
            _instance = null;
            Debug.Log("[GlobalUIManager] 静的インスタンス参照をnullにリセット");
        }
    }

    private void Start()
    {
        Debug.Log($"[GlobalUIManager] Start() 呼び出し: Time.timeScale = {Time.timeScale}");

        // Start時にも状態確認
        if (Time.timeScale != 1f || IsPaused)
        {
            Debug.LogWarning($"[GlobalUIManager] Start()時点で状態が異常。強制リセット実行");
            ForceResetGameState();
        }

        var allManagers = FindObjectsOfType<GlobalUIManager>();
        if (allManagers.Length > 1)
        {
            Debug.LogError($"[GlobalUIManager] Start時点で{allManagers.Length}個のインスタンスが存在！");

            // 自分以外を削除
            foreach (var manager in allManagers)
            {
                if (manager != this && manager != null)
                {
                    Debug.LogWarning($"[GlobalUIManager] Start時に重複削除: {manager.gameObject.name}");
                    DestroyImmediate(manager.gameObject);
                }
            }
        }
    }

    /// <summary>
    /// シーン遷移前の後処理
    /// </summary>
    private void OnApplicationQuit()
    {
        // アプリ終了時はリセット不要
        _instance = null;
    }
}