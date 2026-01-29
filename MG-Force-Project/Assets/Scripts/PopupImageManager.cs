using Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PopupImageManager : MonoBehaviour
{
    [SerializeField] private GameObject canvasPrefab;
    [SerializeField] private GameObject imagePrefab;
    [SerializeField,Header("操作説明")] private Sprite[] controlGuideImages;   // 操作説明（3枚）
    [SerializeField,Header("ルール説明、ステージ１初期開始のみ表示")] private Sprite[] ruleTutorialImages;   // ルール説明（4枚）

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
                //チュートリアル画像表示
                ShowRuleTutorial();
                hasShownControlGuide = true;
            }
        }
    }
    public void ShowControlGuide()
    {
        StartControl(controlGuideImages);
    }

    public void ShowRuleTutorial()
    {
        StartTutorial(ruleTutorialImages);
    }

    public static void ResetTutorialFlag()
    {
        hasShownControlGuide = false;
    }


    private void StartTutorial(Sprite[] images)
    {
        GameInputLock.Lock();

        currentImages = images;
        currentImageIndex = 0;
        SpawnCanvasWithImage(currentImages[currentImageIndex]);
    }
    private void StartControl(Sprite[] images)
    {

        currentImages = images;
        currentImageIndex = 0;
        SpawnCanvasWithImage(currentImages[currentImageIndex]);
    }

    public void SpawnCanvasWithImage(Sprite sprite)
    {
        if (canvasObject != null)
        {
            Destroy(canvasObject);
        }

        canvasObject = Instantiate(canvasPrefab);

        canvasView = canvasObject.GetComponent<PopupCanvasView>();
        if (canvasView == null)
        {
            Debug.LogError("PopupCanvasView が CanvasPrefab に付いていません");
            return;
        }

        // 画像生成（レイヤー固定）
        imageObject = Instantiate(imagePrefab, canvasView.backgroundRoot);

        Image image = imageObject.GetComponentInChildren<Image>();
        if (image != null)
        {
            image.sprite = sprite;
        }

     

        // ボタン設定
        Button nextButton =
            canvasView.controlRoot.Find("NextButton")?.GetComponent<Button>();

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(ShowNextImage);
        }

        Button prevButton =
            canvasView.controlRoot.Find("ChangeImage_Return")?.GetComponent<Button>();

        if (prevButton != null)
        {
            prevButton.onClick.RemoveAllListeners();
            prevButton.onClick.AddListener(ShowPreviousImage);
        }

        Button destoryButton = canvasView.controlRoot.Find("DestoryButton")?.GetComponent<Button>();
        if (destoryButton != null)
        {
            {
                destoryButton.onClick.RemoveAllListeners();
                destoryButton.onClick.AddListener(DestroyCanvasWithImage);
            }
        }
    }

    public void ShowNextImage()
    {
        if (currentImages == null) return;

        if (currentImageIndex < currentImages.Length - 1)
        {
            currentImageIndex++;
            ChangeImage(currentImages[currentImageIndex]);
        }
    }

    public void ShowPreviousImage()
    {
        if (currentImages == null) return;

        if (currentImageIndex > 0)
        {
            currentImageIndex--;
            ChangeImage(currentImages[currentImageIndex]);
        }
    }

    public void ChangeImage(Sprite sprite)
    {
        if (imageObject == null) return;

        Image image = imageObject.GetComponentInChildren<Image>();
        if (image != null)
        {
            image.sprite = sprite;
        }
    }

    public void DestroyCanvasWithImage()
    {
        GameInputLock.Unlock();

        if (canvasObject != null)
        {
            Destroy(canvasObject);
        }
    }



}
public static class GameInputLock
{
    public static bool IsLocked { get; private set; }

    public static void Lock() => IsLocked = true;
    public static void Unlock() => IsLocked = false;
}
