using Game.StageScene;
using UnityEngine;

/// <summary>
/// ギミック全体の動作を制御するクラス
/// - 指定されたgimmickIdを持つボタンの状態に応じてブロックを制御
/// - ボタンが上がっている(UP)時: ブロック表示
/// - ボタンが下がっている(DOWN)時: ブロック非表示
/// </summary>
public class GimmickController : MonoBehaviour
{
    [Header("ギミック設定")]
    [SerializeField] private string _gimmickId; // このギミック専用のID

    private ButtonController _button;
    [SerializeField] private GameObject _fixedBox;

    [Header("デバッグ")]
    [SerializeField] private bool _showDebugLogs = false;

    private void Start()
    {
        if (_showDebugLogs)
        {
            Debug.Log($"GimmickController: 起動 - GimmickID={_gimmickId}, アタッチ先={gameObject.name}");
        }

        TryFindButton();
    }

    private void Update()
    {
        if (_fixedBox == null)
        {
            if (_showDebugLogs)
            {
                Debug.LogWarning("GimmickController: _fixedBox が設定されていません");
            }
            return;
        }

        if (_button == null)
        {
            TryFindButton();
            return;
        }

        // ボタンが上がっている時はブロック表示、下がっている時は非表示
        _fixedBox.SetActive(_button.GetIsUpButton());

        if (_showDebugLogs)
        {
            Debug.Log($"GimmickController({_gimmickId}): ボタン={(_button.GetIsUpButton() ? "UP" : "DOWN")}, ブロック={(_fixedBox.activeSelf ? "表示" : "非表示")}");
        }
    }

    /// <summary>
    /// 指定されたgimmickIdを持つButtonを探して取得
    /// </summary>
    private void TryFindButton()
    {
        // 全てのButtonControllerを取得
        ButtonController[] allButtons = FindObjectsOfType<ButtonController>();

        foreach (ButtonController btn in allButtons)
        {
            // gimmickIdが一致するボタンを探す
            if (btn.gimmickId == _gimmickId)
            {
                _button = btn;

                if (_showDebugLogs)
                {
                    Debug.Log($"GimmickController: GimmickID({_gimmickId})のボタンを取得しました - {btn.gameObject.name}");
                }
                return;
            }
        }

        // 見つからなかった場合
        _button = null;

        if (_showDebugLogs)
        {
            Debug.LogWarning($"GimmickController: GimmickID({_gimmickId})のボタンが見つかりません");
        }
    }

    /// <summary>
    /// StageCreaterから呼ばれる：GimmickIDを設定
    /// </summary>
    public void SetGimmickId(string gimmickId)
    {
        _gimmickId = gimmickId;

        if (_showDebugLogs)
        {
            Debug.Log($"GimmickController: GimmickID設定 - {gimmickId}");
        }
    }

    /// <summary>
    /// StageCreaterから呼ばれる：ボタンを直接設定
    /// </summary>
    public void SetButton(ButtonController button)
    {
        _button = button;

        if (_showDebugLogs)
        {
            Debug.Log($"GimmickController: ボタンが設定されました - {button.gameObject.name}");
        }
    }

    /// <summary>
    /// StageCreaterから呼ばれる：ブロックを設定
    /// </summary>
    public void SetFixedBox(GameObject block)
    {
        _fixedBox = block;

        if (_showDebugLogs)
        {
            Debug.Log($"GimmickController: ブロックが設定されました - {block.name}");
        }
    }
}