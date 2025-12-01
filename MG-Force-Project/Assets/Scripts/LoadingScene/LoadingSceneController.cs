using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.LoadingScene
{
    /// <summary>
    /// LoadingSceneの進行度表示を管理するクラス
    /// </summary>
    public class LoadingSceneController : MonoBehaviour
    {
        private SceneLoader _sceneLoader;

        [SerializeField] private Image _progressGage; // プログレスバーのImage

        private void Awake()
        {
            _sceneLoader = SceneLoader.Instance;

            // SceneLoaderが存在しない場合はTitleに戻す
            if (_sceneLoader == null)
                SceneManager.LoadScene(GameConstants.Scene.Title.ToString());
        }

        private void Update()
        {
            if (_sceneLoader != null && _progressGage != null)
            {
                // プログレスバーを更新
                _progressGage.fillAmount = _sceneLoader.progress;
            }
        }
    }
}
