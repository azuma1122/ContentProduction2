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
                    return;
                }
            }

            // 磁力動作制御用クラスを生成
            magnetController = new MagnetController();

            if (magnetFixed)
            {
                string new_object_type = gameObject.tag; // タグからオブジェクト種別を取得
                MagnetData.MagnetType new_magnet_type = (MagnetData.MagnetType)gameObject.layer; // レイヤーから磁極を取得
                MyData = new MagnetData(new_object_type, new_magnet_type, magnetFixedPower);
            }
            else
            {
                string new_object_type = gameObject.tag;
                MyData = new MagnetData(new_object_type);
            }
        }

        /// <summary>
        /// 毎フレーム更新処理  
        /// 磁力ON/OFF制御や入力によるリセット処理を行う
        /// </summary>
        protected virtual void Update()
        {
            if (_magnetCollider != null)
            {
                _magnetCollider.SetActive(true);
            }

            if (magnetManager == null) return;

            // MagnetManager から磁力起動状態を確認
            if (magnetManager.IsMagnetBoot)
            {
                if (_magnetCollider != null && !_magnetCollider.activeSelf)
                {
                    _magnetCollider.SetActive(true);
                }
                return;
            }

            // MagnetBootがOFFになった場合は磁力判定を無効化
            if (_magnetCollider != null && _magnetCollider.activeSelf)
            {
                _magnetCollider.SetActive(false);
            }

            if (magnetFixed) return;

            if (input.IsActionPressed(InputConstants.Action.RESET))
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

            if (other.gameObject.layer == (int)GameConstants.Layer.BULLET && magnetManager != null)
            {
                gameObject.layer = (int)magnetManager.CurrentType;
                MagnetData.MagnetType new_magnet_type = (MagnetData.MagnetType)gameObject.layer;
                MagnetData.MagnetPower new_magnet_power = (MagnetData.MagnetPower)magnetManager.CurrentPower;
                MyData.SetMagnetData(new_magnet_type, new_magnet_power);

                DebugManager.LogMessage(MyData.MyMangetType.ToString() + " | " + MyData.MyMagnetPower.ToString());
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
                    magnetFixedPower = MagnetData.MagnetPower.Weak;
                    break;
                case (int)MagnetData.MagnetPower.Medium:
                    magnetFixedPower = MagnetData.MagnetPower.Medium;
                    break;
                case (int)MagnetData.MagnetPower.Strong:
                    magnetFixedPower = MagnetData.MagnetPower.Strong;
                    break;
            }
        }
    }
}
