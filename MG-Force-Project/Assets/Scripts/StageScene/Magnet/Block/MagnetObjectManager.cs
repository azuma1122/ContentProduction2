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

        // 磁力システム全体を管理するクラス
        protected MagnetManager magnetManager;

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

            // シーン内の MagnetManager オブジェクトを取得
            magnetManager = GameObject.Find(GameConstants.MAGNET_MANAGER_OBJ).GetComponent<MagnetManager>();

            // 磁力動作制御用クラスを生成
            magnetController = new MagnetController();

            // 固定磁石（例：壁や床など）の場合
            if (magnetFixed)
            {
                string new_object_type = gameObject.tag; // タグからオブジェクト種別を取得
                MagnetData.MagnetType new_magnet_type = (MagnetData.MagnetType)gameObject.layer; // レイヤーから磁極を取得

                // 固定磁石用の磁力データを生成
                MyData = new MagnetData(new_object_type, new_magnet_type, magnetFixedPower);
            }
            else
            {
                // 通常のオブジェクトはデフォルトの磁力データを生成
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
            // コライダーは基本的に常に有効化しておく
            _magnetCollider.SetActive(true);

            if (magnetManager == null) return;

            // MagnetManager から磁力起動状態を確認
            if (magnetManager.IsMagnetBoot)
            {
                // 起動中にコライダーが無効なら再度ON
                if (!_magnetCollider.activeSelf)
                {
                    _magnetCollider.SetActive(true);
                }

                return; // 起動中は以降の処理をスキップ
            }

            // MagnetBootがOFFになった場合は磁力判定を無効化
            if (_magnetCollider.activeSelf)
            {
                _magnetCollider.SetActive(false);
            }

            // 固定磁石は入力によるリセット対象外
            if (magnetFixed) return;

            // リセットキーが押された場合
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
            // 初期状態（無磁力）を設定
            MagnetData.MagnetType reset_type = MagnetData.MagnetType.NotType;
            MagnetData.MagnetPower reset_power = MagnetData.MagnetPower.None;

            // レイヤーを「NotType」に変更
            gameObject.layer = (int)reset_type;

            // MagnetDataをリセット状態に更新
            MyData.SetMagnetData(reset_type, reset_power);

            // UI更新処理（磁力ゲージやアイコンをリセット）
            MagnetUIManager ui = GameObject.Find("MagnetUIManager").GetComponent<MagnetUIManager>();
            ui.Reset();

            DebugManager.LogMessage("リセットしました");
        }

        /// <summary>
        /// トリガー衝突時の処理  
        /// 弾が当たった時に磁力の種類と強さを付与する
        /// </summary>
        protected virtual void OnTriggerEnter(Collider other)
        {
            // 固定磁石には弾の影響を与えない
            if (magnetFixed) return;

            // 弾（Bullet）に当たった場合
            if (other.gameObject.layer == (int)GameConstants.Layer.BULLET)
            {
                // 弾の持つ磁力情報を適用
                gameObject.layer = (int)magnetManager.CurrentType;

                // 新しい磁力情報を取得
                MagnetData.MagnetType new_magnet_type = (MagnetData.MagnetType)gameObject.layer;
                MagnetData.MagnetPower new_magnet_power = (MagnetData.MagnetPower)magnetManager.CurrentPower;

                // MagnetDataを更新
                MyData.SetMagnetData(new_magnet_type, new_magnet_power);

                DebugManager.LogMessage(MyData.MyMangetType.ToString() + " | " + MyData.MyMagnetPower.ToString());
            }
        }

        /// <summary>
        /// オブジェクトの磁力強度を外部から設定する  
        /// （例えばエディタやスクリプトから呼び出し可能）
        /// </summary>
        public void SetObjectPower(int power)
        {
            switch (power)
            {
                case (int)MagnetData.MagnetPower.Weak:
                    magnetFixedPower = MagnetData.MagnetPower.Weak;
                    return;

                case (int)MagnetData.MagnetPower.Medium:
                    magnetFixedPower = MagnetData.MagnetPower.Medium;
                    return;

                case (int)MagnetData.MagnetPower.Strong:
                    magnetFixedPower = MagnetData.MagnetPower.Strong;
                    return;
            }
        }
    }
}
