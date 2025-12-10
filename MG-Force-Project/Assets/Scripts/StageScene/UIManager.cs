using Game.GameSystem;
using UnityEngine;

namespace Game.StageScene
{
    /// <summary>
    /// ステージ内のUI表示を管理するクラス
    /// - プレイヤーが使用している入力デバイス（キーボード/ゲームパッド）を検出
    /// - デバイスに応じた操作ガイド（KeyBar）を自動で切り替える
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // 入力制御を管理するクラス（キーボード/ゲームパッド判定用）
        private InputHandler _inputHandler;

        // 各デバイス用の操作ガイド表示オブジェクト
        [SerializeField] private GameObject _gamepadKeyBar;    // ゲームパッド用ガイド
        [SerializeField] private GameObject _gamepad_2KeyBar;  // ゲームパッド2用ガイド（別レイアウト）
        [SerializeField] private GameObject _keyboardKeyBar;   // キーボード用ガイド

        // 現在使用中のデバイスを記憶（変更検出用）
        private string _currentDevice;

        /// <summary>
        /// 初期化処理
        /// - InputHandlerを取得
        /// - 現在のデバイスを検出してガイド表示を設定
        /// </summary>
        private void Start()
        {
            // ===== InputHandlerの取得を試みる =====
            // まずSingletonのInstanceから取得
            _inputHandler = InputHandler.Instance;

            // InputHandlerが取得できなかった場合の対処
            if (_inputHandler == null)
            {
                Debug.LogWarning("InputHandler.Instance が見つかりません。GameObject.Find で検索します。");

                // シーン内から名前で検索して取得を試みる
                GameObject inputObj = GameObject.Find(GameConstants.Object.INPUT);
                if (inputObj != null)
                {
                    _inputHandler = inputObj.GetComponent<InputHandler>();
                }

                // それでも見つからなければエラー処理
                if (_inputHandler == null)
                {
                    Debug.LogError("InputHandler が見つかりません。デフォルトでキーボード表示にします。");
                    // デフォルトでキーボード表示に設定
                    _currentDevice = InputConstants.ActionDevice.KEY_MOUSE;
                    SetKeyBar();
                    return;
                }
            }

            // ===== 初期デバイスを検出して表示を設定 =====
            // 現在使用中のデバイスを取得（"Keyboard", "Gamepad"など）
            _currentDevice = _inputHandler.GetControlScheme();

            // デバイスに応じたガイドを表示
            SetKeyBar();
        }

        /// <summary>
        /// 毎フレーム実行される処理
        /// - デバイスの切り替えを監視
        /// - 変更があれば表示ガイドを切り替える
        /// </summary>
        private void Update()
        {
            // ===== InputHandlerが存在しない場合は何もしない =====
            if (_inputHandler == null)
            {
                return;
            }

            // ===== 現在のデバイスを取得 =====
            string newDevice = _inputHandler.GetControlScheme();

            // ===== デバイスが変更されていない場合は処理をスキップ =====
            // 毎フレームSetKeyBar()を呼ぶとパフォーマンスが悪いため、
            // 変更があった時だけ更新する
            if (_currentDevice == newDevice)
            {
                return;
            }

            // ===== デバイス変更を検出！ =====
            // 例: キーボード → ゲームパッド に切り替わった
            _currentDevice = newDevice;

            // 新しいデバイスに応じたガイドを表示
            SetKeyBar();
        }

        /// <summary>
        /// 現在のデバイスに応じて操作ガイド（KeyBar）の表示を切り替える
        /// - ゲームパッド → ゲームパッド用ガイドのみ表示
        /// - キーボード → キーボード用ガイドのみ表示
        /// </summary>
        private void SetKeyBar()
        {
            // ===== SerializeFieldのnullチェック =====
            // Inspector で設定し忘れている場合の安全対策
            if (_gamepadKeyBar == null || _gamepad_2KeyBar == null || _keyboardKeyBar == null)
            {
                Debug.LogWarning("KeyBar のいずれかが Inspector で設定されていません。");
                return;
            }

            // ===== デバイスタイプに応じて表示/非表示を切り替え =====
            switch (_currentDevice)
            {
                // ----- ゲームパッド（タイプ1）を使用中 -----
                case InputConstants.ActionDevice.GAMEPAD:
                    _gamepadKeyBar.SetActive(true);      // ゲームパッド用を表示
                    _gamepad_2KeyBar.SetActive(false);   // ゲームパッド2用を非表示
                    _keyboardKeyBar.SetActive(false);    // キーボード用を非表示
                    break;

                // ----- ゲームパッド（タイプ2）を使用中 -----
                case InputConstants.ActionDevice.GAMEPAD_2:
                    _gamepadKeyBar.SetActive(false);     // ゲームパッド用を非表示
                    _gamepad_2KeyBar.SetActive(true);    // ゲームパッド2用を表示
                    _keyboardKeyBar.SetActive(false);    // キーボード用を非表示
                    break;

                // ----- キーボード＆マウスを使用中 -----
                case InputConstants.ActionDevice.KEY_MOUSE:
                    _gamepadKeyBar.SetActive(false);     // ゲームパッド用を非表示
                    _gamepad_2KeyBar.SetActive(false);   // ゲームパッド2用を非表示
                    _keyboardKeyBar.SetActive(true);     // キーボード用を表示
                    break;

                // ----- 未知のデバイスタイプ（予期しない値） -----
                default:
                    Debug.LogWarning($"未知のデバイスタイプ: {_currentDevice}");
                    // 安全のため、デフォルトでキーボード表示にする
                    _gamepadKeyBar.SetActive(false);
                    _gamepad_2KeyBar.SetActive(false);
                    _keyboardKeyBar.SetActive(true);
                    break;
            }
        }
    }
}