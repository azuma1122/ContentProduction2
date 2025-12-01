using UnityEngine;
using Game.GameSystem;

namespace Game.StageScene
{
    /// <summary>
    /// ステージ選択ボタンの処理を管理するクラス
    /// - ボタン押下時に選択したステージ番号を保存し、対応するシーンをロードする
    /// </summary>
    public class StageSelectButtonManager : MonoBehaviour
    {
        private GameDataManager _gameDataManager;  // ゲーム全体のデータ（ステージ進行など）を管理するクラス
        private SceneLoader _sceneLoader;          // シーン遷移を行うクラス

        /// <summary>
        /// 初期化処理
        /// - シングルトンインスタンスを取得
        /// </summary>
        private void Start()
        {
            _gameDataManager = GameDataManager.Instance;
            _sceneLoader = SceneLoader.Instance;
        }

        /// <summary>
        /// ステージ選択ボタンが押されたときに呼ばれる処理
        /// - 引数として渡されたステージ番号を現在のステージとして記録
        /// - ステージシーンを読み込む
        /// </summary>
        /// <param name="stage_index">選択されたステージのインデックス番号</param>
        public void StageSelect(int stage_index)
        {
            // GameDataManagerが正常に取得できているか確認
            if (_gameDataManager != null)
            {
                // 現在のステージ番号を設定
                _gameDataManager.SetCurrentStageIndex(stage_index);

                // ステージシーンをロード（GameConstants.Scene.Stage）
                _sceneLoader.LoadScene(GameConstants.Scene.Stage.ToString());
            }
            else
            {
                // GameDataManagerが存在しない場合はエラーログを出力
                DebugManager.LogMessage("GameDataManagerが見つかりません", DebugManager.MessageType.Error);
            }
        }
    }
}
