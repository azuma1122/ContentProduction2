using Game.StageScene;
using UnityEngine;
using System.Collections.Generic;

public class MultiButtonGimmickController : MonoBehaviour
{
    [Header("ギミックID")]
    [SerializeField] private string _gimmickId;

    [Header("ボタン設定")]
    [SerializeField] private List<ButtonController> _buttons = new List<ButtonController>();

    [Header("ターゲットブロック")]
    [SerializeField] private GameObject _targetBlock;

    [Header("必要なボタン数")]
    [SerializeField] private int _requiredButtonCount = 2;

    [Header("デバッグ")]
    [SerializeField] private bool _showDebugLogs = true;

    private int _lastPressedCount = -1;

    private void Start()
    {
        AutoSetup();

        if (_targetBlock != null)
        {
            _targetBlock.SetActive(true);
            Debug.Log("[MultiButtonGimmick] ターゲットブロック初期状態: 表示");
        }
    }

    /// <summary>
    /// TargetBlock と Button を自動取得
    /// </summary>
    private void AutoSetup()
    {
        // ===== TargetBlock（子から取得）=====
        if (_targetBlock == null)
        {
            Transform t = transform.Find("TargetBlock");
            if (t != null)
            {
                _targetBlock = t.gameObject;
                Debug.Log("[MultiButtonGimmick] TargetBlock を自動取得");
            }
            else
            {
                Debug.LogError("[MultiButtonGimmick] 子に TargetBlock が見つかりません");
            }
        }

        // ===== Button（Scene全体から gimmickId で取得）=====
        if (_buttons == null || _buttons.Count == 0)
        {
            _buttons = new List<ButtonController>();
            ButtonController[] allButtons = FindObjectsOfType<ButtonController>();

            Debug.Log($"[MultiButtonGimmick] シーン内の全ボタン数: {allButtons.Length}");

            foreach (var button in allButtons)
            {
                Debug.Log($"[MultiButtonGimmick] 検査中 - ボタン名:{button.gameObject.name}, ID:'{button.gimmickId}' vs 期待:'{_gimmickId}'");

                if (button.gimmickId == _gimmickId)
                {
                    _buttons.Add(button);
                    Debug.Log($"[MultiButtonGimmick] ボタン追加: {button.gameObject.name}");
                }
            }

            Debug.Log($"[MultiButtonGimmick] gimmickId({_gimmickId}) のボタン {_buttons.Count} 個を取得");
        }
    }

    private void Update()
    {
        if (_targetBlock == null || _buttons == null || _buttons.Count == 0)
            return;

        bool enoughButtonsPressed = CheckButtonsPressed();
        bool shouldBeActive = !enoughButtonsPressed;

        if (_targetBlock.activeSelf != shouldBeActive)
        {
            _targetBlock.SetActive(shouldBeActive);

            if (_showDebugLogs)
            {
                Debug.Log($"[MultiButtonGimmick] ブロック状態変更: {(shouldBeActive ? "表示" : "非表示")}");
            }
        }
    }

    private bool CheckButtonsPressed()
    {
        int pressedCount = 0;

        for (int i = 0; i < _buttons.Count; i++)
        {
            ButtonController button = _buttons[i];
            if (button == null)
                continue;

            bool isPressed = !button.GetIsUpButton();
            if (isPressed)
                pressedCount++;
        }

        bool enoughPressed = pressedCount >= _requiredButtonCount;

        if (_showDebugLogs && _lastPressedCount != pressedCount)
        {
            Debug.Log($"[MultiButtonGimmick] 押下 {pressedCount}/{_buttons.Count} → {(enoughPressed ? "非表示" : "表示")}");
        }

        _lastPressedCount = pressedCount;
        return enoughPressed;
    }

    public void SetButtons(List<ButtonController> buttons)
    {
        _buttons = buttons;
    }

    public void SetTargetBlock(GameObject block)
    {
        _targetBlock = block;
    }

    public void SetRequiredButtonCount(int count)
    {
        _requiredButtonCount = count;
    }

    /// <summary>
    /// 外部からギミックIDを設定する（StageCreaterから呼ばれる）
    /// </summary>
    public void SetGimmickId(string id)
    {
        _gimmickId = id;
        Debug.Log($"[MultiButtonGimmick] ギミックID設定: {_gimmickId}");
    }

    public string GetButtonStates()
    {
        if (_buttons == null || _buttons.Count == 0)
            return "ボタンなし";

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

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_targetBlock == null || _buttons == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, _targetBlock.transform.position);

        Gizmos.color = Color.cyan;
        foreach (var b in _buttons)
        {
            if (b != null)
                Gizmos.DrawLine(transform.position, b.transform.position);
        }
    }
#endif
}