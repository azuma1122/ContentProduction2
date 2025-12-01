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
        // Destroyされた時にも安全に検出できる
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
            Debug.Log("Button を取得しました: " + obj.name);
        }
        else
        {
            _button = null; // Destroy検出s
        }
    }
}
