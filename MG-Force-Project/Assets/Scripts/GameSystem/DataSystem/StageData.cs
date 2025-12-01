using UnityEngine;


    public class StageData : ScriptableObject
    {
        // ステージの管理番号
        [SerializeField]
        private int stageIndex;

        // ステージのプレハブ
        [SerializeField]
        private GameObject stagePrefab;

        // ステージの背景
        [SerializeField]
        private GameObject stageBG;

        // ステージの右上座標
        [SerializeField]
        private Vector2 topRight;

        /* -------- 読み取り用の変数 -------- */

        // ステージの管理番号
        public int StageIndex => stageIndex;

        // ステージのプレハブ
        public GameObject StagePrefab => stagePrefab;

        // ステージの背景
        public GameObject StageBG => stageBG;

        // ステージの右上座標
        public Vector2 TopRight => topRight;
    }
