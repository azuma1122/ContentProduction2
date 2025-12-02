using Game.GameSystem;
using UnityEngine;

namespace Game.StageScene.Magnet
{
    /// <summary>
    /// オブジェクトの磁力管理クラス  
    /// ・磁力オブジェクトに関する状態を保持・更新  
    /// ・磁力ON/OFFやリセット、弾による磁力付与などを管理
    /// </summary>
    public class MagnetObjectManager : MonoBehaviour
    {
        private InputHandler input; // プレイヤー入力処理クラスの参照

        /// <summary>
        /// このオブジェクトの磁力データ  
        /// （磁極の種類や強さなどを保持）
        /// </summary>
        public MagnetData MyData { get; protected set; }

        // 磁力システム全体を管理するクラス（Inspector から設定可）
        [SerializeField] protected MagnetManager magnetManager;

        // 磁力の動作制御（吸着・反発など）を行うクラス
        protected MagnetController magnetController;

        // 磁力判定用のコライダー（当たり判定専用）
        [SerializeField] private GameObject _magnetCollider;

        // このオブジェクトが固定磁石かどうか
        [SerializeField] protected bool magnetFixed;

        // 固定磁石の磁力強度
        [SerializeField] protected MagnetData.MagnetPower magnetFixedPower;

        /// <summary>
        /// 初期化処理  
        /// ゲーム開始時に磁力データを生成・設定する
        /// </summary>
        protected virtual void Start()
        {
            try
            {
                // 入力ハンドラ取得（nullでも例外出さない）
                input = InputHandler.Instance;

                // MagnetManager が Inspector に設定されていない場合は Find で取得
                if (magnetManager == null)
                {
                    var obj = GameObject.Find(GameConstants.MAGNET_MANAGER_OBJ);
                    if (obj != null)
                    {
                        magnetManager = obj.GetComponent<MagnetManager>();
                        if (magnetManager == null)
                            Debug.LogError("MagnetManager コンポーネントが見つかりません");
                    }
                    else
                    {
                        Debug.LogError($"MagnetManager オブジェクトがシーンに存在しません: {GameConstants.MAGNET_MANAGER_OBJ}");
                    }
                }

                // 磁力動作制御用クラスを生成
                magnetController = new MagnetController();

                // 必ず MyData を生成する
                string new_object_type = gameObject.tag;

                if (magnetFixed)
                {
                    MagnetData.MagnetType new_magnet_type = (MagnetData.MagnetType)gameObject.layer;
                    MyData = new MagnetData(new_object_type, new_magnet_type, magnetFixedPower);
                }
                else
                {
                    MyData = new MagnetData(new_object_type);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"MagnetObjectManager.Start() 中に例外発生: {e}");

                // 落ちても MyData は絶対 null にしない
                if (MyData == null)
                    MyData = new MagnetData(gameObject.tag);
            }
        }

        /// <summary>
        /// 毎フレーム更新処理  
        /// 磁力ON/OFF制御や入力によるリセット処理を行う
        /// </summary>
        protected virtual void Update()
        {
            // Collider は常に安全に扱う
            if (_magnetCollider != null)
                _magnetCollider.SetActive(true);

            // MagnetManager がいない → 処理中断（NRE防止）
            if (magnetManager == null)
                return;

            // MagnetManager から磁力起動状態を確認
            if (magnetManager.IsMagnetBoot)
            {
                if (_magnetCollider != null && !_magnetCollider.activeSelf)
                    _magnetCollider.SetActive(true);

                return;
            }

            // MagnetBoot が OFF の場合 → 判定コライダー切る
            if (_magnetCollider != null && _magnetCollider.activeSelf)
                _magnetCollider.SetActive(false);

            if (magnetFixed) return;

            // input が null でも落ちないようにする
            if (input != null && input.IsActionPressed(InputConstants.Action.RESET))
            {
                ResetMagnet();
            }
        }

        /// <summary>
        /// 磁力リセット処理  
        /// オブジェクトの磁力を初期状態（無磁力）に戻す
        /// </summary>
        private void ResetMagnet()
        {
            if (MyData == null)
                MyData = new MagnetData(gameObject.tag);

            MagnetData.MagnetType reset_type = MagnetData.MagnetType.NotType;
            MagnetData.MagnetPower reset_power = MagnetData.MagnetPower.None;

            gameObject.layer = (int)reset_type;
            MyData.SetMagnetData(reset_type, reset_power);

            var uiObj = GameObject.Find("MagnetUIManager");
            if (uiObj != null)
            {
                var ui = uiObj.GetComponent<MagnetUIManager>();
                if (ui != null) ui.Reset();
            }

            DebugManager.LogMessage("リセットしました");
        }

        /// <summary>
        /// トリガー衝突時の処理  
        /// 弾が当たった時に磁力の種類と強さを付与する
        /// </summary>
        protected virtual void OnTriggerEnter(Collider other)
        {
            if (magnetFixed) return;
            if (MyData == null) return; // 追加：念のため安全化

            if (other.gameObject.layer == (int)GameConstants.Layer.BULLET && magnetManager != null)
            {
                gameObject.layer = (int)magnetManager.CurrentType;

                MagnetData.MagnetType new_magnet_type = (MagnetData.MagnetType)gameObject.layer;
                MagnetData.MagnetPower new_magnet_power = (MagnetData.MagnetPower)magnetManager.CurrentPower;

                MyData.SetMagnetData(new_magnet_type, new_magnet_power);

                DebugManager.LogMessage($"{MyData.MyMangetType} | {MyData.MyMagnetPower}");
            }
        }

        /// <summary>
        /// オブジェクトの磁力強度を外部から設定する
        /// </summary>
        public void SetObjectPower(int power)
        {
            switch (power)
            {
                case (int)MagnetData.MagnetPower.Weak:
                    magnetFixedPower = MagnetData.MagnetPower.Weak; break;
                case (int)MagnetData.MagnetPower.Medium:
                    magnetFixedPower = MagnetData.MagnetPower.Medium; break;
                case (int)MagnetData.MagnetPower.Strong:
                    magnetFixedPower = MagnetData.MagnetPower.Strong; break;
            }
        }
    }
}
