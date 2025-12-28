using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.StageScene.Player;

namespace Game.StageScene
{
    /// <summary>
    /// クリスタルの回転とプレイヤー接触検知を管理するクラス
    /// - 回転処理
    /// - プレイヤー接触検知
    /// - ゴール演出 → SE再生 → Clearシーン遷移
    /// </summary>
    public class CrystalController : MonoBehaviour
    {
        [Header("回転スピード")]
        [SerializeField] private float _speed = 18.5f;

        [Header("プレイヤー検出タグ")]
        [SerializeField] private string _playerTag = GameConstants.Tag.PLAYER.ToString();

        [Header("遷移先ステージ名")]
        [SerializeField] private string _nextSceneName = "Clear";

        /// <summary>
        /// ゴール到達フラグ（二重判定防止）
        /// </summary>
        public bool IsGoalEvent { get; private set; }

        private Vector3 _rotate;

        private void Start()
        {
            IsGoalEvent = false;
        }

        private void Update()
        {
            RotateCrystal();
        }

        /// <summary>
        /// クリスタル回転処理
        /// </summary>
        private void RotateCrystal()
        {
            _rotate = transform.eulerAngles;
            _rotate.y += Time.deltaTime * _speed;
            transform.eulerAngles = _rotate;
        }

        /// <summary>
        /// プレイヤー接触判定
        /// </summary>
        private void OnTriggerStay(Collider other)
        {
            if (IsGoalEvent) return;

            if (!other.CompareTag(_playerTag)) return;

            IsGoalEvent = true;
            Debug.Log("ゴールに触れた: " + other.name);

            // ===== プレイヤーにゴール通知 =====
            PlayerControllerBase player =
                other.GetComponent<PlayerControllerBase>();

            if (player != null)
            {
                player.SetGoal(); // ゴールアニメーション開始
            }

            // ===== クリアSE再生 =====
            SEManager.instance.PlaySE(SEManager.Stage.STAGE_CLEAR);

            // ===== SE終了後にシーン遷移 =====
            StartCoroutine(LoadSceneAfterDelay());
        }

        /// <summary>
        /// SE再生終了を待ってからシーン遷移
        /// </summary>
        private IEnumerator LoadSceneAfterDelay()
        {
            // SE再生中は待機
            while (SEManager.instance != null &&
                   SEManager.instance._audioSource != null &&
                   SEManager.instance._audioSource.isPlaying)
            {
                yield return null;
            }

            LoadNextScene();
        }

        /// <summary>
        /// Clearシーン遷移
        /// </summary>
        private void LoadNextScene()
        {
            if (string.IsNullOrEmpty(_nextSceneName))
            {
                Debug.LogWarning("遷移先のシーン名が設定されていません。");
                return;
            }

            SceneManager.LoadScene(_nextSceneName);
        }
    }
}
