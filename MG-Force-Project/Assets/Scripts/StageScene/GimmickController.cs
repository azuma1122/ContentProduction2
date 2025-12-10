using Game.StageScene;
using UnityEngine;

/// <summary>
/// ギミック全体の動作を制御するクラス
/// - ボタンの状態に応じて特定のオブジェクト（_fixedBox）の有効/無効を切り替える
/// - ボタンオブジェクト(ButtonController)を動的に取得して監視する
/// </summary>
public class GimmickController : MonoBehaviour
{
    private ButtonController _button;
    [SerializeField] private GameObject _fixedBox;

    private void Start()
    {
        Debug.Log("このスクリプトがアタッチされているオブジェクト: " + gameObject.name);
        TryFindButton();
    }

    private void Update()
    {
        // _fixedBoxが破壊されているかnullチェック
        if (_fixedBox == null)
        {
            Debug.LogWarning("_fixedBox が破壊されているか設定されていません");
            return;
        }

        // ボタンが破壊されているか確認（Unityの破壊されたオブジェクトは == null で検出可能）
        if (_button == null)
        {
            TryFindButton();
            return;
        }

        // ボタンが存在している時だけ動作
        _fixedBox.SetActive(_button.GetIsUpButton());
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
                Debug.Log("Button を取得しました: " + obj.name);
            }
            else
            {
                Debug.LogWarning("ButtonController コンポーネントが見つかりません");
            }
        }
        else
        {
            _button = null; // Destroy検出
        }
    }
}