using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.StageScene
{
    /// <summary>
    /// クリスタルの回転とプレイヤー接触検知を管理するクラス
    /// - 回転処理
    /// - プレイヤー接触検知
    /// - ゴール到達時のシーン遷移（Clear画面）
    /// </summary>
    public class CrystalController : MonoBehaviour
    {
        [Header("回転スピード")]
        [SerializeField] private float _speed = 18.5f;

        [Header("プレイヤー検出タグ")]
        [SerializeField] private string _playerTag = GameConstants.Tag.PLAYER.ToString();

        [Header("遷移先ステージ名")]
        [SerializeField] private string _nextSceneName = "Clear"; // ゴール後に遷移するScene名

        // ゴール到達フラグ
        public bool IsGoalEvent { get; private set; }

        private Vector3 _rotate;

        private void Start()
        {
            IsGoalEvent = false;
        }

        private void Update()
        {
            // クリスタルを回転
            RotateCrystal();
        }

        /// <summary>
        /// クリスタルを回転させる
        /// </summary>
        private void RotateCrystal()
        {
            _rotate = transform.eulerAngles;
            _rotate.y += Time.deltaTime * _speed;
            transform.eulerAngles = _rotate;
        }

        /// <summary>
        /// プレイヤーとの接触検知（Trigger）
        /// OnTriggerStayを使用することで接触中も継続的に判定
        /// </summary>
        private void OnTriggerStay(Collider other)
        {
            if (IsGoalEvent) return; // すでにゴール済みなら無視

            if (other.CompareTag(_playerTag))
            {
                IsGoalEvent = true;
                Debug.Log("ゴールに触れた: " + other.name);

                // Clear画面へ遷移
                LoadNextScene();
                Debug.Log("ゴールに触れた: " + other.name);

                //クリアSEとクリアシーンでのBGMのタイミングは要チェック
                SEManager.instance.PlaySE(SEManager.Stage.STAGE_CLEAR);

                // Clear画面へ遷移
                StartCoroutine(LoadSceneAfterDelay());
            }
        }
        /// <summary>
        /// SEが流れているかを確認してシーン遷移の関数を実行
        /// </summary>
        /// <returns>IEnumerator</returns>
        private IEnumerator LoadSceneAfterDelay()
        {
            //SEが流れている間はシーン遷移させない
            while (SEManager.instance._audioSource != null && SEManager.instance._audioSource.isPlaying)
            {
                yield return null;
            }
            LoadNextScene();
        }

        /// <summary>
        /// シーン遷移処理
        /// </summary>
        private void LoadNextScene()
        {
            if (string.IsNullOrEmpty(_nextSceneName))
            {
                Debug.LogWarning("遷移先のシーン名が設定されていません。");
                return;
            }
            //ステージクリア

            // Build Settings に登録されているシーンをロード
            SceneManager.LoadScene(_nextSceneName);
        }
    }
}