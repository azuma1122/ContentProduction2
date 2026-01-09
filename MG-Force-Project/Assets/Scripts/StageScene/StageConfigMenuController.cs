using UnityEngine;
using Game.GameSystem;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

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
        // 入力制御(Keyboard/Gamepad)
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

            // 初期状態は非表示(nullチェック付き)
            if (_helpMenuObject != null)
            {
                _helpMenuObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[StageConfigMenu] _helpMenuObject が Inspector で設定されていません");
            }

            if (_confirmDialogObject != null)
            {
                _confirmDialogObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[StageConfigMenu] _confirmDialogObject が Inspector で設定されていません");
            }

            // 確認ダイアログの「はい/いいえ」ボタンイベント登録
            if (_confirmYesButton != null)
            {
                _confirmYesButton.onClick.AddListener(OnConfirmYes);
            }
            else
            {
                Debug.LogWarning("[StageConfigMenu] _confirmYesButton が Inspector で設定されていません");
            }

            if (_confirmNoButton != null)
            {
                _confirmNoButton.onClick.AddListener(CloseConfirmDialog);
            }
            else
            {
                Debug.LogWarning("[StageConfigMenu] _confirmNoButton が Inspector で設定されていません");
            }
        }

        private void Update()
        {

            if (_input == null)
            {
                // InputHandlerがnullの場合、毎フレーム再取得を試みる
                _input = GameObject.Find(GameConstants.Object.INPUT)?.GetComponent<InputHandler>();
                if (_input == null)
                {
                    Debug.LogWarning("[StageConfigMenu] InputHandlerが見つかりません。再取得を試みます...");
                }
                return;
            }

            // ESCキー押下時の処理（直接キー入力も追加）
            if (_input.IsActionPressed(InputConstants.Action.MENU_BACK) || Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("[StageConfigMenu] ESCキー検出");

                // ヘルプが開いている場合は閉じる
                if (_isHelpActive)
                {
                    Debug.Log("[StageConfigMenu] ヘルプを閉じます");
                    CloseHelp();
                    return;
                }
                // ヘルプが開いていない場合は即時リスタート
                Debug.Log("[StageConfigMenu] ESCキー押下 - ステージを即時リスタートします");
                ResetStage();
                return;
            }

            // 詳細デバッグ(Oキー)
            if (Input.GetKeyDown(KeyCode.O))
            {
                DetailedDebug();
            }

            // プレイヤー状態チェック(Lキー)
            if (Input.GetKeyDown(KeyCode.L))
            {
                CheckPlayerStatus();
            }
        }

        /// <summary>
        /// 詳細なデバッグ情報を出力
        /// </summary>
        private void DetailedDebug()
        {
            Debug.Log("========== 詳細デバッグ情報 ==========");
            Debug.Log($"Time.timeScale: {Time.timeScale}");
            Debug.Log($"Time.deltaTime: {Time.deltaTime}");
            Debug.Log($"Physics.simulationMode: {Physics.simulationMode}");
            Debug.Log($"Physics2D.simulationMode: {Physics2D.simulationMode}");

            // プレイヤー検索
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Debug.Log($"プレイヤー発見: {player.name}");
                Debug.Log($"  - Active: {player.activeSelf}");
                Debug.Log($"  - Position: {player.transform.position}");

                var rb = player.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Debug.Log($"  - Rigidbody.isKinematic: {rb.isKinematic}");
                    Debug.Log($"  - Rigidbody.velocity: {rb.velocity}");
                    Debug.Log($"  - Rigidbody.useGravity: {rb.useGravity}");
                }

                var rb2d = player.GetComponent<Rigidbody2D>();
                if (rb2d != null)
                {
                    Debug.Log($"  - Rigidbody2D.isKinematic: {rb2d.isKinematic}");
                    Debug.Log($"  - Rigidbody2D.velocity: {rb2d.velocity}");
                    Debug.Log($"  - Rigidbody2D.simulated: {rb2d.simulated}");
                }

                var scripts = player.GetComponents<MonoBehaviour>();
                Debug.Log($"  - MonoBehaviour数: {scripts.Length}");
                foreach (var script in scripts)
                {
                    Debug.Log($"    - {script.GetType().Name}: enabled={script.enabled}");
                }
            }
            else
            {
                Debug.LogError("★★★ プレイヤーが見つかりません! ★★★");
            }

            // InputHandler確認
            var inputObj = GameObject.Find(GameConstants.Object.INPUT);
            if (inputObj != null)
            {
                Debug.Log($"InputHandler GameObject: {inputObj.name}, Active: {inputObj.activeSelf}");
                var handler = inputObj.GetComponent<InputHandler>();
                if (handler != null)
                {
                    Debug.Log($"  - InputHandler.enabled: {handler.enabled}");
                }
            }
            else
            {
                Debug.LogError("★★★ InputHandlerが見つかりません! ★★★");
            }

            Debug.Log("====================================");
        }

        /// <summary>
        /// プレイヤーの詳細状態チェック(Lキーで呼び出し)
        /// </summary>
        private void CheckPlayerStatus()
        {
            Debug.Log("========== プレイヤー状態チェック (Lキー) ==========");

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("★★★ プレイヤーが見つかりません！ ★★★");

                // Playerタグが付いているオブジェクトを全て検索
                var allPlayers = GameObject.FindGameObjectsWithTag("Player");
                Debug.Log($"「Player」タグのオブジェクト数: {allPlayers.Length}");

                return;
            }

            // 基本情報
            Debug.Log($"[基本] GameObject.name: {player.name}");
            Debug.Log($"[基本] activeSelf: {player.activeSelf}");
            Debug.Log($"[基本] activeInHierarchy: {player.activeInHierarchy}");
            Debug.Log($"[基本] Position: {player.transform.position}");

            // 子オブジェクトの情報
            Debug.Log($"[階層] 子オブジェクト数: {player.transform.childCount}");
            for (int i = 0; i < player.transform.childCount; i++)
            {
                var child = player.transform.GetChild(i);
                Debug.Log($"[階層]   [{i}] {child.name}, Active: {child.gameObject.activeSelf}");
            }

            // Time設定
            Debug.Log($"[Time] Time.timeScale: {Time.timeScale}");
            Debug.Log($"[Time] Time.deltaTime: {Time.deltaTime}");

            // 物理設定
            Debug.Log($"[Physics] simulationMode: {Physics.simulationMode}");
            Debug.Log($"[Physics2D] simulationMode: {Physics2D.simulationMode}");

            // Rigidbody(子を含めて検索)
            var rb = player.GetComponentInChildren<Rigidbody>();
            if (rb != null)
            {
                Debug.Log($"[Rigidbody] 発見: {rb.gameObject.name}");
                Debug.Log($"[Rigidbody] isKinematic: {rb.isKinematic}");
                Debug.Log($"[Rigidbody] velocity: {rb.velocity}");
                Debug.Log($"[Rigidbody] useGravity: {rb.useGravity}");
                Debug.Log($"[Rigidbody] mass: {rb.mass}");
                Debug.Log($"[Rigidbody] drag: {rb.drag}");
                Debug.Log($"[Rigidbody] angularDrag: {rb.angularDrag}");
                Debug.Log($"[Rigidbody] constraints: {rb.constraints}");
                Debug.Log($"[Rigidbody] freezeRotation: {rb.freezeRotation}");
            }
            else
            {
                Debug.LogWarning("[Rigidbody] ★ コンポーネントなし - 3Dキャラクターの場合は必須です");
            }

            // 全スクリプト(子を含めて検索)
            var scripts = player.GetComponentsInChildren<MonoBehaviour>();
            Debug.Log($"[Scripts] 総数（子を含む）: {scripts.Length}");
            foreach (var script in scripts)
            {
                if (script != null)
                {
                    Debug.Log($"[Scripts]   {script.GetType().Name} ({script.gameObject.name}): enabled={script.enabled}");
                }
            }

            // InputHandler確認
            var inputObj = GameObject.Find(GameConstants.Object.INPUT);
            if (inputObj != null)
            {
                Debug.Log($"[Input] GameObject発見: {inputObj.name}, Active: {inputObj.activeSelf}");
                var handler = inputObj.GetComponent<InputHandler>();
                if (handler != null)
                {
                    Debug.Log($"[Input] InputHandler.enabled: {handler.enabled}");
                }
                else
                {
                    Debug.LogError("[Input] ★★★ InputHandlerコンポーネントがありません！");
                }
            }
            else
            {
                Debug.LogError("[Input] ★★★ InputHandlerオブジェクトが見つかりません！");
            }

            // GlobalUIManager確認
            var managers = FindObjectsOfType<GlobalUIManager>();
            Debug.Log($"[Manager] GlobalUIManager数: {managers.Length}");
            if (managers.Length > 1)
            {
                Debug.LogWarning("[Manager] ★★★ GlobalUIManagerが複数存在します！");
                for (int i = 0; i < managers.Length; i++)
                {
                    Debug.Log($"[Manager]   [{i}] {managers[i].gameObject.name}, IsPaused: {managers[i].IsPaused}");
                }
            }
            else if (managers.Length == 1)
            {
                Debug.Log($"[Manager] IsPaused: {managers[0].IsPaused}");
            }

            Debug.Log("====================================================");
        }

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

        /// <summary>
        /// リスタートボタンから直接呼び出し用
        /// 確認ダイアログなしで即座にステージをリセット
        /// </summary>
        public void RestartStageDirect()
        {
            Debug.Log("[StageConfigMenu] リスタートボタン押下 - ステージをリセットします");
            ResetStage();
        }

        /// <summary>
        /// ステージをリスタート
        /// </summary>
        private void ResetStage()
        {
            // ダイアログとヘルプを閉じる
            if (_confirmDialogObject != null)
                _confirmDialogObject.SetActive(false);
            if (_helpMenuObject != null)
                _helpMenuObject.SetActive(false);

            // ConfigMenuを閉じる
            if (GlobalUIManager.Instance != null)
            {
                var configMenu = GameObject.Find("ConfigMenu");
                if (configMenu != null)
                {
                    configMenu.SetActive(false);
                }
            }

            // 状態をリセット
            ForceResumeAll();

            // SE再生
            try
            {
                if (SEManager.instance != null)
                {
                    SEManager.instance.PlaySE(SEManager.Stage.STAGE_RETRY);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"SE再生エラー: {e.Message}");
            }

            // ロードシーンを経由してリロード
            StartCoroutine(LoadSceneWithLoadingScreen());
        }

        /// <summary>
        /// タイトルに戻る
        /// </summary>
        private void BackToTitle()
        {
            // ダイアログとヘルプを閉じる
            if (_confirmDialogObject != null)
                _confirmDialogObject.SetActive(false);
            if (_helpMenuObject != null)
                _helpMenuObject.SetActive(false);

            // 状態をリセット
            ForceResumeAll();

            // ロードシーンを経由してタイトルへ
            StartCoroutine(LoadSceneWithLoadingScreen(GameConstants.Scene.Title.ToString()));
        }

        /// <summary>
        /// ロード画面を経由してシーンを読み込む
        /// </summary>
        /// <param name="targetScene">遷移先シーン名(nullの場合は現在のシーンをリロード)</param>
        private IEnumerator LoadSceneWithLoadingScreen(string targetScene = null)
        {
            Debug.Log("[StageConfigMenu] LoadSceneWithLoadingScreen開始");

            // 複数フレーム待って確実に状態をリセット
            yield return null;
            yield return null;

            // 再度リセット確認(念押し)
            Time.timeScale = 1f;
            Physics.simulationMode = SimulationMode.FixedUpdate;
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;

            Debug.Log($"[StageConfigMenu] Time.timeScale={Time.timeScale}, Physics={Physics.simulationMode}, Physics2D={Physics2D.simulationMode}");

            // プレイヤーのRigidbodyを強制的に再開
            ForceResumePlayer();

            // さらに1フレーム待機
            yield return null;

            // 最終確認
            Time.timeScale = 1f;
            Debug.Log($"[StageConfigMenu] 最終確認 Time.timeScale={Time.timeScale}");

            // 現在のシーン名を取得
            string currentSceneName = SceneManager.GetActiveScene().name;

            // ターゲットシーンが指定されていない場合は現在のシーンをリロード
            string sceneToLoad = string.IsNullOrEmpty(targetScene) ? currentSceneName : targetScene;

            Debug.Log($"[StageConfigMenu] ロードシーン経由でシーン遷移: {sceneToLoad}");

            // SceneLoaderを使用してロード画面経由で遷移
            if (_sceneLoader != null)
            {
                _sceneLoader.LoadScene(sceneToLoad);
            }
            else
            {
                // SceneLoaderが利用できない場合は直接ロード
                Debug.LogWarning("[StageConfigMenu] SceneLoaderが見つからないため直接ロードします");
                SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
            }
        }

        /// <summary>
        /// プレイヤーの物理挙動を強制的に再開
        /// </summary>
        private void ForceResumePlayer()
        {
            // まずタグで検索
            var player = GameObject.FindGameObjectWithTag("Player");

            // タグで見つからない、または名前が"spine"の場合は名前で検索
            if (player == null || player.name == "spine")
            {
                if (player != null && player.name == "spine")
                {
                    Debug.LogWarning("[StageConfigMenu] ★ 「spine」に「Player」タグが付いています。これは間違いです。");
                }

                // 名前で直接検索(MagForce_Prefabまたはそのクローン)
                player = GameObject.Find("MagForce_Prefab(Clone)");

                if (player == null)
                {
                    player = GameObject.Find("MagForce_Prefab");
                }

                if (player != null)
                {
                    Debug.Log($"[StageConfigMenu] 名前で検索してプレイヤーを発見: {player.name}");
                }
            }

            if (player != null)
            {
                Debug.Log("[StageConfigMenu] プレイヤーの物理挙動を強制再開");
                Debug.Log($"  - 検出されたオブジェクト: {player.name}");

                // プレイヤーをアクティブ化
                if (!player.activeSelf)
                {
                    player.SetActive(true);
                    Debug.Log("  - Player をアクティブ化しました");
                }

                // 親子構造も含めて全てのRigidbodyを再開
                var rb = player.GetComponentInChildren<Rigidbody>();
                if (rb != null)
                {
                    Debug.Log($"  - Rigidbody発見: {rb.gameObject.name}");
                    Debug.Log($"  - 修正前: isKinematic={rb.isKinematic}, useGravity={rb.useGravity}, constraints={rb.constraints}");

                    rb.isKinematic = false;

                    // 注意: PlayerMoveControllerがuseGravity=falseでカスタム重力を使用しているため
                    // useGravityはfalseのままにする(OnStart()で設定される)
                    // rb.useGravity = false;

                    // 重要: constraintsを適切に設定 
                    // 3Dキャラクターの場合、通常は回転のみ固定(FreezeRotation)
                    rb.constraints = RigidbodyConstraints.FreezeRotation;

                    rb.WakeUp();

                    Debug.Log($"  - 修正後: isKinematic={rb.isKinematic}, useGravity={rb.useGravity}, constraints={rb.constraints}");
                }

                // Rigidbodyが見つからなかった場合の警告
                if (rb == null)
                {
                    Debug.LogWarning("  - Rigidbodyが見つかりません！プレイヤーにRigidbodyコンポーネントを追加してください");
                }

                // 親子構造も含めて全てのMonoBehaviourスクリプトを有効化
                var scripts = player.GetComponentsInChildren<MonoBehaviour>();
                Debug.Log($"  - MonoBehaviour数（子を含む）: {scripts.Length}");
                foreach (var script in scripts)
                {
                    if (script != null && script != this)  // 自分自身は除外
                    {
                        script.enabled = true;
                        Debug.Log($"    - {script.GetType().Name} ({script.gameObject.name}): enabled={script.enabled}");
                    }
                }

                if (scripts.Length == 0)
                {
                    Debug.LogWarning("  - プレイヤーにスクリプトが1つも付いていません！移動スクリプトを追加してください");
                }
            }
            else
            {
                Debug.LogError("プレイヤーが見つかりません！タグ「Player」が設定されているか、またはオブジェクト名が「MagForce_Prefab」であることを確認してください ");
            }
        }

        /// <summary>
        /// ゲーム状態を強制的にリセット
        /// </summary>
        private void ForceResumeAll()
        {
            Debug.Log("[StageConfigMenu] ========================================");
            Debug.Log("[StageConfigMenu] ForceResumeAll() 実行開始");
            Debug.Log("[StageConfigMenu] ========================================");

            // Time.timeScaleをリセット(最優先)
            Time.timeScale = 1f;
            Debug.Log($"[StageConfigMenu] Time.timeScale を 1f に設定: {Time.timeScale}");

            // 物理シミュレーションもリセット
            Physics.simulationMode = SimulationMode.FixedUpdate;
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
            Debug.Log($"[StageConfigMenu] Physics.simulationMode: {Physics.simulationMode}");
            Debug.Log($"[StageConfigMenu] Physics2D.simulationMode: {Physics2D.simulationMode}");

            // プレイヤーの物理挙動を強制再開
            ForceResumePlayer();

            // GlobalUIManagerの複数インスタンスを削除
            var allManagers = FindObjectsOfType<GlobalUIManager>();
            Debug.Log($"[StageConfigMenu] GlobalUIManager数: {allManagers.Length}");

            if (allManagers.Length > 1)
            {
                Debug.LogWarning($"[StageConfigMenu] 複数のGlobalUIManager検出! 余分なインスタンスを削除します");

                // 最初のインスタンス以外を即座に削除
                for (int i = 1; i < allManagers.Length; i++)
                {
                    if (allManagers[i] != null && allManagers[i].gameObject != null)
                    {
                        Debug.Log($"[StageConfigMenu] 削除: {allManagers[i].gameObject.name}");
                        DestroyImmediate(allManagers[i].gameObject);
                    }
                }
            }

            // 全てのGlobalUIManagerインスタンスに対してForceResetを実行 
            allManagers = FindObjectsOfType<GlobalUIManager>();
            foreach (var manager in allManagers)
            {
                if (manager != null)
                {
                    Debug.Log($"[StageConfigMenu] {manager.gameObject.name} の ForceResetGameState() を呼び出し");
                    Debug.Log($"  - 修正前: IsPaused={manager.IsPaused}");

                    manager.ForceResetGameState();

                    Debug.Log($"  - 修正後: IsPaused={manager.IsPaused}");
                }
            }

            // InputHandlerの状態もリセット 
            if (_input != null)
            {
                Debug.Log("[StageConfigMenu] InputHandlerの状態をリセット");
                _input.ForceResetInputState();
            }
            else
            {
                Debug.LogWarning("[StageConfigMenu] InputHandlerが null です");
            }

            // 最後にもう一度確認(念押し)
            Time.timeScale = 1f;

            Debug.Log($"[StageConfigMenu] ForceResumeAll() 完了 - Time.timeScale: {Time.timeScale}");
            Debug.Log("[StageConfigMenu] ========================================");
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
        }

        private void CloseConfirmDialog()
        {
            if (_confirmDialogObject == null) return;

            _isConfirmActive = false;
            _confirmDialogObject.SetActive(false);
            _confirmCallback = null;

            try
            {
                if (SEManager.instance != null)
                {
                    SEManager.instance.PlaySE(SEManager.Menu.CANCEL);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"SE再生エラー: {e.Message}");
            }
        }

        #endregion
    }
}