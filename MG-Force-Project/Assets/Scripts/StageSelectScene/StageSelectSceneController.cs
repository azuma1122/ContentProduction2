using UnityEngine;
using Game.GameSystem;

namespace Game.StageScene
{
    /// <summary>
    /// ステージ選択シーン全体を管理するクラス
    /// - 研究室（Lab）画面・メニュー画面・ステージ選択画面の切り替えを制御
    /// - ショートカット入力によるシーン遷移
    /// - クリスタルのゴールイベント検知でクリアシーンへ遷移
    /// </summary>
    public class StageSelectSceneController : MonoBehaviour
    {
        private InputHandler _inputHandler;       // 入力処理を管理するクラスの参照
        private SceneLoader _sceneLoader;         // シーン遷移を管理するクラスの参照

        [SerializeField] private GameObject[] _labObject;      // 研究室（Lab）画面に関連するオブジェクト群
        [SerializeField] private GameObject _menuObject;       // メニュー画面の親オブジェクト
        [SerializeField] private GameObject _stageSelectObject; // ステージ選択画面の親オブジェクト

        public bool isMenuScreen { get; private set; }          // 現在メニュー画面かどうか
        public bool isStageSelectScreen { get; private set; }   // 現在ステージ選択画面かどうか

        private CrystalController _crystalController;           // クリスタル（ステージ選択用オブジェクト）を制御するクラス

        private void Awake()
        {
            // 各マネージャークラスのインスタンス取得
            _inputHandler = InputHandler.Instance;
            _sceneLoader = SceneLoader.Instance;

            // 初期状態：Lab画面を有効に、他を無効に設定
            SetActive(true, false, false);

            // クリスタルオブジェクトの参照を取得
            _crystalController = GameObject.Find("Crystal_Model_Prefab(Clone)").GetComponent<CrystalController>();
        }

        private void Update()
        {
            // 現在がステージ選択画面なら、その更新処理を実行
            if (isStageSelectScreen)
            {
                StageSelectScreenUpdate();
            }
            else
            {
                // ショートカットキーの確認（シーン切り替え等）
                ShortCutCheck();

                // 現在メニュー画面なら、その更新処理を実行
                if (isMenuScreen)
                {
                    MenuScreenUpdate();
                }
            }

            // クリスタルがゴールイベントを発生させた場合 → クリアシーンへ遷移
            if (_crystalController.IsGoalEvent)
            {
                _sceneLoader.LoadScene(GameConstants.Scene.Clear.ToString());
            }
        }

        /// <summary>
        /// ステージ選択画面中の更新処理
        /// </summary>
        private void StageSelectScreenUpdate()
        {
            // ステージ選択画面が非アクティブなら有効化
            if (!_stageSelectObject.activeSelf) { _stageSelectObject.SetActive(true); }

            // 戻るボタンが押されたらLab画面へ戻る
            if (_inputHandler.IsActionPressed(InputConstants.Action.MENU_BACK))
            {
                SetActive(true, false, false);
            }
        }

        /// <summary>
        /// メニュー画面中の更新処理
        /// </summary>
        private void MenuScreenUpdate()
        {
            // メニューが非アクティブなら有効化
            if (!_menuObject.activeSelf) { _menuObject.SetActive(true); }
        }

        /// <summary>
        /// ショートカットキーの入力チェック
        /// - シーン遷移や画面切り替えに使用
        /// </summary>
        private void ShortCutCheck()
        {
            // ショートカット1 → タイトルシーンへ
            if (_inputHandler.IsActionPressed(InputConstants.Action.SHORTCUT_1))
            {
                _sceneLoader.LoadScene(GameConstants.Scene.Title.ToString());
            }
            // ショートカット2 → ステージ選択シーンへ
            else if (_inputHandler.IsActionPressed(InputConstants.Action.SHORTCUT_2))
            {
                _sceneLoader.LoadScene(GameConstants.Scene.StageSelect.ToString());
            }
            // ショートカット3 → ステージ選択画面を開く
            else if (_inputHandler.IsActionPressed(InputConstants.Action.SHORTCUT_3))
            {
                SetActive(false, false, true);
            }
            // ショートカット4 → メニューの開閉切り替え
            else if (_inputHandler.IsActionPressed(InputConstants.Action.SHORTCUT_4))
            {
                isMenuScreen = !isMenuScreen;
            }
        }

        /// <summary>
        /// 各画面（Lab / Menu / StageSelect）の表示・非表示を切り替える
        /// </summary>
        /// <param name="lab">Lab画面を有効にするか</param>
        /// <param name="menu">Menu画面を有効にするか</param>
        /// <param name="stage_select">StageSelect画面を有効にするか</param>
        private void SetActive(bool lab, bool menu, bool stage_select)
        {
            // 研究室画面オブジェクト群の有効状態を設定
            for (int i = 0; i < _labObject.Length; i++)
            {
                _labObject[i].SetActive(lab);
            }

            // 各画面の有効状態を反映
            _menuObject.SetActive(menu);
            _stageSelectObject.SetActive(stage_select);
        }
    }
}
