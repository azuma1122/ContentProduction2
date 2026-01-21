using Game.StageScene;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 指定した1つのボタンだけを使う逆ギミック
/// - 初期状態：必ず非表示
/// - ボタンDOWN：表示
/// - ボタンUP：非表示
/// </summary>
public class ReverseGimmickController : MonoBehaviour
{
    [Header("ギミック設定")]
    [SerializeField] private List<GameObject> _fixedBoxes = new List<GameObject>();
    [SerializeField] private ButtonController _targetButton;

    [Header("デバッグ")]
    [SerializeField] private bool _showDebugLogs = true;

    private void Start()
    {
        // 初期状態は必ず非表示
        foreach (GameObject box in _fixedBoxes)
        {
            if (box != null)
            {
                box.SetActive(false);
            }
        }

        if (_showDebugLogs)
        {
            if (_fixedBoxes.Count == 0)
                Debug.LogWarning("[ReverseGimmick] FixedBox が設定されていません");
            if (_targetButton == null)
                Debug.LogWarning("[ReverseGimmick] TargetButton が設定されていません");
            else
                Debug.Log(
                    $"[ReverseGimmick] 使用ボタン={_targetButton.gameObject.name}, ブロック数={_fixedBoxes.Count}"
                );
        }
    }

    private void Update()
    {
        if (_fixedBoxes.Count == 0 || _targetButton == null)
            return;

        // ボタンが押されているか（DOWN）
        bool isButtonDown = !_targetButton.GetIsUpButton();

        // 全てのブロックに対してボタンDOWNなら表示、UPなら非表示
        foreach (GameObject box in _fixedBoxes)
        {
            if (box != null)
            {
                box.SetActive(isButtonDown);
            }
        }
    }

    /// <summary>
    /// 外部からボタンを設定する（StageCreaterから呼ばれる）
    /// </summary>
    public void SetButton(ButtonController button)
    {
        _targetButton = button;

        if (_showDebugLogs)
        {
            Debug.Log($"[ReverseGimmick] ボタン設定: {button.gameObject.name}");
        }
    }

    /// <summary>
    /// 外部からブロックを設定する（StageCreaterから呼ばれる、複数回呼び出し可能）
    /// </summary>
    public void SetFixedBox(GameObject block)
    {
        if (block != null && !_fixedBoxes.Contains(block))
        {
            _fixedBoxes.Add(block);
            block.SetActive(false); 

            if (_showDebugLogs)
            {
                Debug.Log(
                    $"[ReverseGimmick] ブロック追加（非表示設定）: {block.name}（合計: {_fixedBoxes.Count}個）"
                );
            }
        }
    }
}