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
        private Transform _playerTransform;

        private void Start()
        {
            IsGoalEvent = false;

            // Start時に一度 Player を検索
            FindPlayer();
        }

        private void Update()
        {
            // クリスタルを回転
            RotateCrystal();

            // Player が null の場合は毎フレーム検索
            if (_playerTransform == null)
            {
                FindPlayer();
            }
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
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (IsGoalEvent) return; // すでにゴール済みなら無視

            if (other.CompareTag(_playerTag))
            {
                IsGoalEvent = true;
                Debug.Log("ゴールに触れた: " + other.name);

                // Clear画面へ遷移
                LoadNextScene();
            }
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

            // Build Settings に登録されているシーンをロード
            SceneManager.LoadScene(_nextSceneName);
        }

        /// <summary>
        /// プレイヤーオブジェクトを探す
        /// </summary>
        private void FindPlayer()
        {
            GameObject player = GameObject.FindWithTag(_playerTag);
            if (player != null)
            {
                _playerTransform = player.transform;
                Debug.Log("Player を検出: " + player.name);
            }
            else
            {
                _playerTransform = null;
                Debug.LogWarning("Player が見つかりません。再生成待ち。");
            }
        }

        /// <summary>
        /// 他スクリプトから直接 Player をセットする用のメソッド
        /// （動的生成後に確実に検出させたい場合に使用）
        /// </summary>
        public void SetPlayer(Transform playerTransform)
        {
            _playerTransform = playerTransform;
            Debug.Log("Player を手動セット: " + playerTransform.name);
        }
    }
}
