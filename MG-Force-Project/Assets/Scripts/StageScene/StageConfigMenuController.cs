using UnityEngine;
using Game.GameSystem;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace Game.Stage
{
    /// <summary>
    /// ステージシーン内の設定メニュー制御クラス
    /// - BGM/SE音量調整
    /// - キー設定切り替え
    /// - 操作説明表示
    /// - ステージリセット
    /// - タイトルへ戻る
    /// </summary>
    public class StageConfigMenuController : MonoBehaviour
    {
        // 入力管理
        private InputHandler _input;

        // シーン遷移管理
        private SceneLoader _sceneLoader = SceneLoader.Instance;

        #region -------- メニュー項目定義 --------

        /// <summary>
        /// 設定メニュー内のボタン
        /// </summary>
        private enum ConfigMenu
        {
            BGM,            // BGM設定
            SE,             // SE設定
            KEY,            // キー設定
            HELP,           // 操作説明
            RESET,          // ステージリセット
            TITLE,          // タイトルに戻る
            BACK,           // 戻る（ゲーム再開）
            MAX_BUTTON,
        }

        /// <summary>
        /// サウンドスライダーの種類
        /// </summary>
        private enum SoundSlider
        {
            BGM,
            SE,
            MAX_SLIDER,
        }

        #endregion

        private const int INIT_BUTTON = -1; // 初期状態（ボタン未選択）

        [Header("メニュー表示")]
        [SerializeField] private GameObject _configMenuObject;              // 設定メニュー全体
        [SerializeField] private GameObject _helpMenuObject;                // 操作説明画面
        [SerializeField] private GameObject _confirmDialogObject;           // 確認ダイアログ

        [Header("設定メニューボタン")]
        [SerializeField] private GameObject[] _configMenu = new GameObject[(int)ConfigMenu.MAX_BUTTON];

        [Header("サウンド設定")]
        [SerializeField] private Slider[] _soundSlider = new Slider[(int)SoundSlider.MAX_SLIDER];
        [SerializeField] private Toggle _keyToggle;

        [Header("確認ダイアログ")]
        [SerializeField] private TextMeshProUGUI _confirmMessageText;       // 確認メッセージ
        [SerializeField] private Button _confirmYesButton;                  // はいボタン
        [SerializeField] private Button _confirmNoButton;                   // いいえボタン

        [Header("操作説明")]
        [SerializeField] private TextMeshProUGUI _helpText;                 // 操作説明テキスト

        private int _currentButton = INIT_BUTTON;           // 現在選択中のボタン番号
        private bool _isMenuActive = false;                 // メニューが開いているか
        private bool _isConfirmActive = false;              // 確認ダイアログが開いているか
        private bool _isHelpActive = false;                 // 操作説明が開いているか
        private System.Action _confirmCallback;             // 確認ダイアログで「はい」を押した時の処理

        // 選択中／非選択時のボタン拡大率
        private Vector3 _targetButton = new Vector3(1.2f, 1.2f, 1.2f);
        private Vector3 _offTargetButton = new Vector3(1.0f, 1.0f, 1.0f);

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Awake()
        {
            // 各管理クラスの取得
            _input = GameObject.Find(GameConstants.Object.INPUT).GetComponent<InputHandler>();
            _sceneLoader = SceneLoader.Instance;

            // 初期状態では非表示
            _configMenuObject.SetActive(false);
            _helpMenuObject.SetActive(false);
            _confirmDialogObject.SetActive(false);

            // 確認ダイアログのボタン設定
            _confirmYesButton.onClick.AddListener(OnConfirmYes);
            _confirmNoButton.onClick.AddListener(OnConfirmNo);
        }

        /// <summary>
        /// 毎フレーム更新処理
        /// </summary>
        private void Update()
        {
            // Mキー または ESCキーでメニュー開閉
            if (_input.IsActionPressed(InputConstants.Action.MENU_OPEN) ||
                _input.IsActionPressed(InputConstants.Action.MENU_BACK))
            {
                // 確認ダイアログが開いている場合は閉じる
                if (_isConfirmActive)
                {
                    CloseConfirmDialog();
                    return;
                }

                // 操作説明が開いている場合は閉じる
                if (_isHelpActive)
                {
                    CloseHelp();
                    return;
                }

                // メニューの開閉切り替え
                if (_isMenuActive)
                {
                    CloseMenu();
                }
                else
                {
                    OpenMenu();
                }
                return;
            }

            // メニューが開いている時のみ操作受付
            if (_isMenuActive && !_isConfirmActive && !_isHelpActive)
            {
                ConfigMenuUpdate();
            }
        }

        #region -------- メニュー開閉処理 --------

        /// <summary>
        /// メニューを開く
        /// </summary>
        public void OpenMenu()
        {
            _isMenuActive = true;
            _configMenuObject.SetActive(true);
            _currentButton = INIT_BUTTON;

            // ゲームを一時停止
            Time.timeScale = 0f;
        }

        /// <summary>
        /// メニューを閉じる
        /// </summary>
        public void CloseMenu()
        {
            _isMenuActive = false;
            _configMenuObject.SetActive(false);
            _currentButton = INIT_BUTTON;

            // ゲームを再開
            Time.timeScale = 1f;
        }

        #endregion

        #region -------- 設定メニュー処理 --------

        /// <summary>
        /// 設定メニューの更新処理
        /// </summary>
        private void ConfigMenuUpdate()
        {
            if (_input.IsActionPressed(InputConstants.Action.MENU_DECISION))
            {
                ConfigMenuDecision(_currentButton);
            }
            else if (_input.IsActionPressed(InputConstants.Action.MENU_UP_SELECT))
            {
                if (_currentButton == INIT_BUTTON)
                    _currentButton = (int)ConfigMenu.BGM;
                else if (_currentButton != (int)ConfigMenu.BGM)
                    _currentButton--;
            }
            else if (_input.IsActionPressed(InputConstants.Action.MENU_DOWN_SELECT))
            {
                if (_currentButton == INIT_BUTTON)
                    _currentButton = (int)ConfigMenu.BGM;
                else if (_currentButton != (int)ConfigMenu.BACK)
                    _currentButton++;
            }

            // 音量変更更新
            SoundVolumeUpdate();

            // 見た目更新
            ConfigMenuButtonUpdate();
        }

        /// <summary>
        /// 設定メニュー内のボタン見た目更新
        /// </summary>
        private void ConfigMenuButtonUpdate()
        {
            for (int i = (int)ConfigMenu.BGM; i < (int)ConfigMenu.MAX_BUTTON; i++)
            {
                if (_configMenu[i] != null)
                {
                    _configMenu[i].transform.localScale =
                        (_currentButton == i) ? _targetButton : _offTargetButton;
                }
            }
        }

        /// <summary>
        /// 設定メニュー決定処理
        /// </summary>
        public void ConfigMenuDecision(int button_index)
        {
            switch (button_index)
            {
                case (int)ConfigMenu.BGM:
                    ToggleBGMMute();
                    break;

                case (int)ConfigMenu.SE:
                    ToggleSEMute();
                    break;

                case (int)ConfigMenu.KEY:
                    ToggleKeyConfig();
                    break;

                case (int)ConfigMenu.HELP:
                    ShowHelp();
                    break;

                case (int)ConfigMenu.RESET:
                    ShowResetConfirm();
                    break;

                case (int)ConfigMenu.TITLE:
                    ShowTitleConfirm();
                    break;

                case (int)ConfigMenu.BACK:
                    CloseMenu();
                    break;

                case INIT_BUTTON:
                    _currentButton = (int)ConfigMenu.BGM;
                    break;
            }
        }

        #endregion

        #region -------- 各機能の実装 --------

        /// <summary>
        /// BGMミュート切り替え
        /// </summary>
        private void ToggleBGMMute()
        {
            AudioSource bgm_audio = GameObject.Find(GameConstants.Object.BGM_MANAGER).GetComponent<AudioSource>();
            if (bgm_audio != null)
            {
                bgm_audio.mute = !bgm_audio.mute;
            }
        }

        /// <summary>
        /// SEミュート切り替え
        /// </summary>
        private void ToggleSEMute()
        {
            AudioSource se_audio = GameObject.Find(GameConstants.Object.SE_MANAGER).GetComponent<AudioSource>();
            if (se_audio != null)
            {
                se_audio.mute = !se_audio.mute;
            }
        }

        /// <summary>
        /// キー設定切り替え
        /// </summary>
        private void ToggleKeyConfig()
        {
            if (_keyToggle != null)
            {
                _keyToggle.isOn = !_keyToggle.isOn;
                _input.GamePadKeyChange();
            }
        }

        /// <summary>
        /// 操作説明を表示
        /// </summary>
        private void ShowHelp()
        {
            _isHelpActive = true;
            _helpMenuObject.SetActive(true);

            // 操作説明テキストの設定
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
        private void CloseHelp()
        {
            _isHelpActive = false;
            _helpMenuObject.SetActive(false);
        }

        /// <summary>
        /// リセット確認ダイアログを表示
        /// </summary>
        private void ShowResetConfirm()
        {
            ShowConfirmDialog(
                "ステージを最初からやり直しますか？",
                () => ResetStage()
            );
        }

        /// <summary>
        /// タイトルへ戻る確認ダイアログを表示
        /// </summary>
        private void ShowTitleConfirm()
        {
            ShowConfirmDialog(
                "タイトル画面に戻りますか？\n（進行状況は保存されません）",
                () => BackToTitle()
            );
        }

        /// <summary>
        /// ステージをリセット
        /// </summary>
        private void ResetStage()
        {
            // 時間を戻す
            Time.timeScale = 1f;

            // 現在のシーンを再読み込み
            string currentSceneName = SceneManager.GetActiveScene().name;
            _sceneLoader.LoadScene(currentSceneName);
        }

        /// <summary>
        /// タイトルに戻る
        /// </summary>
        private void BackToTitle()
        {
            // 時間を戻す
            Time.timeScale = 1f;

            // タイトルシーンへ遷移
            _sceneLoader.LoadScene(GameConstants.Scene.Title.ToString());
        }

        #endregion

        #region -------- 確認ダイアログ処理 --------

        /// <summary>
        /// 確認ダイアログを表示
        /// </summary>
        private void ShowConfirmDialog(string message, System.Action onConfirm)
        {
            _isConfirmActive = true;
            _confirmDialogObject.SetActive(true);
            _confirmMessageText.text = message;
            _confirmCallback = onConfirm;
        }

        /// <summary>
        /// 確認ダイアログを閉じる
        /// </summary>
        private void CloseConfirmDialog()
        {
            _isConfirmActive = false;
            _confirmDialogObject.SetActive(false);
            _confirmCallback = null;
        }

        /// <summary>
        /// 確認ダイアログで「はい」を選択
        /// </summary>
        private void OnConfirmYes()
        {
            _confirmCallback?.Invoke();
            CloseConfirmDialog();
        }

        /// <summary>
        /// 確認ダイアログで「いいえ」を選択
        /// </summary>
        private void OnConfirmNo()
        {
            CloseConfirmDialog();
        }

        #endregion

        #region -------- サウンド設定関連 --------

        /// <summary>
        /// 音量更新処理
        /// </summary>
        private void SoundVolumeUpdate()
        {
            if (_currentButton == (int)ConfigMenu.BGM || _currentButton == INIT_BUTTON)
                ChangeBGMVolume();

            if (_currentButton == (int)ConfigMenu.SE || _currentButton == INIT_BUTTON)
                ChangeSEVolume();
        }

        /// <summary>
        /// BGM音量変更
        /// </summary>
        private void ChangeBGMVolume()
        {
            BGMManager bgm_manager = GameObject.Find(GameConstants.Object.BGM_MANAGER)?.GetComponent<BGMManager>();
            if (bgm_manager != null && _soundSlider[(int)SoundSlider.BGM] != null)
            {
                float sound = _soundSlider[(int)SoundSlider.BGM].value;
                _soundSlider[(int)SoundSlider.BGM].value = bgm_manager.VolumeChange(sound);
            }
        }

        /// <summary>
        /// SE音量変更
        /// </summary>
        private void ChangeSEVolume()
        {
            SEManager se_manager = GameObject.Find(GameConstants.Object.SE_MANAGER)?.GetComponent<SEManager>();
            if (se_manager != null && _soundSlider[(int)SoundSlider.SE] != null)
            {
                float sound = _soundSlider[(int)SoundSlider.SE].value;
                _soundSlider[(int)SoundSlider.SE].value = se_manager.VolumeChange(sound);
            }
        }

        #endregion

        #region -------- 外部から呼び出し用のパブリックメソッド --------

        /// <summary>
        /// メニュー開閉トグル
        /// </summary>
        public void ToggleMenu()
        {
            if (_isMenuActive)
                CloseMenu();
            else
                OpenMenu();
        }

        #endregion
    }
}