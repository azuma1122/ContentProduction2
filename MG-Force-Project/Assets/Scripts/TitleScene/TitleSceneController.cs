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

        [SerializeField] private TextMeshProUGUI _titleMessage;

        [SerializeField] private GameObject[] _menuObjects = new GameObject[(int)TitleStep.MAX_STEP];

        [SerializeField] private GameObject[] _gameMenu = new GameObject[(int)GameMenu.MAX_BUTTON];
        [SerializeField] private GameObject[] _startMenu = new GameObject[(int)StartMenu.MAX_BUTTON];
        [SerializeField] private GameObject[] _configMenu = new GameObject[(int)ConfigMenu.MAX_BUTTON];

        [SerializeField] private Slider[] _soundSlider = new Slider[(int)SoundSlider.MAX_SLIDER];
        [SerializeField] private Toggle _keyToggle;

        [SerializeField] private GameDataEraseController _eraseContrller;
        [SerializeField, Range(0f, 1f)] private float _doubleClickPreventionTime;

        private int _currentButton = INIT_BUTTON;
        private bool _isStepChanging = false;

        private Vector3 _targetButton = new Vector3(1.2f, 1.2f, 1.2f);
        private Vector3 _offTargetButton = new Vector3(1.0f, 1.0f, 1.0f);

        private void Awake()
        {
            StageDataLoader.LoadStageData();

            if (!isLoadGameData)
            {
                isLoadGameData = true;
            }

            _input = GameObject.Find(GameConstants.Object.INPUT).GetComponent<InputHandler>();
            _deviceManager = GameObject.Find(GameConstants.Object.DEVICE_MANAGER).GetComponent<DeviceManager>();
            _sceneLoader = SceneLoader.Instance;

            GameObject seManagerObj = GameObject.Find(GameConstants.Object.SE_MANAGER);
            if (seManagerObj != null)
            {
                _seManager = seManagerObj.GetComponent<SEManager>();
            }

            _currentStep = TitleStep.TITLE;
        }

        private void Update()
        {
            if (_input.IsActionPressed(InputConstants.Action.MENU_OPEN))
            {
                SetStep(TitleStep.CONFIG_MENU);
                return;
            }

            if (_input.IsActionPressing(InputConstants.Action.DEBUG_CREDITS))
            {
                _sceneLoader.LoadScene(GameConstants.Scene.Credits.ToString());
            }

            switch (_currentStep)
            {
                case TitleStep.TITLE:
                    TitleUpdate();
                    break;

                case TitleStep.GAME_MENU:
                    GameMenuUpdate();
                    break;

                case TitleStep.CONFIG_MENU:
                    ConfigMenuUpdate();
                    break;

                case TitleStep.GAMEDATA_ERASE:
                    if (!_menuObjects[(int)TitleStep.GAMEDATA_ERASE].activeSelf)
                    {
                        _menuObjects[(int)TitleStep.GAMEDATA_ERASE].SetActive(true);
                    }
                    else
                    {
                        if (!_eraseContrller.isActive)
                        {
                            SetStep(TitleStep.CONFIG_MENU);
                        }
                    }
                    break;
            }
        }

        public void TitleUpdate(bool is_push_button = false)
        {
            if (_deviceManager.isGamepad)
            {
                _titleMessage.text = "ボタンを押してください";
            }
            else
            {
                _titleMessage.text = "画面をクリックしてください";
            }

            if (_input.IsActionPressed(InputConstants.Action.MENU_DECISION) || is_push_button)
            {
                SetStep(TitleStep.GAME_MENU);
            }
        }

        private void GameMenuUpdate()
        {
            // 追加：遷移中なら入力を一切受け付けない
            if (_isStepChanging) return;
            if (_input.IsActionPressed(InputConstants.Action.MENU_DECISION))
            {
                SEManager.instance.PlaySE(SEManager.Menu.DECISION);
                GameMenuDecision(_currentButton);
                return;
            }
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
            else if (_input.IsActionPressed(InputConstants.Action.MENU_BACK))
            {
                SEManager.instance.PlaySE(SEManager.Menu.CANCEL);
                SetStep(TitleStep.TITLE);
            }

            GameMenuButtonUpdate();
        }

        private void GameMenuButtonUpdate()
        {
            for (int i = (int)GameMenu.CONFIG; i < (int)GameMenu.MAX_BUTTON; i++)
            {
                _gameMenu[i].transform.localScale = (_currentButton == i) ? _targetButton : _offTargetButton;
            }
        }

        public void GameMenuDecision(int button_index)
        {
            if (_isStepChanging) return;


            // 決定した瞬間にロックをかける
            _isStepChanging = true;

            // シーン遷移以外のステップ（設定画面へ行く等）の場合は、
            // 一定時間後にロックを解除するコルーチンを回す
            if (button_index != (int)GameMenu.START && button_index != (int)GameMenu.GAME_FINISH)
            {
                StartCoroutine(ResetInputLock());
            }
            if (button_index != INIT_BUTTON)
            {
                _currentButton = button_index;
            }

            switch (button_index)
            {
                case (int)GameMenu.CONFIG:
                    _seManager?.PlaySE(SEManager.Menu.DECISION);
                    SetStep(TitleStep.CONFIG_MENU);
                    break;

                case (int)GameMenu.START:
                    _seManager?.PlaySE(SEManager.Menu.DECISION);
                    Debug.Log("Scene Transition to: " + GameConstants.Scene.StageSelect.ToString());
                    _sceneLoader.LoadScene(GameConstants.Scene.StageSelect.ToString());
                    break;

                case (int)GameMenu.GAME_FINISH:
                    GameFinish();
                    break;

                case INIT_BUTTON:
                    _currentButton = (int)GameMenu.START;
                    break;
            }
        }

        public void OnPointerEnterButton(int button_index)
        {
            _currentButton = button_index;
            SEManager.instance.PlaySE(SEManager.Menu.SELECT);
        }

        public void GameFinish()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ConfigMenuUpdate()
        {
            if (_input.IsActionPressed(InputConstants.Action.MENU_DECISION))
            {
                ConfigMenuDecisioin(_currentButton);
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
            else if (_input.IsActionPressed(InputConstants.Action.MENU_BACK))
            {
                SetStep(TitleStep.GAME_MENU);
            }

            SoundVolumeUpdate();
            ConfigMenuButtonUpdate();
        }

        private void ConfigMenuButtonUpdate()
        {
            for (int i = (int)ConfigMenu.BGM; i < (int)ConfigMenu.MAX_BUTTON; i++)
            {
                _configMenu[i].transform.localScale = (_currentButton == i) ? _targetButton : _offTargetButton;
            }
        }

        public void ConfigMenuDecisioin(int button_index)
        {
            switch (button_index)
            {
                case (int)ConfigMenu.BGM:
                    AudioSource bgm_audio = GameObject.Find(GameConstants.Object.BGM_MANAGER).GetComponent<AudioSource>();
                    bgm_audio.mute = !bgm_audio.mute;
                    break;

                case (int)ConfigMenu.SE:
                    AudioSource se_audio = GameObject.Find(GameConstants.Object.SE_MANAGER).GetComponent<AudioSource>();
                    se_audio.mute = !se_audio.mute;
                    break;

                case (int)ConfigMenu.KEY:
                    _keyToggle.isOn = !_keyToggle.isOn;
                    break;

                case (int)ConfigMenu.DATA:
                    SetStep(TitleStep.GAMEDATA_ERASE);
                    break;

                case (int)ConfigMenu.BACK:
                    SetStep(TitleStep.GAME_MENU);
                    break;
            }
        }

        private void SoundVolumeUpdate()
        {
            if (_currentButton == (int)ConfigMenu.BGM || _currentButton == INIT_BUTTON)
                ChangeBGMVolume();

            if (_currentButton == (int)ConfigMenu.SE || _currentButton == INIT_BUTTON)
                ChangeSEVolume();
        }

        private void ChangeBGMVolume()
        {
            BGMManager bgm_manager = GameObject.Find(GameConstants.Object.BGM_MANAGER).GetComponent<BGMManager>();
            float sound = _soundSlider[(int)SoundSlider.BGM].value;
            _soundSlider[(int)SoundSlider.BGM].value = bgm_manager.VolumeChange(sound);
        }

        private void ChangeSEVolume()
        {
            SEManager se_manager = GameObject.Find(GameConstants.Object.SE_MANAGER).GetComponent<SEManager>();
            float sound = _soundSlider[(int)SoundSlider.SE].value;
            _soundSlider[(int)SoundSlider.SE].value = se_manager.VolumeChange(sound);
        }

        public void GamePadKeyChange()
        {
            _input.GamePadKeyChange();
        }

        private void SetStep(TitleStep step)
        {
            _menuObjects[(int)_currentStep].SetActive(false);
            _currentStep = step;
            _currentButton = INIT_BUTTON;
            _menuObjects[(int)_currentStep].SetActive(true);
        }

        private IEnumerator ResetInputLock()
        {
            _isStepChanging = true;
            yield return new WaitForSeconds(_doubleClickPreventionTime);
            _isStepChanging = false;
        }

        public void SetStep(int step)
        {
            SetStep((TitleStep)step);
        }
    }
}
