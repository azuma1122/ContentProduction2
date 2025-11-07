using UnityEngine;

namespace Game
{
    /// <summary>
    /// 入力システム(新InputSystem)で使用する文字列定数をまとめたクラス  
    /// - デバイスの種類  
    /// - Action Map（入力のグループ）  
    /// - 個別のアクション名  
    /// - 方向入力のベクトル値  
    /// などを管理しており、文字列の打ち間違い防止・保守性向上に役立つ
    /// </summary>
    public static class InputConstants
    {
        /// <summary>
        /// ゲーム機や入力デバイスの種類を識別するための定数
        /// </summary>
        public static class Device
        {
            public const string SWITCH = "switch";
            public const string PLAY_STATION = "playstation";
            public const string XBOX = "xbox";
        }

        /// <summary>
        /// Input Systemにおける「アクションを行うデバイスの種類」  
        /// （KeyMouse や Gamepad などの区別に使用）
        /// </summary>
        public static class ActionDevice
        {
            public const string KEY_MOUSE = "KeyMouse";
            public const string GAMEPAD = "Gamepad";
            public const string GAMEPAD_2 = "Gamepad_2"; // 2P目などに使用可能
        }

        /// <summary>
        /// Input Actionsで設定する「Action Map名」  
        /// 操作系統（Player / Magnet / Camera / Menu など）を分類
        /// </summary>
        public static class ActionMaps
        {
            public const string PLAYER_MAPS = "Player";
            public const string MAGNET_MAPS = "Magnet";
            public const string CAMERA_MAPS = "Camera";
            public const string MENU_MAPS = "Menu";

            public const string SHORTCUT_MAPS = "Shortcut";
            public const string DEBUG_MAPS = "Debug"; // デバッグ専用操作
        }

        /// <summary>
        /// 各Action Mapの中に含まれる「個別アクション名」を定義  
        /// InputActionのNameと完全一致している必要がある
        /// </summary>
        public static class Action
        {
            // ==== Player Action ====
            public const string ACTION = "Action";               // 汎用アクションボタン
            public const string LEFTMOVE = "PlayerLeftMove";     // 左移動
            public const string RIGHTMOVE = "PlayerRightMove";   // 右移動
            public const string JUMP = "Jump";                   // ジャンプ
            public const string MENU_OPEN = "MenuOpen";          // メニューを開く
            public const string VIEW_MODE_START = "ViewModeStart";// 視点操作開始
            public const string MAGNET_BOOT = "MagnetBoot";      // 磁力装置の起動

            // ==== Magnet Action ====
            public const string POLE_SWITCHING = "PoleSwitching"; // 磁極切り替え
            public const string MAGNET_POWER = "PowerCharge";     // 磁力チャージ
            public const string SHOOT = "Shoot";                  // 射撃ボタン（押し続け可）
            public const string SHOOT_ANGLE = "ShootAngle";       // 射撃方向入力
            public const string SHOOT_CANCEL = "ShootCancel";     // 射撃キャンセル
            public const string RESET = "Reset";                  // プレイヤー位置リセットなど

            // ==== Camera Action ====
            public const string VIEW_MODE_END = "ViewModeEnd";    // 視点操作終了
            public const string VIEW_MOVE_LEFT = "CameraLeftMove";
            public const string VIEW_MOVE_RIGHT = "CameraRightMove";
            public const string VIEW_MOVE_UP = "CameraUpMove";
            public const string VIEW_MOVE_DOWN = "CameraDownMove";

            // ==== Menu Action ====
            public const string MENU_CLOSE = "Close";
            public const string MENU_DECISION = "Decision";       // 決定ボタン
            public const string MENU_BACK = "Back";               // 戻る
            public const string MENU_LEFT_SELECT = "LeftSelect";
            public const string MENU_RIGHT_SELECT = "RightSelect";
            public const string MENU_UP_SELECT = "UpSelect";
            public const string MENU_DOWN_SELECT = "DownSelect";

            // ==== Shortcut (クイック操作) ====
            public const string SHORTCUT_1 = "ShortCut_1";
            public const string SHORTCUT_2 = "ShortCut_2";
            public const string SHORTCUT_3 = "ShortCut_3";
            public const string SHORTCUT_4 = "ShortCut_4";

#if UNITY_EDITOR
            // ==== Debug（開発中だけ有効） ====
            public const string DEBUG_NEXT = "NextUpdate";    // 次ステップへ進む
            public const string DEBUG_RENEXT = "ReUpdate";    // 一つ前へ戻る
            public const string DEBUG_RESET = "ReSet";        // 状態リセット
#endif
            public const string DEBUG_CREDITS = "CreditsMove"; // クレジット移動（製品版でも使える可能性）
        }

        /// <summary>
        /// 入力方向（アナログスティックやキー組み合わせ用）をVector2で表現  
        /// 斜め方向も対応可能
        /// </summary>
        public static class ActionVector
        {
            public static readonly Vector2 North = new Vector2(0.0f, 1.0f);
            public static readonly Vector2 South = new Vector2(0.0f, -1.0f);
            public static readonly Vector2 West = new Vector2(-1.0f, 0.0f);
            public static readonly Vector2 East = new Vector2(1.0f, 0.0f);

            public static readonly Vector2 NorthWest = new Vector2(-1.0f, 1.0f);
            public static readonly Vector2 NorthEast = new Vector2(1.0f, 1.0f);
            public static readonly Vector2 SouthWest = new Vector2(-1.0f, -1.0f);
            public static readonly Vector2 SouthEast = new Vector2(1.0f, -1.0f);
        }
    }
}
