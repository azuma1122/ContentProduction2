using UnityEngine;

namespace Game
{
    /// <summary>
    /// ゲーム全体での定数管理用
    /// </summary>
    public static class GameConstants
    {
        /* ================ シーン ================ */

        public enum Scene
        {
            Title = 0,          // Build Index: 0
            Setting = 1,        // Build Index: 1
            Options = 2,        // Build Index: 2
            Loading = 3,        // Build Index: 3
            StageSelect = 4,    // Build Index: 4
            Stage1 = 5,         // Build Index: 5
            Clear = 6,          // Build Index: 6
            Stage2 = 7,         // Build Index: 7
            Stage3 = 8,         // Build Index: 8
            Credits = 10,       // Build Index: 10 (9はStageで削除予定)
            Max,
        }

        /* ================ タグ ================ */

        public static class Tag
        {
            public const string UNTAGGED = "Untagged";
            public const string FIXED = "Fixed";
            public const string MOVING = "Moving";
            public const string STAGE = "Stage";
            public const string PLAYER = "Player";
        }

        /* ================ レイヤー ================ */

        /// <summary>
        /// ゲーム内のタグを定義する列挙型
        /// 【int型にキャストして使用する】
        /// </summary>
        public enum Layer
        {
            DEFAULT = 0,         // すべてのオブジェクトのデフォルト
            TRANSPARENT_FX = 1,  // 透明エフェクト用
            IGNORE_RAYCAST = 2,  // レイキャストを無視する

            WATER = 4,           // 水の表現用
            UI = 5,              // ユーザーインターフェース
            N_MAGNET = 6,        // 磁力 N極
            S_MAGNET = 7,        // 磁力 S極
            BULLET = 8,          // 磁力の弾
            MAGNET_RANGE = 9,    // 磁力の範囲
        }

        /* ================ オブジェクト ================ */

        public static class Object
        {
            public const string MAIN_STAGE = "MainStage(Clone)";

            public const string STAGE_SELECT = "StageSelectSceneManager";
            public const string STAGE = "StageSceneManager";

            public const string INPUT = "InputManager";
            public const string SYSTEM_MESSAGE = "SystemMessagePrefab";
            public const string DEVICE_MANAGER = "DeviceManager";

            public const string BGM_MANAGER = "BGMManager";
            public const string SE_MANAGER = "SEManager";
            public const string TITLE_SCENE_MANAGER = "TitleSceneManager";
            public const string GOAL_CRYSTAL = "Crystal_Model_Prefab(Clone)";

            public const string STAGE_LOADER = "StageLoader";
        }

        public const string MAIN_CAMERA = "Main Camera";

        // サウンド管理用オブジェクト
        public const string SOUND_MANAGER_OBJ = "SoundManager";

        // 磁力管理用オブジェクト
        public const string MAGNET_MANAGER_OBJ = "MagnetManager";

        // ステージロード用オブジェクト
        public const string STAGE_LOADER_OBJ = "StageLoader";

        // 生成するプレイヤーオブジェクト
        public const string PLAYER_OBJ = "MagForce_Prefab(Clone)";

        /* ================ ステージ ================ */

        // ステージの最大数
        public const int STAGE_MAX_NUM = 10;

        // ステージ名
        public enum Stage
        {
            Stage_Select,
            Stage_1,
            Stage_2,
            Stage_3,
            Stage_4,
            Stage_5,
            Stage_6,
            Stage_7,
            Stage_8,
            DebugStage,
        }

        /* ================ 時間 ================ */

        // 標準値
        public const int DEFAULT_FPS = 60;

        public const int HIGH_FPS = 120;

        /* ================ 座標 ================ */

        public static readonly Vector3 LowerLeft = new Vector3(-6.5f, -3.0f, 0.0f);        // 左下最小座標
        public static readonly Vector3 TopRight = new Vector3(29.5f, 19.0f, 0.0f);         // 右上最大座標

        public static readonly Vector3 LowerLeftCamera = new Vector3(1.0f, 1.0f, -10.0f);  // カメラの左下最小座標
        public static readonly Vector3 TopRightCamera = new Vector3(22.5f, 16.5f, -10.0f); // カメラの右上最大座標

        /* ================ 数値計算 ================ */

        // 半分計算
        public const float HAFE = 2.0f;

        // 反転値
        public const float INVERSION = -1.0f;

        /* ================ 状態管理 ================ */

        // 値のリセット
        public const float RESET = 0.0f;

        // 正常動作の判定用
        public const int NORMAL = 0;

        // エラー動作の判定用
        public const int ERROR = -1;
    }
}