using Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// チュートリアルや操作説明の画像をポップアップで表示する管理クラス
/// ・Canvasを生成
/// ・画像を表示
/// ・Next / Prev / Close ボタン制御
/// </summary>
public class PopupImageManager : MonoBehaviour
{
    [SerializeField] private GameObject canvasPrefab;
    [SerializeField] private GameObject imagePrefab;

    [SerializeField, Header("操作説明")]
    private Sprite[] controlGuideImages;

    [SerializeField, Header("ルール説明（ステージ1初回のみ）")]
    private Sprite[] ruleTutorialImages;

    private Sprite[] currentImages;
    private static bool hasShownControlGuide = false;
    private GameObject canvasObject;
    private GameObject imageObject;
    private PopupCanvasView canvasView;
    private int currentImageIndex = 0;

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == GameConstants.Stage.Stage1.ToString())
        {
            if (!hasShownControlGuide)
            {
                // ルール説明 → 操作説明の順に表示
                ShowRuleTutorial();
                hasShownControlGuide = true;
            }
        }
    }

    /// <summary>
    /// 操作説明を表示（外部からボタンなどで呼ぶ用）
    /// </summary>
    public void ShowControlGuide()
    {
        StartControl(controlGuideImages);
    }

    /// <summary>
    /// ルールチュートリアル表示
    /// </summary>
    public void ShowRuleTutorial()
    {
        StartTutorial(ruleTutorialImages);
    }

    /// <summary>
    /// チュートリアル表示フラグリセット
    /// </summary>
    public static void ResetTutorialFlag()
    {
        hasShownControlGuide = false;
    }

    /// <summary>
    /// チュートリアル開始（入力ロックあり）
    /// </summary>
    private void StartTutorial(Sprite[] images)
    {
        if (images == null || images.Length == 0)
        {
            Debug.LogWarning("StartTutorial: 画像が設定されていません");
            return;
        }

        GameInputLock.Lock();

        currentImages = images;
        currentImageIndex = 0;

        SpawnCanvasWithImage(currentImages[currentImageIndex]);
    }

    /// <summary>
    /// 操作説明開始（入力ロックあり）
    /// </summary>
    private void StartControl(Sprite[] images)
    {
        if (images == null || images.Length == 0)
        {
            Debug.LogWarning("StartControl: 画像が設定されていません");
            return;
        }

        GameInputLock.Lock();

        currentImages = images;
        currentImageIndex = 0;

        SpawnCanvasWithImage(currentImages[currentImageIndex]);
    }

    /// <summary>
    /// Canvas生成 + 画像表示 + ボタン設定
    /// </summary>
    public void SpawnCanvasWithImage(Sprite sprite)
    {
        // 既存Canvas削除
        if (canvasObject != null)
        {
            Destroy(canvasObject);
        }

        // Canvas生成
        canvasObject = Instantiate(canvasPrefab);

        // CanvasView取得
        canvasView = canvasObject.GetComponent<PopupCanvasView>();

        if (canvasView == null)
        {
            Debug.LogError("PopupCanvasView が CanvasPrefab に付いていません");
            return;
        }

        // 画像生成
        imageObject = Instantiate(imagePrefab, canvasView.backgroundRoot);

        Image image = imageObject.GetComponentInChildren<Image>();
        if (image != null)
        {
            image.sprite = sprite;
        }

        // ページ表示テキスト更新
        UpdatePageText();

        // Next ボタン
        if (canvasView.nextButton != null)
        {
            canvasView.nextButton.onClick.RemoveAllListeners();
            canvasView.nextButton.onClick.AddListener(ShowNextImage);
        }
        else
        {
            Debug.LogWarning("nextButton が null です");
        }

        // Prev ボタン
        if (canvasView.prevButton != null)
        {
            canvasView.prevButton.onClick.RemoveAllListeners();
            canvasView.prevButton.onClick.AddListener(ShowPreviousImage);
        }
        else
        {
            Debug.LogWarning("prevButton が null です");
        }

        // Close ボタン
        if (canvasView.destroyButton != null)
        {
            canvasView.destroyButton.onClick.RemoveAllListeners();
            canvasView.destroyButton.onClick.AddListener(DestroyCanvasWithImage);
        }
        else
        {
            Debug.LogWarning("destroyButton が null です");
        }
    }

    /// <summary>
    /// 次の画像へ
    /// </summary>
    public void ShowNextImage()
    {
        if (currentImages == null) return;

        if (currentImageIndex < currentImages.Length - 1)
        {
            currentImageIndex++;
            ChangeImage(currentImages[currentImageIndex]);
            UpdatePageText();
        }
    }

    /// <summary>
    /// 前の画像へ
    /// </summary>
    public void ShowPreviousImage()
    {
        if (currentImages == null) return;

        if (currentImageIndex > 0)
        {
            currentImageIndex--;
            ChangeImage(currentImages[currentImageIndex]);
            UpdatePageText();
        }
    }

    /// <summary>
    /// 画像変更
    /// </summary>
    public void ChangeImage(Sprite sprite)
    {
        if (imageObject == null) return;

        Image image = imageObject.GetComponentInChildren<Image>();
        if (image != null)
        {
            image.sprite = sprite;
        }
    }

    /// <summary>
    /// ページ数テキスト更新（例: 1/3）
    /// </summary>
    private void UpdatePageText()
    {
        if (canvasView == null || canvasView.text == null) return;
        if (currentImages == null) return;

        canvasView.text.text = $"{currentImageIndex + 1}/{currentImages.Length}";
    }

    /// <summary>
    /// Canvas削除・入力ロック解除
    /// </summary>
    public void DestroyCanvasWithImage()
    {
        GameInputLock.Unlock();

        if (canvasObject != null)
        {
            Destroy(canvasObject);
            canvasObject = null;
            imageObject = null;
            canvasView = null;
        }
    }
}

/// <summary>
/// ゲーム入力ロック管理
/// </summary>
public static class GameInputLock
{
    public static bool IsLocked { get; private set; }

    public static void Lock()
    {
        IsLocked = true;
    }

    public static void Unlock()
    {
        IsLocked = false;
    }
}