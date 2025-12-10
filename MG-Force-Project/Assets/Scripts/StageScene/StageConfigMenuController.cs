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
        [SerializeField] private GameObject _helpMenuObject;
        [SerializeField] private TextMeshProUGUI _helpText;

        [Header("確認ダイアログ")]
        [SerializeField] private GameObject _confirmDialogObject;
        [SerializeField] private TextMeshProUGUI _confirmMessageText;
        [SerializeField] private Button _confirmYesButton;
        [SerializeField] private Button _confirmNoButton;

        [Header("キー設定")]
        [SerializeField] private Toggle _keyToggle;

        private bool _isConfirmActive = false;
        private bool _isHelpActive = false;
        private System.Action _confirmCallback;

        private void Awake()
        {
            _input = GameObject.Find(GameConstants.Object.INPUT)?.GetComponent<InputHandler>();

            // ★★★ デバッグ：InputHandler の状態確認 ★★★
            if (_input == null)
            {
                Debug.LogError($"[StageConfigMenu] InputHandler が見つかりません！ シーン: {SceneManager.GetActiveScene().name}");
            }
            else
            {
                Debug.Log($"[StageConfigMenu] InputHandler 取得成功: {_input.gameObject.name}");
            }

            // 初期状態は非表示
            if (_helpMenuObject != null) _helpMenuObject.SetActive(false);
            if (_confirmDialogObject != null) _confirmDialogObject.SetActive(false);

            // 確認ダイアログの「はい/いいえ」ボタンイベント登録
            if (_confirmYesButton != null) _confirmYesButton.onClick.AddListener(OnConfirmYes);
            if (_confirmNoButton != null) _confirmNoButton.onClick.AddListener(CloseConfirmDialog);

            // ★★★ Awake時に自動診断 ★★★
            Invoke("AutoDiagnoseOnStart", 1f);
        }

        private void Update()
        {
            // ★★★ Pキーは Input.GetKeyDown で直接チェック（InputHandler不要） ★★★
            if (Input.GetKeyDown(KeyCode.P))
            {
                DebugTimeScale();
            }

            // ★★★ Iキーで詳細診断（InputHandler不要） ★★★
            if (Input.GetKeyDown(KeyCode.I))
            {
                DiagnoseScene();
            }

            // ★★★ Oキーでボタン診断（InputHandler不要） ★★★
            if (Input.GetKeyDown(KeyCode.O))
            {
                DiagnoseButtons();
            }

            if (_input == null)
            {
                // ★★★ InputHandlerがnullの場合、毎フレーム再取得を試みる ★★★
                _input = GameObject.Find(GameConstants.Object.INPUT)?.GetComponent<InputHandler>();
                return;
            }

            // ESCキー押下でウィンドウ類を閉じる
            if (_input.IsActionPressed(InputConstants.Action.MENU_BACK))
            {
                if (_isConfirmActive)
                {
                    CloseConfirmDialog();
                    return;
                }
                if (_isHelpActive)
                {
                    CloseHelp();
                    return;
                }
            }
        }

        #region--- デバッグ機能 ---

        /// <summary>
        /// 起動時の自動診断
        /// </summary>
        private void AutoDiagnoseOnStart()
        {
            Debug.Log("========================================");
            Debug.Log($"=== {SceneManager.GetActiveScene().name} 自動診断 ===");
            Debug.Log("========================================");

            DiagnoseScene();
            DiagnoseButtons();

            Debug.Log("========================================");
            Debug.Log("=== 診断完了 ===");
            Debug.Log("Pキー: タイムスケール診断");
            Debug.Log("Iキー: シーン診断");
            Debug.Log("Oキー: ボタン診断");
            Debug.Log("========================================");
        }

        /// <summary>
        /// シーン全体の診断
        /// </summary>
        private void DiagnoseScene()
        {
            Debug.Log("========== シーン診断 ==========");
            Debug.Log($"シーン名: {SceneManager.GetActiveScene().name}");
            Debug.Log($"Time.timeScale: {Time.timeScale}");
            Debug.Log($"Physics.simulationMode: {Physics.simulationMode}");

            // InputHandler確認
            var inputObj = GameObject.Find(GameConstants.Object.INPUT);
            if (inputObj != null)
            {
                Debug.Log($"InputObject発見: {inputObj.name}, Active: {inputObj.activeSelf}");
                var handler = inputObj.GetComponent<InputHandler>();
                Debug.Log($"  InputHandler: {(handler != null ? "あり" : "なし")}");
                if (handler != null)
                {
                    Debug.Log($"  InputHandler.enabled: {handler.enabled}");
                }
            }
            else
            {
                Debug.LogError($"InputObject が見つかりません！探索名: {GameConstants.Object.INPUT}");
            }

            // EventSystem確認
            var eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem != null)
            {
                Debug.Log($"EventSystem: {eventSystem.gameObject.name}, enabled: {eventSystem.enabled}");
                var inputModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (inputModule != null)
                {
                    Debug.Log($"  StandaloneInputModule: enabled={inputModule.enabled}");
                }
            }
            else
            {
                Debug.LogError("★★★ EventSystem が見つかりません！これが原因の可能性大 ★★★");
            }

            // Canvas確認
            var canvases = FindObjectsOfType<Canvas>();
            Debug.Log($"Canvas数: {canvases.Length}");
            foreach (var canvas in canvases)
            {
                var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                Debug.Log($"  Canvas: {canvas.name}");
                Debug.Log($"    enabled: {canvas.enabled}");
                Debug.Log($"    renderMode: {canvas.renderMode}");
                Debug.Log($"    Raycaster: {(raycaster != null && raycaster.enabled ? "有効" : "無効")}");
            }

            Debug.Log("================================");
        }

        /// <summary>
        /// ボタンの詳細診断
        /// </summary>
        private void DiagnoseButtons()
        {
            Debug.Log("========== ボタン診断 ==========");

            // UI Button確認
            var allButtons = FindObjectsOfType<Button>();
            Debug.Log($"UI Button数: {allButtons.Length}");
            foreach (var btn in allButtons)
            {
                Debug.Log($"  Button: {btn.gameObject.name}");
                Debug.Log($"    enabled: {btn.enabled}");
                Debug.Log($"    interactable: {btn.interactable}");
                Debug.Log($"    GameObject.active: {btn.gameObject.activeSelf}");
                Debug.Log($"    親がアクティブ: {btn.transform.parent == null || btn.transform.parent.gameObject.activeSelf}");

                // Canvas確認
                var canvas = btn.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    Debug.Log($"    所属Canvas: {canvas.name}, enabled: {canvas.enabled}");
                }
            }

            // Toggle確認
            var allToggles = FindObjectsOfType<Toggle>();
            Debug.Log($"Toggle数: {allToggles.Length}");
            foreach (var toggle in allToggles)
            {
                Debug.Log($"  Toggle: {toggle.gameObject.name}");
                Debug.Log($"    enabled: {toggle.enabled}");
                Debug.Log($"    interactable: {toggle.interactable}");
            }

            // 3Dボタン（Collider付き）確認
            var allColliders = FindObjectsOfType<Collider>();
            int buttonColliderCount = 0;
            foreach (var col in allColliders)
            {
                if (col.gameObject.name.ToLower().Contains("button") ||
                    col.gameObject.tag == "Button")
                {
                    buttonColliderCount++;
                    Debug.Log($"  3Dボタン: {col.gameObject.name}");
                    Debug.Log($"    Position: {col.transform.position}");
                    Debug.Log($"    enabled: {col.enabled}");
                    Debug.Log($"    isTrigger: {col.isTrigger}");
                }
            }
            Debug.Log($"3Dボタン（Collider）数: {buttonColliderCount}");

            Debug.Log("================================");
        }

        /// <summary>
        /// 現在のTime.timeScaleとポーズ状態を詳細にログ出力
        /// </summary>
        private void DebugTimeScale()
        {
            Debug.Log("========== タイムスケール診断 ==========");
            Debug.Log($"Time.timeScale: {Time.timeScale}");
            Debug.Log($"Time.deltaTime: {Time.deltaTime}");
            Debug.Log($"Time.unscaledDeltaTime: {Time.unscaledDeltaTime}");
            Debug.Log($"Physics.simulationMode: {Physics.simulationMode}");

            // GlobalUIManagerの状態確認
            if (GlobalUIManager.Instance != null)
            {
                Debug.Log($"GlobalUIManager.IsPaused: {GlobalUIManager.Instance.IsPaused}");
                Debug.Log($"GlobalUIManager GameObject: {GlobalUIManager.Instance.gameObject.name}");
            }
            else
            {
                Debug.LogWarning("GlobalUIManager.Instance が null です！");
            }

            // シーン内のすべてのGlobalUIManagerを検索
            var allManagers = FindObjectsOfType<GlobalUIManager>();
            Debug.Log($"シーン内のGlobalUIManager数: {allManagers.Length}");
            foreach (var manager in allManagers)
            {
                Debug.Log($"  - {manager.gameObject.name}, IsPaused: {manager.IsPaused}");
            }

            Debug.Log("======================================");
        }

        #endregion

        #region--- 機能 ---

        public void ToggleKeyConfig()
        {
            if (_keyToggle != null)
            {
                _keyToggle.isOn = !_keyToggle.isOn;
                _input?.GamePadKeyChange();
            }
        }

        public void ShowHelp()
        {
            if (_helpMenuObject == null) return;

            _isHelpActive = true;
            _helpMenuObject.SetActive(true);

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

        public void CloseHelp()
        {
            if (_helpMenuObject == null) return;
            _isHelpActive = false;
            _helpMenuObject.SetActive(false);
        }

        public void ShowResetConfirm()
        {
            ShowConfirmDialog(
                "ステージを最初からやり直しますか?",
                () => ResetStage()
            );
        }

        public void ShowTitleConfirm()
        {
            ShowConfirmDialog(
                "タイトル画面に戻りますか?\n(進行状況は保存されません)",
                () => BackToTitle()
            );
        }

        public void BackToTitleDirect()
        {
            BackToTitle();
        }

        private void ResetStage()
        {
            Debug.Log("=== ステージリセット開始 ===");
            DebugTimeScale();

            if (_confirmDialogObject != null)
                _confirmDialogObject.SetActive(false);
            if (_helpMenuObject != null)
                _helpMenuObject.SetActive(false);

            if (GlobalUIManager.Instance != null)
            {
                var configMenu = GameObject.Find("ConfigMenu");
                if (configMenu != null)
                {
                    Debug.Log("設定メニューを強制的に閉じます");
                    configMenu.SetActive(false);
                }
            }

            ForceResumeAll();
            Debug.Log($"リセット後のTime.timeScale: {Time.timeScale}");

            string currentSceneName = SceneManager.GetActiveScene().name;
            Debug.Log($"シーン再読み込み: {currentSceneName}");

            _sceneLoader.LoadScene(currentSceneName);
        }

        private void BackToTitle()
        {
            Debug.Log("=== タイトルへ戻る ===");
            DebugTimeScale();

            if (_confirmDialogObject != null)
                _confirmDialogObject.SetActive(false);
            if (_helpMenuObject != null)
                _helpMenuObject.SetActive(false);

            ForceResumeAll();
            Debug.Log($"遷移直前のTime.timeScale: {Time.timeScale}");

            _sceneLoader.LoadScene(GameConstants.Scene.Title.ToString());
        }

        private void ForceResumeAll()
        {
            Debug.Log("--- ポーズ状態の強制解除開始 ---");

            Time.timeScale = 1f;
            Debug.Log($"Time.timeScale を 1f に設定: {Time.timeScale}");

            var allManagers = FindObjectsOfType<GlobalUIManager>();
            Debug.Log($"検出されたGlobalUIManager数: {allManagers.Length}");

            if (allManagers.Length > 1)
            {
                Debug.LogWarning("★★★ 複数のGlobalUIManagerが検出されました。余分なインスタンスを破棄します ★★★");

                for (int i = 1; i < allManagers.Length; i++)
                {
                    Debug.LogWarning($"  → {allManagers[i].gameObject.name} を破棄します");
                    Destroy(allManagers[i].gameObject);
                }
            }

            foreach (var manager in allManagers)
            {
                if (manager != null && manager.gameObject != null)
                {
                    Debug.Log($"  - {manager.gameObject.name} をリセット中...");
                    manager.Resume();
                }
            }

            if (GlobalUIManager.Instance != null)
            {
                Debug.Log("GlobalUIManager.Instance を明示的にリセット");
                GlobalUIManager.Instance.Resume();
            }

            Time.timeScale = 1f;
            Debug.Log($"--- 強制解除完了: Time.timeScale = {Time.timeScale} ---");
        }

        #endregion

        #region--- 確認ダイアログ処理 ---

        private void ShowConfirmDialog(string message, System.Action onConfirm)
        {
            if (_confirmDialogObject == null) return;

            _isConfirmActive = true;
            _confirmDialogObject.SetActive(true);

            if (_confirmMessageText != null)
                _confirmMessageText.text = message;

            _confirmCallback = onConfirm;
        }

        private void OnConfirmYes()
        {
            _confirmCallback?.Invoke();
            CloseConfirmDialog();
            //リトライ時のSE
            SEManager.instance.PlaySE(SEManager.Stage.STAGE_RETRY); 
        }

        private void CloseConfirmDialog()
        {
            if (_confirmDialogObject == null) return;

            _isConfirmActive = false;
            _confirmDialogObject.SetActive(false);
            _confirmCallback = null;
            SEManager.instance.PlaySE(SEManager.Menu.CANCEL);

        }

        #endregion
    }
}