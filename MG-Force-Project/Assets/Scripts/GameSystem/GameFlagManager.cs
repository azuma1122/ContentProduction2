using UnityEngine;

namespace Game
{
    /// <summary>
    /// ゲーム全体で使用するフラグ（状態）を管理するクラス
    /// - シングルトンとして 1 つだけ存在
    /// - シーンごとのチェックフラグ
    /// - エラー種別ごとのフラグ
    /// - DontDestroyOnLoad によりシーンが変わっても保持される
    /// </summary>
    public class GameFlagManager : MonoBehaviour
    {
        #region -------- シングルトンの設定 --------

        /// <summary>
        /// グローバルアクセス用の唯一のインスタンス
        /// </summary>
        public static GameFlagManager Instance { get; private set; }

        private void Awake()
        {
            // すでに存在する場合は重複生成を防ぐため破棄
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            // インスタンスとして登録
            Instance = this;

            // シーンを跨いでも破棄されないようにする
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        /// <summary>
        /// エラー種別を表す列挙体
        /// - INPUT   : 入力系の異常
        /// - LOAD    : データ読み込み等の異常
        /// - EVENT   : ゲーム内イベント関連
        /// - SCENE   : シーン遷移などの異常
        /// - PLAYER  : プレイヤー関連
        /// - MAGNET  : 磁力システム関連
        /// - DEBUG   : デバッグ用フラグ
        /// </summary>
        public enum ErrorFlag
        {
            INPUT,
            LOAD,
            EVENT,
            SCENE,
            PLAYER,
            MAGNET,
            DEBUG,

            MAX,    // 配列サイズ管理用
        }

        #region -------- シーンフラグの管理 --------

        /// <summary>
        /// シーンごとのフラグ（NamedSerializeField によりインスペクタに名前表示）
        /// - TitleScene
        /// - StageSelectScene
        /// - StageScene
        /// - ClearScene
        ///
        /// GameConstants.Scene の Max 値に合わせて配列サイズを維持
        /// </summary>
        [NamedSerializeField(
            new string[]
            {
                "TitleScene",
                "StageSelectScene",
                "StageScene",
                "ClearScene",
            }
        )]
        [SerializeField]
        private bool[] CheckSceneFlag = new bool[(int)GameConstants.Scene.Max];

        #endregion

        #region -------- エラーフラグの管理 --------

        /// <summary>
        /// エラー種別ごとのフラグ
        /// - NamedSerializeField によって各項目の名前をインスペクタに表示
        /// </summary>
        [NamedSerializeField(
            new string[]
            {
                "Input",
                "Load",
                "Event",
                "Scene",
                "Player",
                "Magnet",
                "Debug",
            }
        )]
        [SerializeField]
        private bool[] CheckErrorFlag = new bool[(int)ErrorFlag.MAX];

        #endregion

        private void Update()
        {
            // ここにはビルド版とエディタ版の終了処理コードが置かれている
            // 現在はコメントアウトされているため何も動作しない

            /*
            #if UNITY_EDITOR
                // エディターの実行停止
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                // 実行中アプリケーションを終了
                Application.Quit();
            #endif
            */
        }

        #region -------- フラグ操作用メソッド --------

        /// <summary>
        /// 任意のシーンの状態フラグをセットする
        /// </summary>
        public void SetFlag(GameConstants.Scene scene, bool truth)
        {
            CheckSceneFlag[(int)scene] = truth;
        }

        /// <summary>
        /// 任意のエラーフラグをセットする
        /// </summary>
        public void SetFlag(ErrorFlag error, bool truth)
        {
            CheckErrorFlag[(int)error] = truth;
        }

        /// <summary>
        /// シーンの状態フラグを取得する
        /// </summary>
        public bool GetFlag(GameConstants.Scene scene)
        {
            return CheckSceneFlag[(int)scene];
        }

        /// <summary>
        /// エラー状態フラグを取得する
        /// </summary>
        public bool GetFlag(ErrorFlag error)
        {
            return CheckErrorFlag[(int)error];
        }

        #endregion
    }
}
