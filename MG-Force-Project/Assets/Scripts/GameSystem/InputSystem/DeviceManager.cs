using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.GameSystem
{
    /// <summary>
    /// ゲーム全体で使用する入力デバイス管理クラス
    /// - シングルトンで管理
    /// - コントローラー接続・切断を検出し、SystemMessageManager に通知
    /// - 現在接続されているデバイスの種類を保持
    /// </summary>
    public class DeviceManager : MonoBehaviour
    {
        #region -------- シングルトンの設定 --------

        // シングルトンインスタンス
        public static DeviceManager Instance { get; private set; }

        private void Awake()
        {
            // すでにインスタンスが存在する場合は破棄
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // シングルトンをセット
            Instance = this;

            // シーンをまたいで破棄されないようにする
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        // 接続・切断時のメッセージ
        private const string GAMEPAD_IN_MESSAGE = "コントローラーが接続されました";
        private const string GAMEPAD_OUT_MESSAGE = "コントローラーが切断されました";

        // メッセージ表示用
        private SystemMessageManager _systemMessage;

        // 現在接続されているデバイスがゲームパッドかどうか
        public bool isGamepad { get; private set; }

        #region -------- イベント登録 / 解除 --------

        private void OnEnable()
        {
            // デバイス変更イベントに登録
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        private void OnDisable()
        {
            // デバイス変更イベントから解除
            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        #endregion

        /// <summary>
        /// ゲーム開始時のデバイス確認
        /// - 接続済みのゲームパッドがある場合はメッセージ表示
        /// - SystemMessageManager が存在しない場合は警告ログ出力
        /// </summary>
        private void Start()
        {
            // SystemMessageManager が未セットの場合は探索
            if (_systemMessage == null)
            {
                GameObject obj = GameObject.Find(GameConstants.Object.SYSTEM_MESSAGE);
                if (obj != null)
                {
                    _systemMessage = obj.GetComponent<SystemMessageManager>();
                }
                else
                {
                    Debug.LogWarning($"SystemMessageオブジェクトが見つかりません: {GameConstants.Object.SYSTEM_MESSAGE}");
                    return; // 無ければ処理をスキップ
                }
            }

            // 現在接続されているデバイスを確認
            foreach (var device in InputSystem.devices)
            {
                if (device is Gamepad)
                {
                    isGamepad = true;
                    _systemMessage.DrawMessage(GAMEPAD_IN_MESSAGE);
                    break; // 最初のゲームパッドだけ確認すれば十分
                }
            }
        }

        /// <summary>
        /// デバイス接続・切断時に呼ばれるコールバック
        /// - nullチェックを行い、SystemMessageManager が存在しなければ処理をスキップ
        /// - 接続なら isGamepad を true に、切断なら false に設定
        /// </summary>
        /// <param name="device">接続・切断されたデバイス</param>
        /// <param name="change">デバイス変更の種類</param>
        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            // SystemMessageManager が未セットの場合は探索
            if (_systemMessage == null)
            {
                GameObject obj = GameObject.Find(GameConstants.Object.SYSTEM_MESSAGE);
                if (obj != null)
                {
                    _systemMessage = obj.GetComponent<SystemMessageManager>();
                }
                else
                {
                    Debug.LogWarning($"SystemMessageオブジェクトが見つかりません: {GameConstants.Object.SYSTEM_MESSAGE}");
                    return; // 無ければ処理をスキップ
                }
            }

            // 接続・切断に応じて処理
            if (change == InputDeviceChange.Added)
            {
                isGamepad = true;
                _systemMessage.DrawMessage(GAMEPAD_IN_MESSAGE);
            }
            else if (change == InputDeviceChange.Removed)
            {
                isGamepad = false;
                _systemMessage.DrawMessage(GAMEPAD_OUT_MESSAGE);
            }
        }
    }
}
