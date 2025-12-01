using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// シーン遷移を管理するシングルトンクラス
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        #region -------- シングルトンの設定 --------

        public static SceneLoader Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject); // 既に存在する場合は破棄
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject); // シーン切り替えでも破棄されない
        }

        #endregion

        // 非同期ロードでの進行度最大値
        private float LOADING_PROGRESS_MAX = 0.9f;

        // ロード進行度（0～1）
        public float progress { get; private set; }

        [SerializeField] private GameObject _brackOut; // ブラックアウト用UIプレハブ

        private bool _canLoading = false; // ロード可能かどうか

        // LoadingSceneを経由する際に次にロードするシーン名を保持
        private string _nextScene;
        public string NextScene => _nextScene;

        /// <summary>
        /// シーンのロード開始
        /// </summary>
        /// <param name="scene">ロードするシーン名</param>
        /// <param name="useLoading">true: LoadingSceneを経由する / false: 即時遷移</param>
        public void LoadScene(string scene, bool useLoading = true)
        {
            if (useLoading)
            {
                _nextScene = scene; // LoadingScene経由用にシーン名を保持
                _canLoading = true;
                StartCoroutine(OnLoading(scene)); // コルーチンでロード開始
            }
            else
            {
                // 即時ロード（LoadingSceneなし）
                SceneManager.LoadScene(scene);
            }
        }

        /// <summary>
        /// LoadingSceneを経由してシーンをロードするコルーチン
        /// </summary>
        private IEnumerator OnLoading(string scene)
        {
            yield return StartCoroutine(BrackOut()); // 画面暗転演出
            yield return StartCoroutine(Loading(scene)); // 非同期ロード
        }

        /// <summary>
        /// ブラックアウト演出コルーチン
        /// </summary>
        private IEnumerator BrackOut()
        {
            GameObject obj = Instantiate(_brackOut);
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                obj.transform.SetParent(canvas.transform, false);
            }

            Image sr = obj.GetComponent<Image>();

            const float TIME_INTERVAL = 0.01f; // 更新間隔
            const float MIN_ALPHA = 0.0f;
            const float MAX_ALPHA = 1.0f;

            float currentAlpha = MIN_ALPHA; // 初期透明度
            float interval = 0.01f; // 1ステップごとの増加値

            // 徐々に不透明化するループ
            while (true)
            {
                if (sr != null)
                    sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, currentAlpha);

                yield return new WaitForSeconds(TIME_INTERVAL);

                currentAlpha += interval;

                if (currentAlpha >= MAX_ALPHA) break; // 最大値になったら終了
            }
        }

        /// <summary>
        /// 非同期ロードのコルーチン
        /// </summary>
        private IEnumerator Loading(string scene)
        {
            float delayTime = 1.0f; // ロード前後の待機時間

            if (_canLoading)
            {
                _canLoading = false;

                // LoadingSceneを表示
                SceneManager.LoadScene(GameConstants.Scene.Loading.ToString());

                yield return new WaitForSeconds(delayTime);

                // 実際のシーンを非同期ロード
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene);
                if (asyncLoad != null)
                {
                    asyncLoad.allowSceneActivation = false; // 自動切り替えを止める
                    progress = 0f;

                    // 進行度が最大値に達するまで待機
                    while (progress < LOADING_PROGRESS_MAX)
                    {
                        progress = Mathf.Clamp01(asyncLoad.progress / LOADING_PROGRESS_MAX);
                        yield return null;
                    }

                    yield return new WaitForSeconds(delayTime);

                    asyncLoad.allowSceneActivation = true; // シーン切り替え
                }
            }
            else
            {
                // _canLoadingがfalseの場合は短い待機後に終了
                yield return new WaitForSeconds(delayTime);
            }
        }
    }
}
