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
    /// - ゴール演出 → 固定時間待機 → SE再生 → Clearシーン遷移
    /// </summary>
    public class CrystalController : MonoBehaviour
    {
        [Header("回転スピード")]
        [SerializeField] private float _speed = 18.5f;

        [Header("プレイヤー検出タグ")]
        [SerializeField] private string _playerTag = GameConstants.Tag.PLAYER.ToString();

        [Header("遷移先ステージ名")]
        [SerializeField] private string _nextSceneName = "Clear";

        [Header("ゴールアニメーション待機時間（秒）")]
        [Tooltip("ゴールアニメーション再生時間（アニメーションの長さに合わせて調整）")]
        [SerializeField] private float _goalAnimationDuration = 2.5f;

        [Header("クリスタル非表示設定")]
        [Tooltip("ゴール時にクリスタルを非表示にするか")]
        [SerializeField] private bool _hideCrystalOnGoal = true;

        [Tooltip("クリスタルを非表示にするタイミング（秒）")]
        [SerializeField] private float _hideCrystalDelay = 0f;

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
            if (!IsGoalEvent) // ゴール後は回転を停止
            {
                RotateCrystal();
            }
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
            Debug.Log("[Crystal] ゴールに触れた: " + other.name);

            // ===== プレイヤーにゴール通知 =====
            PlayerControllerBase player = other.GetComponent<PlayerControllerBase>();
            if (player != null)
            {
                player.SetGoal(); // ゴールアニメーション開始（PlayerAnimationControllerが処理）
                Debug.Log("[Crystal] プレイヤーにゴール通知完了");
            }
            else
            {
                Debug.LogError("[Crystal] PlayerControllerBase が見つかりません");
            }

            // ===== クリスタル非表示処理 =====
            if (_hideCrystalOnGoal)
            {
                StartCoroutine(HideCrystalRoutine());
            }

            // ===== ゴール演出シーケンス開始 =====
            StartCoroutine(GoalSequence());
        }

        /// <summary>
        /// クリスタルを非表示にする
        /// </summary>
        private IEnumerator HideCrystalRoutine()
        {
            // 指定時間待機してから非表示
            if (_hideCrystalDelay > 0f)
            {
                Debug.Log($"[Crystal] クリスタル非表示まで {_hideCrystalDelay}秒待機");
                yield return new WaitForSeconds(_hideCrystalDelay);
            }

            // MeshRendererを非表示にする（Colliderは残す）
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
                Debug.Log("[Crystal] クリスタルを非表示");
            }

            // 子オブジェクトも含めて完全に非表示にする場合
            MeshRenderer[] childRenderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in childRenderers)
            {
                renderer.enabled = false;
            }

            // SkinnedMeshRendererがある場合も対応
            SkinnedMeshRenderer[] skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in skinnedRenderers)
            {
                renderer.enabled = false;
            }

            Debug.Log("[Crystal] クリスタルを完全に非表示にしました");
        }

        /// <summary>
        /// ゴール演出シーケンス
        /// 1. ゴールアニメーション再生時間だけ待機
        /// 2. クリアSE再生
        /// 3. SE終了を待つ
        /// 4. Clearシーン遷移
        /// </summary>
        private IEnumerator GoalSequence()
        {
            Debug.Log("[Crystal] ===== ゴールシーケンス開始 =====");

            // ===== 1. ゴールアニメーション再生完了を待つ =====
            Debug.Log($"[Crystal] ゴールアニメーション待機中... ({_goalAnimationDuration}秒)");
            yield return new WaitForSeconds(_goalAnimationDuration);
            Debug.Log("[Crystal] ゴールアニメーション待機完了");

            // ===== 2. クリアSE再生 =====
            SEManager.instance.PlaySE(SEManager.Stage.STAGE_CLEAR);
            Debug.Log("[Crystal]  クリアSE再生開始");

            // ===== 3. SE終了を待つ =====
            yield return StartCoroutine(WaitForSEComplete());

            // ===== 4. Clearシーン遷移 =====
            LoadNextScene();
        }

        /// <summary>
        /// SE再生終了を待つ
        /// </summary>
        private IEnumerator WaitForSEComplete()
        {
            while (SEManager.instance != null &&
                   SEManager.instance._audioSource != null &&
                   SEManager.instance._audioSource.isPlaying)
            {
                yield return null;
            }

            Debug.Log("[Crystal] クリアSE再生完了");
        }

        /// <summary>
        /// Clearシーン遷移
        /// </summary>
        private void LoadNextScene()
        {
            if (string.IsNullOrEmpty(_nextSceneName))
            {
                Debug.LogWarning("[Crystal] 遷移先のシーン名が設定されていません");
                return;
            }

            Debug.Log($"[Crystal]  {_nextSceneName}シーンへ遷移");
            SceneManager.LoadScene(_nextSceneName);
        }
    }
}