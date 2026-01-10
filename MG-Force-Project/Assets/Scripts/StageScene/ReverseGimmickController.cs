using Game.StageScene;
using UnityEngine;

/// <summary>
/// ボタンを押すとブロックが出現するギミックコントローラー（逆ギミック）
/// - 初期状態: ブロックは非表示
/// - ボタンを押す(DOWN): ブロックが表示される
/// - ボタンを離す(UP): ブロックが非表示になる
/// </summary>
public class ReverseGimmickController : MonoBehaviour
{
    private ButtonController _button;
    [SerializeField] private GameObject _fixedBox;

    [Header("デバッグ")]
    [SerializeField] private bool _showDebugLogs = false;

    private void Start()
    {
        if (_showDebugLogs)
        {
            Debug.Log($"ReverseGimmickController: 起動 - アタッチ先={gameObject.name}");
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
        // _fixedBoxが破壊されているかnullチェック
        if (_fixedBox == null)
        {
            return;
        }

        // ボタンが破壊されているか確認
        if (_button == null)
        {
            TryFindButton();
            return;
        }

        // ボタンが存在している時だけ動作
        // 通常版とは逆：ボタンが下がっている(押されている)時にブロックを表示
        bool isButtonDown = !_button.GetIsUpButton();
        _fixedBox.SetActive(isButtonDown);

        if (_showDebugLogs)
        {
            Debug.Log($"ReverseGimmickController: ボタン={(_button.GetIsUpButton() ? "UP" : "DOWN")}, ブロック={(_fixedBox.activeSelf ? "表示" : "非表示")}");
        }
    }

    /// <summary>
    /// Button(Clone) を探して取得する
    /// </summary>
    private void TryFindButton()
    {
        GameObject obj = GameObject.Find("Button(Clone)");
        if (obj != null)
        {
            _button = obj.GetComponent<ButtonController>();
            if (_button != null)
            {
                if (_showDebugLogs)
                {
                    Debug.Log($"ReverseGimmickController: Button を取得しました - {obj.name}");
                }
            }
            else
            {
                Debug.LogWarning("ReverseGimmickController: ButtonController コンポーネントが見つかりません");
            }
        }
        else
        {
            _button = null;
        }
    }

    /// <summary>
    /// 外部からボタンを設定する
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
    /// 外部からブロックを設定する
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