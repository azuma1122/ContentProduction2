using UnityEngine;
using Game.GameSystem;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace Game.Stage
{
    /// <summary>
    /// ステージ内の設定メニュー制御
    /// ・操作説明の表示/非表示
    /// ・確認ダイアログの表示/処理
    /// ・ステージリセット/タイトルへ戻る
    /// ・キー設定切替
    /// </summary>
    public class StageConfigMenuController : MonoBehaviour
    {
        // 入力制御（Keyboard/Gamepad）
        private InputHandler _input;

        // シーン切り替え管理
        private SceneLoader _sceneLoader = SceneLoader.Instance;

        [Header("操作説明")]
        [SerializeField] private GameObject _helpMenuObject;      // 操作説明ウィンドウ
        [SerializeField] private TextMeshProUGUI _helpText;        // 説明文表示

        [Header("確認ダイアログ")]
        [SerializeField] private GameObject _confirmDialogObject; // 「はい/いいえ」確認ウィンドウ
        [SerializeField] private TextMeshProUGUI _confirmMessageText;
        [SerializeField] private Button _confirmYesButton;
        [SerializeField] private Button _confirmNoButton;

        [Header("キー設定")]
        [SerializeField] private Toggle _keyToggle;                // キー切り替えトグル

        private bool _isConfirmActive = false; // ダイアログ開閉フラグ
        private bool _isHelpActive = false;    // 操作説明開閉フラグ
        private System.Action _confirmCallback; // 「はい」押下時の処理保持

        private void Awake()
        {
            _input = GameObject.Find(GameConstants.Object.INPUT)?.GetComponent<InputHandler>();

            // 初期状態は非表示
            if (_helpMenuObject != null) _helpMenuObject.SetActive(false);
            if (_confirmDialogObject != null) _confirmDialogObject.SetActive(false);

            // 確認ダイアログの「はい/いいえ」ボタンイベント登録
            if (_confirmYesButton != null) _confirmYesButton.onClick.AddListener(OnConfirmYes);
            if (_confirmNoButton != null) _confirmNoButton.onClick.AddListener(CloseConfirmDialog);
        }

        private void Update()
        {
            if (_input == null) return;

            // ESCキー押下でウィンドウ類を閉じる
            if (_input.IsActionPressed(InputConstants.Action.MENU_BACK))
            {
                // ダイアログ中→閉じる
                if (_isConfirmActive)
                {
                    CloseConfirmDialog();
                    return;
                }
                // 操作説明中→閉じる
                if (_isHelpActive)
                {
                    CloseHelp();
                    return;
                }
            }
        }

        #region--- 機能 ---

        /// <summary>
        /// キー設定のON/OFF切替
        /// </summary>
        public void ToggleKeyConfig()
        {
            if (_keyToggle != null)
            {
                _keyToggle.isOn = !_keyToggle.isOn;
                _input?.GamePadKeyChange();
            }
        }

        /// <summary>
        /// 操作説明画面を開く
        /// </summary>
        public void ShowHelp()
        {
            if (_helpMenuObject == null) return;

            _isHelpActive = true;
            _helpMenuObject.SetActive(true);

            // ゲームをポーズ
            GlobalUIManager.Instance?.Pause();

            // 説明文設定
            if (_helpText != null)
            {
                _helpText.text = @"
【基本操作】
移動: WASD / 方向キー / 左スティック
ジャンプ: スペース / Aボタン
攻撃: 左クリック / Xボタン
ダッシュ: Shift / Bボタン

【メニュー】
メニュー開く: M / Startボタン
決定: Enter / Aボタン
キャンセル: ESC / Bボタン

【その他】
ポーズ: ESC / Startボタン
";
            }
        }

        /// <summary>
        /// 操作説明を閉じる
        /// </summary>
        public void CloseHelp()
        {
            if (_helpMenuObject == null) return;

            _isHelpActive = false;
            _helpMenuObject.SetActive(false);

            // ポーズ解除
            GlobalUIManager.Instance?.Resume();
        }

        /// <summary>
        /// ステージリセット確認を表示
        /// </summary>
        public void ShowResetConfirm()
        {
            ShowConfirmDialog(
                "ステージを最初からやり直しますか?",
                () => ResetStage()
            );
        }

        /// <summary>
        /// タイトルに戻る確認を表示
        /// </summary>
        public void ShowTitleConfirm()
        {
            ShowConfirmDialog(
                "タイトル画面に戻りますか?\n(進行状況は保存されません)",
                () => BackToTitle()
            );
        }

        /// <summary>
        /// 同一ステージを再読み込み
        /// </summary>
        private void ResetStage()
        {
            GlobalUIManager.Instance?.Resume();
            string currentSceneName = SceneManager.GetActiveScene().name;
            _sceneLoader.LoadScene(currentSceneName);
        }

        /// <summary>
        /// タイトル画面へ
        /// </summary>
        private void BackToTitle()
        {
            GlobalUIManager.Instance?.Resume();
            _sceneLoader.LoadScene(GameConstants.Scene.Title.ToString());
        }

        #endregion

        #region--- 確認ダイアログ処理 ---

        /// <summary>
        /// 確認メッセージ表示（onConfirmに処理を渡す）
        /// </summary>
        private void ShowConfirmDialog(string message, System.Action onConfirm)
        {
            if (_confirmDialogObject == null) return;

            _isConfirmActive = true;
            _confirmDialogObject.SetActive(true);

            // ポーズ
            GlobalUIManager.Instance?.Pause();

            // メッセージ更新
            if (_confirmMessageText != null)
                _confirmMessageText.text = message;

            // 「はい」押下時に実行する処理を保持
            _confirmCallback = onConfirm;
        }

        /// <summary>
        /// 「はい」押された時の処理 → 登録された処理を実行
        /// </summary>
        private void OnConfirmYes()
        {
            _confirmCallback?.Invoke();
            CloseConfirmDialog();
        }

        /// <summary>
        /// ダイアログ閉じる
        /// </summary>
        private void CloseConfirmDialog()
        {
            if (_confirmDialogObject == null) return;

            _isConfirmActive = false;
            _confirmDialogObject.SetActive(false);
            _confirmCallback = null;
        }

        #endregion
    }
}
