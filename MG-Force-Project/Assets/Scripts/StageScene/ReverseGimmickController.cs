using Game.StageScene;
using UnityEngine;

/// <summary>
/// ボタンを押すとブロックが出現するギミックコントローラー（逆ギミック）
/// - 初期状態: ブロックは非表示
/// - ボタンが下がっている(DOWN)時: ブロック表示
/// - ボタンが上がっている(UP)時: ブロック非表示
/// </summary>
public class ReverseGimmickController : MonoBehaviour
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
            Debug.Log($"ReverseGimmickController: 起動 - GimmickID={_gimmickId}, アタッチ先={gameObject.name}");
        }

        TryFindButton();

        // 初期状態：ブロックを非表示にする
        if (_fixedBox != null)
        {
            _fixedBox.SetActive(false);

            if (_showDebugLogs)
            {
                Debug.Log("ReverseGimmickController: 初期状態でブロックを非表示にしました");
            }
        }
    }

    private void Update()
    {
        if (_fixedBox == null)
        {
            return;
        }

        if (_button == null)
        {
            TryFindButton();
            return;
        }

        // ボタンが下がっている(押されている)時にブロックを表示
        bool isButtonDown = !_button.GetIsUpButton();
        _fixedBox.SetActive(isButtonDown);

        if (_showDebugLogs)
        {
            Debug.Log($"ReverseGimmickController({_gimmickId}): ボタン={(_button.GetIsUpButton() ? "UP" : "DOWN")}, ブロック={(_fixedBox.activeSelf ? "表示" : "非表示")}");
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
                    Debug.Log($"ReverseGimmickController: GimmickID({_gimmickId})のボタンを取得しました - {btn.gameObject.name}");
                }
                return;
            }
        }

        // 見つからなかった場合
        _button = null;

        if (_showDebugLogs)
        {
            Debug.LogWarning($"ReverseGimmickController: GimmickID({_gimmickId})のボタンが見つかりません");
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
            Debug.Log($"ReverseGimmickController: GimmickID設定 - {gimmickId}");
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
            Debug.Log($"ReverseGimmickController: ボタンが設定されました - {button.gameObject.name}");
        }
    }

    /// <summary>
    /// StageCreaterから呼ばれる：ブロックを設定
    /// </summary>
    public void SetFixedBox(GameObject block)
    {
        _fixedBox = block;

        // 設定時に非表示にする
        if (_fixedBox != null)
        {
            _fixedBox.SetActive(false);
        }

        if (_showDebugLogs)
        {
            Debug.Log($"ReverseGimmickController: ブロックが設定されました - {block.name}");
        }
    }
}