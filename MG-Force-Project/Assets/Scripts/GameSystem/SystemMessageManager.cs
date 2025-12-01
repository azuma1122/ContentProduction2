using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Game
{
    /// <summary>
    /// システムメッセージ（画面上部などに一時的に表示される通知）の管理クラス
    /// - 指定されたメッセージを一定時間表示し、自動で非表示にする
    /// - メッセージの上書き表示にも対応
    /// </summary>
    public class SystemMessageManager : MonoBehaviour
    {
        [Header("メッセージ表示用オブジェクト（パネルなど）")]
        [SerializeField] private GameObject _systemMessagePrefab;

        [Header("メッセージテキスト")]
        [SerializeField] private TextMeshProUGUI _messageText;

        /// <summary>
        /// 現在実行中の自動非表示コルーチン
        /// </summary>
        private Coroutine _currentCoroutine = null;

        private void Awake()
        {
            // 開始時は非表示にしておく
            _systemMessagePrefab.SetActive(false);
        }

        /// <summary>
        /// メッセージを画面に表示し、一定時間後に自動で非表示にする
        /// </summary>
        /// <param name="message">表示する文章</param>
        public void DrawMessage(string message)
        {
            // 安全チェック
            if (_systemMessagePrefab == null) return;

            // 表示を一旦強制リセット
            _systemMessagePrefab.SetActive(false);

            // 以前のコルーチンが動いていれば停止（メッセージの上書き対応）
            if (_currentCoroutine != null)
            {
                StopCoroutine(_currentCoroutine);
            }

            // メッセージ表示
            _systemMessagePrefab.SetActive(true);
            _messageText.text = message;

            // 自動消去コルーチンを開始
            _currentCoroutine = StartCoroutine(MessageStart());
        }

        /// <summary>
        /// 一定時間経過後にメッセージを非表示にするコルーチン
        /// </summary>
        private IEnumerator MessageStart()
        {
            // 表示時間（5秒）
            float delay_time = 5.0f;

            // 指定秒数待機
            yield return new WaitForSeconds(delay_time);

            // メッセージを非表示にする
            _systemMessagePrefab.SetActive(false);
        }
    }
}
