using System.Collections;
using System.Collections.Generic;
using Game;
using UnityEngine;

public class GlobalUIManager : MonoBehaviour
{
    [SerializeField] GameObject configMenu; // ← 設定メニューのパネル

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
        configMenu.SetActive(!isActive);  // ← 非表示 → 表示 / 表示 → 非表示
    }
}