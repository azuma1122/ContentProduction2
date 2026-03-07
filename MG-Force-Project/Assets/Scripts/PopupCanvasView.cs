using UnityEngine;
using UnityEngine.UI;
using TMPro;

///<summary>
/// PopupCanvas内のUI参照を管理するクラス
/// Prefab生成時にボタンを自動取得する
///</summary>
public class PopupCanvasView : MonoBehaviour
{
    [Header("背景Root")]
    public Transform backgroundRoot;

    [Header("UI Root")]
    public Transform controlRoot;

    [Header("ボタン")]
    public Button nextButton;
    public Button prevButton;
    public Button destroyButton;

    [Header("テキスト")]
    public TextMeshProUGUI text;

    ///<summary>
    /// Prefab生成時にUIを自動取得
    ///</summary>
    void Awake()
    {
        // Root取得
        if (backgroundRoot == null)
            backgroundRoot = transform.Find("BackgroundRoot");

        if (controlRoot == null)
            controlRoot = transform.Find("ControlRoot");

        if (controlRoot == null)
        {
            Debug.LogError("ControlRoot が見つかりません");
            return;
        }

        // Next
        if (nextButton == null)
            nextButton = controlRoot.Find("NextButton")?.GetComponent<Button>();

        // Prev (ChangeImage_Return)
        if (prevButton == null)
            prevButton = controlRoot.Find("ChangeImage_Return")?.GetComponent<Button>();

        // Close
        if (destroyButton == null)
            destroyButton = controlRoot.Find("DestroyButton")?.GetComponent<Button>();

        // Text
        if (text == null)
            text = controlRoot.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();

        // Debug
        if (nextButton == null)
            Debug.LogWarning("NextButton が見つかりません");

        if (prevButton == null)
            Debug.LogWarning("PrevButton が見つかりません");

        if (destroyButton == null)
            Debug.LogWarning("DestroyButton が見つかりません");
    }
}