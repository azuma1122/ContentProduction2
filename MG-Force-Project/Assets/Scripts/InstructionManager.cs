using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InstructionManager : MonoBehaviour
{
    [Header("表示する画像（3枚）")]
    public Sprite[] instructionImages;

    [Header("UIパーツ")]
    public Image imageDisplay;
    public Button prevButton;
    public Button nextButton;
    public Button closeButton;
    public TextMeshProUGUI pageText;

    private int currentIndex = 0;

    // Start → OnEnable に変更（表示のたびに初期化される）
    void OnEnable()
    {
        if (instructionImages == null || instructionImages.Length == 0)
        {
            Debug.LogWarning("InstructionManager: 画像が設定されていません");
            return;
        }

        // ボタンの重複登録を防ぐため一度クリア
        prevButton.onClick.RemoveAllListeners();
        nextButton.onClick.RemoveAllListeners();
        closeButton.onClick.RemoveAllListeners();

        prevButton.onClick.AddListener(ShowPrev);
        nextButton.onClick.AddListener(ShowNext);
        closeButton.onClick.AddListener(ClosePanel);

        currentIndex = 0;
        UpdateDisplay();

        // パネルが開いたらPlayer操作を無効化
        GameInputLock.Lock();
    }

    void ShowNext()
    {
        if (currentIndex < instructionImages.Length - 1)
        {
            currentIndex++;
            ChangeImage(instructionImages[currentIndex]);
            UpdatePageText();
            UpdateButtonState();
        }
    }

    void ShowPrev()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            ChangeImage(instructionImages[currentIndex]);
            UpdatePageText();
            UpdateButtonState();
        }
    }

    void ChangeImage(Sprite sprite)
    {
        if (imageDisplay == null)
        {
            Debug.LogError("imageDisplay が null です");
            return;
        }
        imageDisplay.sprite = sprite;
    }

    void UpdatePageText()
    {
        if (pageText == null || instructionImages == null) return;
        pageText.text = $"{currentIndex + 1}/{instructionImages.Length}";
    }

    void UpdateButtonState()
    {
        prevButton.interactable = (currentIndex > 0);
        nextButton.interactable = (currentIndex < instructionImages.Length - 1);
    }

    void UpdateDisplay()
    {
        ChangeImage(instructionImages[currentIndex]);
        UpdatePageText();
        UpdateButtonState();
    }

    void ClosePanel()
    {
        // パネルを閉じたらPlayer操作を再開
        GameInputLock.Unlock();
        gameObject.SetActive(false);
    }
}