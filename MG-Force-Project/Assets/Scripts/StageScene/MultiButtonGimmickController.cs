using Game.StageScene;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 複数のボタンのうち2つ以上が押されるとブロックが消えるギミック
/// </summary>
public class MultiButtonGimmickController : MonoBehaviour
{
    private List<ButtonController> _buttons = new List<ButtonController>();
    private GameObject _targetBlock;

    [Header("デバッグ")]
    [SerializeField] private bool _showDebugLogs = false;

    private void Update()
    {
        if (_targetBlock == null)
        {
            return;
        }

        if (_buttons == null || _buttons.Count == 0)
        {
            return;
        }

        bool enoughButtonsPressed = CheckAllButtonsPressed();

        _targetBlock.SetActive(!enoughButtonsPressed);
    }

    /// <summary>
    /// 2つ以上のボタンが押されているかをチェック
    /// </summary>
    private bool CheckAllButtonsPressed()
    {
        int pressedCount = 0;

        foreach (ButtonController button in _buttons)
        {
            if (button == null)
            {
                continue;
            }

            // ボタンが押されている（DOWN）
            if (!button.GetIsUpButton())
            {
                pressedCount++;
            }
        }

        bool enoughPressed = (pressedCount >= 2);

        if (_showDebugLogs)
        {
            Debug.Log($"MultiButtonGimmick: 押されているボタン {pressedCount}個 - ブロック表示: {!enoughPressed}");
        }

        return enoughPressed;
    }

    public void SetButtons(List<ButtonController> buttons)
    {
        _buttons = buttons;

        if (_showDebugLogs)
        {
            Debug.Log($"MultiButtonGimmick: {buttons.Count}個のボタンを設定しました");
        }
    }

    public void SetTargetBlock(GameObject block)
    {
        _targetBlock = block;

        if (_showDebugLogs)
        {
            Debug.Log($"MultiButtonGimmick: ターゲットブロックを設定しました - {block.name}");
        }
    }

    public string GetButtonStates()
    {
        if (_buttons == null || _buttons.Count == 0)
        {
            return "ボタンなし";
        }

        string states = "";
        for (int i = 0; i < _buttons.Count; i++)
        {
            if (_buttons[i] != null)
            {
                bool isUp = _buttons[i].GetIsUpButton();
                states += $"Button{i + 1}: {(isUp ? "UP" : "DOWN")} ";
            }
        }

        return states;
    }
}
