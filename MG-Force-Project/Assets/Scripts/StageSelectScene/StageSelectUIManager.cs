using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Game.StageScene
{
    /// <summary>
    /// 惑星ボタンにカーソルが乗ったときに
    /// ハイライトリングと矢印を表示し、
    /// クリックでステージに遷移するクラス
    /// </summary>
    public class StageSelectUIManager : MonoBehaviour
    {
        [Header("UI参照設定")]
        [SerializeField] private Image HighlightRing;   // ハイライト用リング
        [SerializeField] private Image Arrow;           // 指し示す矢印
        [SerializeField] private Button[] PlanetButtons; // 惑星ボタン群

        private void Start()
        {
            // 最初は非表示
            HighlightRing.gameObject.SetActive(false);
            Arrow.gameObject.SetActive(false);

            // 各ボタンにイベントを登録
            foreach (Button btn in PlanetButtons)
            {
                AddHoverEvents(btn);

                // クリックイベント登録
                btn.onClick.AddListener(() => OnClickStage(btn));
            }
        }

        /// <summary>
        /// 各ボタンにマウスホバーイベントを登録
        /// </summary>
        private void AddHoverEvents(Button button)
        {
            EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = button.gameObject.AddComponent<EventTrigger>();

            // マウスが乗った時のイベント
            EventTrigger.Entry enter = new EventTrigger.Entry();
            enter.eventID = EventTriggerType.PointerEnter;
            enter.callback.AddListener((eventData) => OnHoverEnter(button));
            trigger.triggers.Add(enter);

            // マウスが離れた時のイベント
            EventTrigger.Entry exit = new EventTrigger.Entry();
            exit.eventID = EventTriggerType.PointerExit;
            exit.callback.AddListener((eventData) => OnHoverExit());
            trigger.triggers.Add(exit);
        }

        /// <summary>
        /// カーソルがボタンに乗った時の処理
        /// </summary>
        private void OnHoverEnter(Button button)
        {
            HighlightRing.gameObject.SetActive(true);
            Arrow.gameObject.SetActive(true);

            RectTransform planetRect = button.GetComponent<RectTransform>();
            HighlightRing.rectTransform.position = planetRect.position;
            HighlightRing.rectTransform.localScale = Vector3.one * 1.2f;
            Arrow.rectTransform.position = planetRect.position + new Vector3(0, 80f, 0);
        }

        /// <summary>
        /// カーソルがボタンから離れた時の処理
        /// </summary>
        private void OnHoverExit()
        {
            HighlightRing.gameObject.SetActive(false);
            Arrow.gameObject.SetActive(false);
        }

        /// <summary>
        /// ボタンがクリックされたときに呼ばれる
        /// </summary>
        private void OnClickStage(Button button)
        {
            string stageName = button.name;
            //Debug.Log("Clicked: " + stageName);

            // 名前に応じてシーンを切り替え
            if (stageName.Contains("Stage1"))
            {
                SceneManager.LoadScene("Stage1"); // 実際のシーン名に変更
            }
            else if (stageName.Contains("Stage2"))
            {
                SceneManager.LoadScene("Stage2");
            }
            else if (stageName.Contains("Stage3"))
            {
                SceneManager.LoadScene("Stage3");
            }
        }
    }
}
