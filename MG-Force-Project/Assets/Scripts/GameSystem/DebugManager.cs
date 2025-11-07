using UnityEngine;

namespace Game
{
    /// <summary>
    /// デバック用
    /// </summary>
    public static class DebugManager
    {
        // メッセージタイプ
        public enum MessageType
        {
            Normal,
            Warning,
            Error,
        }

        /* -------- カテゴリー表示 -------- */

        private const string NORMAL = "実行";

        private const string WARNING = "警告";

        private const string ERROR = "エラー";

        /* -------- 色分け -------- */

        private const string DEFAULT_COLOR = "<color=white>";
        //private const string GREEN_COLOR = "<color=lime>";
        private const string YELLOW_COLOR = "<color=yellow>";
        private const string RED_COLOR = "<color=red>";

        private const string FINISH_COLOR = "</color>";

        /// <summary>
        /// ログメッセージ表示処理
        /// </summary>
        /// <param name="message"></param>
        /// <param name="type"></param>
        /// <param name="class_name"></param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogMessage(string message, MessageType type = MessageType.Normal)
        {
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(1, true);
            System.Diagnostics.StackFrame frame = stackTrace.GetFrame(0);
            string fileName = System.IO.Path.GetFileName(frame.GetFileName() ?? "Unknown File");
            int lineNumber = frame.GetFileLineNumber();
            string callerInfo = $"[{fileName}:{lineNumber}]";

            // タイプによって表示形式を変える
            switch (type) 
            { 
                case MessageType.Normal:
                    Debug.Log($"{DEFAULT_COLOR}【{NORMAL}】{message} {callerInfo}{FINISH_COLOR}");
                    break;

                case MessageType.Warning:
                    Debug.LogWarning($"{YELLOW_COLOR}【{WARNING}】{message} {callerInfo}{FINISH_COLOR}");
                    break;

                case MessageType.Error:
                   // Debug.LogError($"{RED_COLOR}【{ERROR}】{message} {callerInfo}{FINISH_COLOR}");
                    break;
            }
        }
    }
}