using UnityEngine;
using Game.GameSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Game.Title
{
    /// <summary>
    /// タイトルシーン全体の制御を行うクラス
    /// - 入力処理の受け取り
    /// - 各メニュー画面の切り替え管理
    /// - サウンド設定やセーブデータ確認などの初期処理
    /// </summary>
    public class TitleSceneController : MonoBehaviour
    {
        // 入力管理
        private InputHandler _input;

        // デバイス（ゲームパッド／キーボードなど）管理
        private DeviceManager _deviceManager = null;

        // シーン遷移管理
        private SceneLoader _sceneLoader = SceneLoader.Instance;

        // ゲームデータロード済みかどうかのフラグ（最初の一度だけロード）
        private static bool isLoadGameData = false;

        // SE（効果音）管理
        private SEManager _seManager;

        #region -------- ステップ管理用定数 --------

        /// <summary>
        /// タイトルシーン内のステップ（画面状態）
        /// </summary>
        public enum TitleStep
        {
            TITLE,          // タイトル画面
            GAME_MENU,      // ゲームメニュー（設定・スタート・終了）
            START_MENU,     // ゲーム開始メニュー（新規／再開）
            CONFIG_MENU,    // 設定メニュー
            GAMEDATA_ERASE, // セーブデータ削除確認画面
            MAX_STEP,
        }

        // ゲームメニュー内のボタン
        private enum GameMenu
        {
            CONFIG,         // 設定
            START,          // スタート
            GAME_FINISH,    // 終了
            MAX_BUTTON
        }

        // スタートメニュー内のボタン
        private enum StartMenu
        {
            NEW_START,      // 新しく始める
            RE_START,       // 続きから
            MAX_BUTTON,
        }

        // 設定メニュー内のボタン
        private enum ConfigMenu
        {
            BGM,            // BGM設定
            SE,             // SE設定
            KEY,            // キー設定
            HELP,           // ヘルプ
            DATA,           // データ削除
            BACK,           // 戻る
            MAX_BUTTON,
        }

        #endregion

        // サウンドスライダーの種類
        private enum SoundSlider
        {
            BGM,
            SE,
            MAX_SLIDER,
        }

        private const int INIT_BUTTON = -1; // 初期状態（ボタン未選択）

        // 現在の画面ステップ
        private TitleStep _currentStep;

        [SerializeField] private TextMeshProUGUI _titleMessage; // タイトル画面のメッセージ（例：ボタンを押してください）

        [SerializeField] private GameObject[] _menuObjects = new GameObject[(int)TitleStep.MAX_STEP];   // 各メニューオブジェクト

        [SerializeField] private GameObject[] _gameMenu = new GameObject[(int)GameMenu.MAX_BUTTON];     // ゲームメニュー内のボタン
        [SerializeField] private GameObject[] _startMenu = new GameObject[(int)StartMenu.MAX_BUTTON];   // スタートメニュー内のボタン
        [SerializeField] private GameObject[] _configMenu = new GameObject[(int)ConfigMenu.MAX_BUTTON]; // 設定メニュー内のボタン

        [SerializeField] private Slider[] _soundSlider = new Slider[(int)SoundSlider.MAX_SLIDER];       // 音量調整用スライダー
        [SerializeField] private Toggle _keyToggle;                                                     // キー設定トグル

        [SerializeField] private GameDataEraseController _eraseContrller; // セーブデータ削除管理
        [SerializeField, Range(0f, 1f)] private float _doubleClickPreventionTime; //ダブルクリック防止時間   

        private int _currentButton = INIT_BUTTON;                         // 現在選択中のボタン番号
        private bool _isExistGameData;                                    // セーブデータが存在するかどうか
        private bool _isStepChanging = false; // ステップ遷移中フラグ
        // 選択中／非選択時のボタン拡大率
        private Vector3 _targetButton = new Vector3(1.2f, 1.2f, 1.2f);
        private Vector3 _offTargetButton = new Vector3(1.0f, 1.0f, 1.0f);

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Awake()
        {
            // ステージデータの読み込み(タイトル起動時)
            StageDataLoader.LoadStageData();

            // ゲームデータのロード（実行中に1回のみ）
            if (!isLoadGameData)
            {
                _isExistGameData = SaveSystem.LoadManager();
                isLoadGameData = true;
            }

            // 各管理クラスの取得
            _input = GameObject.Find(GameConstants.Object.INPUT).GetComponent<InputHandler>();
            _deviceManager = GameObject.Find(GameConstants.Object.DEVICE_MANAGER).GetComponent<DeviceManager>();
            _sceneLoader = SceneLoader.Instance;

            // SEManager の取得（nullチェック付き）
            GameObject seManagerObj = GameObject.Find(GameConstants.Object.SE_MANAGER);
            if (seManagerObj != null)
            {
                _seManager = seManagerObj.GetComponent<SEManager>();
            }

            // 最初はタイトル画面ステップから開始
            _currentStep = TitleStep.TITLE;
        }

        /// <summary>
        /// 毎フレーム更新処理
        /// </summary>
        private void Update()
        {
            // どの画面でも M キーで設定メニューへ移動
            if (_input.IsActionPressed(InputConstants.Action.MENU_OPEN))
            {
                SetStep(TitleStep.CONFIG_MENU);
                return; // 正常に遷移したら他の処理を止める
            }

            // デバッグ用：クレジットシーンへ遷移
            if (_input.IsActionPressing(InputConstants.Action.DEBUG_CREDITS))
            {
                _sceneLoader.LoadScene(GameConstants.Scene.Credits.ToString());
            }

            // 現在のステップに応じた更新処理
            switch (_currentStep)
            {
                case TitleStep.TITLE:
                    TitleUpdate();
                    break;

                case TitleStep.GAME_MENU:
                    GameMenuUpdate();
                    break;

                case TitleStep.START_MENU:
                    StartMenuUpdate();
                    break;

                case TitleStep.CONFIG_MENU:
                    ConfigMenuUpdate();
                    break;

                case TitleStep.GAMEDATA_ERASE:
                    // データ削除確認画面を開く
                    if (!_menuObjects[(int)TitleStep.GAMEDATA_ERASE].activeSelf)
                    {
                        _menuObjects[(int)TitleStep.GAMEDATA_ERASE].SetActive(true);
                    }
                    else
                    {
                        // 終了後に設定画面へ戻る
                        if (!_eraseContrller.isActive)
                        {
                            SetStep(TitleStep.CONFIG_MENU);
                        }
                    }
                    break;
            }
        }

        #region -------- タイトル画面処理 --------

        /// <summary>
        /// タイトル画面の更新処理
        /// </summary>
        public void TitleUpdate(bool is_push_button = false)
        {
            // 入力デバイスに応じてメッセージを表示
            if (_deviceManager.isGamepad)
            {
                _titleMessage.text = "ボタンを押してください";
            }
            else
            {
                _titleMessage.text = "画面をクリックしてください";
            }

            // 決定入力でメニューへ移行
            if (_input.IsActionPressed(InputConstants.Action.MENU_DECISION) || is_push_button)
            {
                //決定
                // SEManager.instance.PlaySE(SEManager.Menu.DECISION); 
                SetStep(TitleStep.GAME_MENU);
            }
        }

        #endregion

        #region -------- ゲームメニュー処理 --------

        /// <summary>
        /// ゲームメニューの更新処理
        /// </summary>
        private void GameMenuUpdate()
        {
            // 決定ボタンで実行
            if (_input.IsActionPressed(InputConstants.Action.MENU_DECISION))
            {
                SEManager.instance.PlaySE(SEManager.Menu.DECISION);

                GameMenuDecision(_currentButton);
                return;

            }
            // 左右キーでボタン選択
            else if (_input.IsActionPressed(InputConstants.Action.MENU_LEFT_SELECT))
            {
                SEManager.instance.PlaySE(SEManager.Menu.SELECT);

                if (_currentButton == INIT_BUTTON)
                    _currentButton = (int)GameMenu.START;
                else if (_currentButton != (int)GameMenu.CONFIG)
                    _currentButton--;
            }
            else if (_input.IsActionPressed(InputConstants.Action.MENU_RIGHT_SELECT))
            {
                SEManager.instance.PlaySE(SEManager.Menu.SELECT);

                if (_currentButton == INIT_BUTTON)
                    _currentButton = (int)GameMenu.START;
                else if (_currentButton != (int)GameMenu.GAME_FINISH)
                    _currentButton++;
            }
            // 戻るキーでタイトルに戻る
            else if (_input.IsActionPressed(InputConstants.Action.MENU_BACK))
            {
                SEManager.instance.PlaySE(SEManager.Menu.CANCEL);

                SetStep(TitleStep.TITLE);
            }

            // ボタンの見た目更新
            GameMenuButtonUpdate();
        }

        /// <summary>
        /// ボタン拡大などの見た目更新
        /// </summary>
        private void GameMenuButtonUpdate()
        {
            for (int i = (int)GameMenu.CONFIG; i < (int)GameMenu.MAX_BUTTON; i++)
            {
                _gameMenu[i].transform.localScale = (_currentButton == i) ? _targetButton : _offTargetButton;
            }
        }

        /// <summary>
        /// ボタン決定時の処理
        /// </summary>
        public void GameMenuDecision(int button_index)
        {
            // すでに遷移処理中なら、SEも再生せず処理を抜ける
            if (_isStepChanging) return;
            // ここで一定時間ロックをかける
            StartCoroutine(ResetInputLock());

            if (button_index != INIT_BUTTON)
            {
                _currentButton = button_index;
            }
            switch (button_index)
            {
                case (int)GameMenu.CONFIG:
                    // SE再生（SEManagerが初期化されている場合のみ）
                    if (_seManager != null)
                    {
                        _seManager.PlaySE(SEManager.Menu.DECISION);
                    }
                    SetStep(TitleStep.CONFIG_MENU);
                    break;

                case (int)GameMenu.START:
                    // SE再生
                    if (_seManager != null)
                    {
                        _seManager.PlaySE(SEManager.Menu.DECISION);
                    }
                    SetStep(TitleStep.START_MENU);
                    break;

                case (int)GameMenu.GAME_FINISH:
                    GameFinish();
                    break;

                case INIT_BUTTON:
                    _currentButton = (int)GameMenu.START;
                    break;
            }
        }
        // マウスがボタンに乗った時にインデックスを強制同期する
        public void OnPointerEnterButton(int button_index)
        {
            _currentButton = button_index;
            SEManager.instance.PlaySE(SEManager.Menu.SELECT);

        }

        /// <summary>
        /// ゲーム終了処理（エディタ／実機対応）
        /// </summary>
        public void GameFinish()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // エディタ停止
#else
            Application.Quit(); // 実機終了
#endif
        }

        #endregion

        #region -------- スタートメニュー処理 --------

        private void StartMenuUpdate()
        {
            if (_input.IsActionPressed(InputConstants.Action.MENU_DECISION))
            {

                StartMenuDecision(_currentButton);
            }
            else if (_input.IsActionPressed(InputConstants.Action.MENU_LEFT_SELECT))
            {
                SEManager.instance.PlaySE(SEManager.Menu.SELECT);

                if (_currentButton == INIT_BUTTON)
                    _currentButton = (_isExistGameData) ? (int)StartMenu.RE_START : (int)StartMenu.NEW_START;
                else if (_currentButton != (int)StartMenu.NEW_START)
                    _currentButton--;
            }
            else if (_input.IsActionPressed(InputConstants.Action.MENU_RIGHT_SELECT))
            {
                SEManager.instance.PlaySE(SEManager.Menu.SELECT);

                if (_currentButton == INIT_BUTTON)
                    _currentButton = (_isExistGameData) ? (int)StartMenu.RE_START : (int)StartMenu.NEW_START;
                else if (_currentButton != (int)StartMenu.RE_START)
                    _currentButton++;
            }
            else if (_input.IsActionPressed(InputConstants.Action.MENU_BACK))
            {
                SetStep(TitleStep.GAME_MENU);
                SEManager.instance.PlaySE(SEManager.Menu.CANCEL);

            }

            StartMenuButtonUpdate();
        }

        /// <summary>
        /// ボタンの拡大／縮小更新
        /// </summary>
        private void StartMenuButtonUpdate()
        {
            for (int i = (int)StartMenu.NEW_START; i < (int)StartMenu.MAX_BUTTON; i++)
            {
                _startMenu[i].transform.localScale = (_currentButton == i) ? _targetButton : _offTargetButton;
            }
        }

        /// <summary>
        /// 決定時の処理（新規開始／続きから）
        /// </summary>
        public void StartMenuDecision(int button_index)
        {
            // すでに遷移処理中なら、SEも再生せず処理を抜ける

            if (_isStepChanging) return;
            if (button_index == INIT_BUTTON)
            {
                _currentButton = _isExistGameData ? (int)StartMenu.RE_START : (int)StartMenu.NEW_START;
                return;
            }
            _isStepChanging = true;      //遷移中フラグをTrueに
            SEManager.instance.PlaySE(SEManager.Menu.DECISION);
            switch (button_index)
            {
                case (int)StartMenu.NEW_START:
                    // 新規データ開始（未実装）
                    break;

                case (int)StartMenu.RE_START:
                    // 続きから開始（未実装）
                    break;

                case INIT_BUTTON:
                    _currentButton = _isExistGameData ? (int)StartMenu.RE_START : (int)StartMenu.NEW_START;
                    return;
            }
            //シーン遷移中をデバッグログで確認
            Debug.Log("Scene Transition to: " + GameConstants.Scene.StageSelect.ToString());


            // ステージ選択シーンへ遷移
            _sceneLoader.LoadScene(GameConstants.Scene.StageSelect.ToString());
        }

        #endregion

        #region -------- 設定メニュー処理 --------

        private void ConfigMenuUpdate()
        {
            if (_input.IsActionPressed(InputConstants.Action.MENU_DECISION))
            {
                // SEManager.instance.PlaySE(SEManager.Menu.DECISION);

                ConfigMenuDecisioin(_currentButton);

            }
            else if (_input.IsActionPressed(InputConstants.Action.MENU_UP_SELECT))
            {
                // SEManager.instance.PlaySE(SEManager.Menu.SELECT);

                if (_currentButton == INIT_BUTTON)
                    _currentButton = (int)ConfigMenu.BGM;
                else if (_currentButton != (int)ConfigMenu.BGM)
                    _currentButton--;
            }
            else if (_input.IsActionPressed(InputConstants.Action.MENU_DOWN_SELECT))
            {
                // SEManager.instance.PlaySE(SEManager.Menu.SELECT);

                if (_currentButton == INIT_BUTTON)
                    _currentButton = (int)ConfigMenu.BGM;
                else if (_currentButton != (int)ConfigMenu.BACK)
                    _currentButton++;
            }
            else if (_input.IsActionPressed(InputConstants.Action.MENU_BACK))
            {
                // SEManager.instance.PlaySE(SEManager.Menu.CANCEL);

                SetStep(TitleStep.GAME_MENU);
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
                _configMenu[i].transform.localScale = (_currentButton == i) ? _targetButton : _offTargetButton;
            }
        }

        /// <summary>
        /// 設定メニュー決定処理
        /// </summary>
        public void ConfigMenuDecisioin(int button_index)
        {
            switch (button_index)
            {
                case (int)ConfigMenu.BGM:
                    // BGMミュート切り替え
                    AudioSource bgm_audio = GameObject.Find(GameConstants.Object.BGM_MANAGER).GetComponent<AudioSource>();
                    bgm_audio.mute = !bgm_audio.mute;
                    break;

                case (int)ConfigMenu.SE:
                    // SEミュート切り替え
                    AudioSource se_audio = GameObject.Find(GameConstants.Object.SE_MANAGER).GetComponent<AudioSource>();
                    se_audio.mute = !se_audio.mute;
                    break;

                case (int)ConfigMenu.KEY:
                    // キー設定のトグル切り替え
                    _keyToggle.isOn = !_keyToggle.isOn;
                    break;

                case (int)ConfigMenu.HELP:
                    // ヘルプ表示（未実装）
                    break;

                case (int)ConfigMenu.DATA:
                    // セーブデータ削除画面へ
                    SetStep(TitleStep.GAMEDATA_ERASE);
                    break;

                case (int)ConfigMenu.BACK:
                    // メニューに戻る
                    SetStep(TitleStep.GAME_MENU);
                    break;
            }
        }

        #region ------------ サウンド設定関連 ------------

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
            BGMManager bgm_manager = GameObject.Find(GameConstants.Object.BGM_MANAGER).GetComponent<BGMManager>();
            float sound = _soundSlider[(int)SoundSlider.BGM].value;
            _soundSlider[(int)SoundSlider.BGM].value = bgm_manager.VolumeChange(sound);
        }

        /// <summary>
        /// SE音量変更
        /// </summary>
        private void ChangeSEVolume()
        {
            SEManager se_manager = GameObject.Find(GameConstants.Object.SE_MANAGER).GetComponent<SEManager>();
            float sound = _soundSlider[(int)SoundSlider.SE].value;
            _soundSlider[(int)SoundSlider.SE].value = se_manager.VolumeChange(sound);
        }

        #endregion

        /// <summary>
        /// ゲームパッドのキー割り当て切り替え
        /// </summary>
        public void GamePadKeyChange()
        {
            _input.GamePadKeyChange();
        }

        #endregion

        /// <summary>
        /// ステップ（画面）切り替え処理
        /// </summary>
        private void SetStep(TitleStep step)
        {
            // 現在の画面を非表示に
            _menuObjects[(int)_currentStep].SetActive(false);

            // ステップ更新
            _currentStep = step;
            _currentButton = INIT_BUTTON;

            // 新しい画面を有効化
            _menuObjects[(int)_currentStep].SetActive(true);
        }

        /// <summary>
        /// _doubleClickPreventionTime秒ほど入力を無視する
        /// </summary>
        /// <returns></returns>
        private IEnumerator ResetInputLock()
        {
            _isStepChanging = true;
            yield return new WaitForSeconds(_doubleClickPreventionTime);
            _isStepChanging = false;
        }
        /// <summary>
        /// ボタンから呼び出す用のステップ変更
        /// </summary>
        public void SetStep(int step)
        {
            SetStep((TitleStep)step);
        }
    }
}