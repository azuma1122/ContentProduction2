using Game.StageScene;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 複数のボタンのうち指定数以上が押されるとブロックが消えるギミック
/// </summary>
public class MultiButtonGimmickController : MonoBehaviour
{
    [Header("ギミック設定")]
    // このギミックの識別ID（StageCreaterから設定）
    [SerializeField] private string _gimmickId;

    [SerializeField] private List<ButtonController> _buttons = new List<ButtonController>();

    // ターゲットブロックを複数管理
    [SerializeField] private List<GameObject> _targetBlocks = new List<GameObject>();

    [SerializeField] private int _requiredButtonCount = 2; // 必要なボタンの数
    [SerializeField] private bool _autoFindButtons = false;

    [Header("デバッグ")]
    [SerializeField] private bool _showDebugLogs = false;

    private void Start()
    {
        if (_autoFindButtons && (_buttons == null || _buttons.Count == 0))
        {
            AutoFindButtons();
        }

        if (_showDebugLogs)
        {
            Debug.Log(
                $"MultiButtonGimmick: 初期化完了 " +
                $"- GimmickID: {_gimmickId} " +
                $"- ボタン数: {_buttons.Count} " +
                $"- 必要数: {_requiredButtonCount} " +
                $"- 対象ブロック数: {_targetBlocks.Count}"
            );
        }
    }

    /// <summary>
    /// gimmickId を持つボタンのみ自動取得
    /// </summary>
    private void AutoFindButtons()
    {
        ButtonController[] foundButtons = FindObjectsOfType<ButtonController>();

        _buttons = foundButtons
            .Where(b => b != null && b.gimmickId == _gimmickId)
            .ToList();

        if (_showDebugLogs)
        {
            Debug.Log(
                $"MultiButtonGimmick: GimmickID({_gimmickId}) " +
                $"のボタンを {_buttons.Count} 個検出しました"
            );
        }
    }

    private void Update()
    {
        if (_targetBlocks == null || _targetBlocks.Count == 0)
        {
            return;
        }

        if (_buttons == null || _buttons.Count == 0)
        {
            return;
        }

        bool enoughButtonsPressed = CheckAllButtonsPressed();

        // 登録されている全ブロックを制御
        foreach (GameObject block in _targetBlocks)
        {
            if (block != null)
            {
                // 必要数以上押されていたらブロック非表示、そうでなければ表示
                block.SetActive(!enoughButtonsPressed);
            }
        }
    }

    /// <summary>
    /// 必要数以上のボタンが押されているかをチェック
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

            // ボタンが押されている（DOWN = GetIsUpButton()がfalse）
            if (!button.GetIsUpButton())
            {
                pressedCount++;
            }
        }

        bool enoughPressed = (pressedCount >= _requiredButtonCount);

        if (_showDebugLogs)
        {
            Debug.Log(
                $"MultiButtonGimmick({_gimmickId}): " +
                $"押下 {pressedCount}/{_requiredButtonCount} " +
                $"- ブロック表示: {!enoughPressed}"
            );
        }

        return enoughPressed;
    }

    // ===== StageCreater から呼ぶ用のメソッド =====

    /// <summary>
    /// GimmickIDを設定
    /// </summary>
    public void SetGimmickId(string gimmickId)
    {
        _gimmickId = gimmickId;

        if (_showDebugLogs)
        {
            Debug.Log($"MultiButtonGimmick: GimmickID設定 - {gimmickId}");
        }
    }

    /// <summary>
    /// ボタンのリストを設定
    /// </summary>
    public void SetButtons(List<ButtonController> buttons)
    {
        _buttons = buttons;

        if (_showDebugLogs)
        {
            Debug.Log($"MultiButtonGimmick: {buttons.Count}個のボタンを設定しました");
        }
    }

    /// <summary>
    /// 単一のターゲットブロックを設定（互換性のため）
    /// </summary>
    public void SetTargetBlock(GameObject block)
    {
        if (block == null)
        {
            return;
        }

        if (!_targetBlocks.Contains(block))
        {
            _targetBlocks.Add(block);

            if (_showDebugLogs)
            {
                Debug.Log(
                    $"MultiButtonGimmick: ターゲットブロック設定 - {block.name} " +
                    $"(合計 {_targetBlocks.Count})"
                );
            }
        }
    }

    /// <summary>
    /// ターゲットブロックを追加
    /// </summary>
    public void AddTargetBlock(GameObject block)
    {
        if (block == null)
        {
            return;
        }

        if (!_targetBlocks.Contains(block))
        {
            _targetBlocks.Add(block);

            if (_showDebugLogs)
            {
                Debug.Log(
                    $"MultiButtonGimmick: ターゲットブロック追加 - {block.name} " +
                    $"(合計 {_targetBlocks.Count})"
                );
            }
        }
    }

    /// <summary>
    /// 必要なボタン数を設定
    /// </summary>
    public void SetRequiredButtonCount(int count)
    {
        _requiredButtonCount = count;

        if (_showDebugLogs)
        {
            Debug.Log($"MultiButtonGimmick: 必要ボタン数設定 - {count}");
        }
    }

    /// <summary>
    /// デバッグ用：現在のボタンの状態を取得
    /// </summary>
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