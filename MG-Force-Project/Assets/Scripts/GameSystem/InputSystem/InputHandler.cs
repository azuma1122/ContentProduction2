using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game.GameSystem
{
    public class InputHandler : MonoBehaviour
    {
        private const float THRES_HOLD = 0.5f;

        private PlayerInput _playerInput;

        private DeviceManager _deviceManager;

        #region -------- シングルトンの設定 --------

        // シングルトンインスタンス
        public static InputHandler Instance { get; private set; }

        private void Awake()
        {
            // シングルトンの設定
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _playerInput = GetComponent<PlayerInput>();

            _deviceManager = GameObject.Find(GameConstants.Object.DEVICE_MANAGER).GetComponent<DeviceManager>();

            InitializeInputSystem();

            _playerInput.SwitchCurrentControlScheme(InputConstants.ActionDevice.KEY_MOUSE, InputSystem.GetDevice<Keyboard>(), InputSystem.GetDevice<Mouse>());
            _playerInput.SwitchCurrentActionMap(InputConstants.ActionMaps.MENU_MAPS);
        }

        #endregion

        // InputActionアセット
        private InputActionAsset _inputActions;

        // アクション状態の辞書
        private Dictionary<string, bool> _actionStates = new Dictionary<string, bool>();

        private bool _isKeyChange;

        private void InitializeInputSystem()
        {
            _inputActions = _playerInput.actions;

            foreach (var map in _inputActions.actionMaps)
            {
                foreach (var action in map.actions)
                {
                    _actionStates[action.name] = false;
                    action.performed += ctx => _actionStates[action.name] = true;
                    action.canceled += ctx => _actionStates[action.name] = false;
                }
            }
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        private void Update()
        {
            InputDeviceUpdate();

            InputEnableUpdate();
        }

        private void InputDeviceUpdate()
        {
            if (_deviceManager.isGamepad)
            {
                _playerInput.SwitchCurrentControlScheme(
                    (!_isKeyChange) ? InputConstants.ActionDevice.GAMEPAD : InputConstants.ActionDevice.GAMEPAD_2, InputSystem.GetDevice<Gamepad>());
            }
            else if (!_deviceManager.isGamepad && _playerInput.currentControlScheme != InputConstants.ActionDevice.KEY_MOUSE)
            {
                _playerInput.SwitchCurrentControlScheme(InputConstants.ActionDevice.KEY_MOUSE, InputSystem.GetDevice<Keyboard>(), InputSystem.GetDevice<Mouse>());
            }
        }

        /// <summary>
        /// Input System経由でアクションが押されているかを取得
        /// </summary>
        public bool IsActionPressed(string action_name)
        {
            bool is_action_pressed = _playerInput.actions[action_name].triggered;
            
            return is_action_pressed;
        }

        public bool IsActionPressing(string action_name)
        {
            float current_action_pressed = _playerInput.actions[action_name].ReadValue<float>();

            return Mathf.Abs(current_action_pressed) >= THRES_HOLD;
        }

        public bool IsActionPressing(string action_name, Vector2 type)
        {
            Vector2 current_action_vector = _playerInput.actions[action_name].ReadValue<Vector2>();

            // 座標が1以上(マウス座標)の時
            if (current_action_vector.x > 1.0f || current_action_vector.y > 1.0f ||
                current_action_vector.x < -1.0f || current_action_vector.y < -1.0f)
            {
                current_action_vector = new Vector2(960.0f, 540.0f) - current_action_vector;

                current_action_vector = -current_action_vector;
            }

            // 正規化して最小値に
            current_action_vector = current_action_vector.normalized;

            return Vector2.Distance(current_action_vector, type) < THRES_HOLD;
        }

        private void InputEnableUpdate()
        {
            Scene scene = SceneManager.GetActiveScene();

            switch ((GameConstants.Scene)scene.buildIndex) 
            {
                case GameConstants.Scene.StageSelect:
                    InputStageSelectScene();
                    break;

                case GameConstants.Scene.Stage:
                    InputStageScene();
                    break;

                default:
                    InputSetMenuMap();
                    break;
            }

            //Debug.Log($"MenuOpen : {_isMenuOpen}, ViewMode : {_isViewMode}, MagnetBoot : {_isMagnetBoot}");

            _playerInput.actions.FindActionMap(InputConstants.ActionMaps.SHORTCUT_MAPS).Enable();

#if UNITY_EDITOR
            _playerInput.actions.FindActionMap(InputConstants.ActionMaps.DEBUG_MAPS).Enable();
#endif
        }

        #region -------- キーの有効化処理 --------

        private bool _isMenuOpen = false;
        private bool _isViewMode = false;
        private bool _isMagnetBoot = false;

        private void InputSetMenuMap()
        {
            if (_playerInput.currentActionMap.name != InputConstants.ActionMaps.MENU_MAPS)
            {
                _playerInput.SwitchCurrentActionMap(InputConstants.ActionMaps.MENU_MAPS);
            }
        }

        private void InputStageSelectScene()
        {
            OnMenu();

            if (!_isMenuOpen)
            {
                if (!_isMagnetBoot)
                {
                    OnViewMode();
                }

                if (!_isViewMode)
                {
                    OnMagnet();
                }
            }

            if (!_isMenuOpen && !_isViewMode)
            {
                _playerInput.SwitchCurrentActionMap(InputConstants.ActionMaps.PLAYER_MAPS);

                if (!_isMagnetBoot)
                {
                    _playerInput.actions.FindActionMap(InputConstants.ActionMaps.MAGNET_MAPS).Enable();
                }
            }
        }

        private void InputStageScene()
        {
            OnMenu();

            if (!_isMenuOpen)
            {
                if (!_isMagnetBoot)
                {
                    OnViewMode();
                }

                if (!_isViewMode)
                {
                    OnMagnet();
                }
            }

            if (!_isMenuOpen && !_isViewMode)
            {
                _playerInput.SwitchCurrentActionMap(InputConstants.ActionMaps.PLAYER_MAPS);

                if (!_isMagnetBoot)
                {
                    _playerInput.actions.FindActionMap(InputConstants.ActionMaps.MAGNET_MAPS).Enable();
                }
            }
        }

        private void OnMenu()
        {
            if (_isMenuOpen)
            {
                // メニューを閉じる
                if (_playerInput.actions[InputConstants.Action.MENU_CLOSE].triggered)
                {
                    _isMenuOpen = false;
                }
            }
            else
            {
                // メニューを開く
                if (_playerInput.actions[InputConstants.Action.MENU_OPEN].triggered)
                {
                    _playerInput.SwitchCurrentActionMap(InputConstants.ActionMaps.MENU_MAPS);
                    _isMenuOpen = true;
                }
            }
        }

        private void OnViewMode()
        {
            if (_isViewMode)
            {
                // ビューモード終了
                if (_playerInput.actions[InputConstants.Action.VIEW_MODE_END].triggered)
                {
                    _isViewMode = false;
                }
            }
            else
            {
                // ビューモード開始
                if (_playerInput.actions[InputConstants.Action.VIEW_MODE_START].triggered)
                {
                    _playerInput.SwitchCurrentActionMap(InputConstants.ActionMaps.CAMERA_MAPS);
                    _isViewMode = true;
                }
            }
        }

        private void OnMagnet()
        {
            if (_isMagnetBoot)
            {
                if (_playerInput.actions[InputConstants.Action.MAGNET_BOOT].triggered)
                {
                    _isMagnetBoot = false;
                }
            }
            else
            {
                if (_playerInput.actions[InputConstants.Action.MAGNET_BOOT].triggered)
                {
                    _isMagnetBoot = true;
                }
            }
        }

        #endregion

        public void GamePadKeyChange()
        {
            _isKeyChange = !_isKeyChange;

            if (_playerInput.currentControlScheme == InputConstants.ActionDevice.KEY_MOUSE) return;

            _playerInput.SwitchCurrentControlScheme(
                (!_isKeyChange) ? InputConstants.ActionDevice.GAMEPAD : InputConstants.ActionDevice.GAMEPAD_2, InputSystem.GetDevice<Gamepad>());
        }

        public string GetControlScheme()
        {
            return _playerInput.currentControlScheme;
        }
    }
}