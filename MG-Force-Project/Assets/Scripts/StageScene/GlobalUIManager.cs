using System.Collections;
using System.Collections.Generic;
using Game;
using UnityEngine;

public class GlobalUIManager : MonoBehaviour
{
    [SerializeField] GameObject configMenu; // ← 設定メニューのパネル

    // ===== ポーズ管理機能を統合 =====
    private static GlobalUIManager _instance;
    public static GlobalUIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GlobalUIManager>();
            }
            return _instance;
        }
    }

    /// <summary>
    /// ゲームがポーズ中かどうか
    /// </summary>
    public bool IsPaused { get; private set; } = false;

    private void Awake()
    {
        // シングルトン設定
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleConfigMenu();
        }
    }

    public void ToggleConfigMenu()
    {
        bool isActive = configMenu.activeSelf;
        // メニューをON/OFF
        configMenu.SetActive(!isActive);

        // ゲーム停止・再開
        if (configMenu.activeSelf)
        {
            Pause();
        }
        else
        {
            Resume();
        }
    }

    /// <summary>
    /// ゲームをポーズする
    /// </summary>
    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
    }

    /// <summary>
    /// ゲームを再開する
    /// </summary>
    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
    }
}