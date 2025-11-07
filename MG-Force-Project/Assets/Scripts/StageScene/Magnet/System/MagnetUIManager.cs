using UnityEngine;
using UnityEngine.UI;

namespace Game.StageScene.Magnet
{
    /// <summary>
    /// マグネット関連のUIを管理するクラス
    /// - 磁力のON/OFFやN/S極の切り替えをUIに反映
    /// - ボタン入力によって磁力タイプや起動状態を変更
    /// </summary>
    public class MagnetUIManager : MonoBehaviour
    {
        // MagnetManagerの参照を保持
        private MagnetManager Magnet;

        /// <summary>
        /// 管理するUI要素の種類
        /// </summary>
        private enum UI
        {
            EnergyGage, // エネルギー残量ゲージ
            Boot_ON,    // 磁力ON時の表示UI
            Boot_OFF,   // 磁力OFF時の表示UI
            N_Magnet,   // N極のUI表示
            S_Magnet,   // S極のUI表示

            MAX,        // UI要素の最大数（配列サイズ用）
        }

        /// <summary>
        /// 各UIオブジェクトをまとめてInspector上で指定する
        /// ※要素順は enum UI の順番と一致させること
        /// </summary>
        [NamedSerializeField(
            new string[]
            {
                "EnergyGage",
                "Boot_ON",
                "Boot_OFF",
                "N_Magnet",
                "S_Magnet",
            }
        )]
        [SerializeField]
        private GameObject[] _uiObjects = new GameObject[(int)UI.MAX];

        /// <summary>
        /// 初期化処理
        /// - MagnetManagerオブジェクトを取得
        /// </summary>
        private void Start()
        {
            // シーン内の "MagnetManager" という名前のオブジェクトからコンポーネントを取得
            Magnet = GameObject.Find("MagnetManager").GetComponent<MagnetManager>();
        }
      
        /// <summary>
        /// 【UIボタン用】
        /// 磁力の極タイプ（N/S）を切り替える処理
        /// </summary>
        public void OnButton_ChangeMagnetType()
        {
            // MagnetManager側の関数を呼び出してタイプを変更
            Magnet.ChangeMagnetType();

            // 現在の極タイプをデバッグ出力
            DebugManager.LogMessage(Magnet.CurrentType.ToString(), DebugManager.MessageType.Normal);
        }

        /// <summary>
        /// 【UIボタン用】
        /// 磁力の起動状態（ON/OFF）を切り替える処理
        /// </summary>
        public void OnButton_ChangeMagnetBoot()
        {
            // MagnetManager側の関数を呼び出して起動状態を変更
            Magnet.ChangeMagnetBoot();

            // 現在の起動状態をデバッグ出力
            DebugManager.LogMessage(Magnet.IsMagnetBoot.ToString(), DebugManager.MessageType.Normal);
        }

        /// <summary>
        /// 【デバッグ用】
        /// 任意のボタンが押されたか確認する関数
        /// </summary>
        public void OnButtonClick()
        {
            DebugManager.LogMessage("pushButton!", DebugManager.MessageType.Normal);
        }

        /// <summary>
        /// 毎フレーム更新処理
        /// - 現在の磁力タイプ（N/S）と起動状態（ON/OFF）をUIに反映
        /// </summary>
        public void Update()
        {
            // Rキー入力で磁力タイプ切り替え
            if (Input.GetKeyDown(KeyCode.R))
            {
                OnButton_ChangeMagnetType();
            }

            // ---- 磁力タイプ（N/S）によるUI切り替え ----
            if (Magnet.CurrentType == GameConstants.Layer.N_MAGNET)
            {
                _uiObjects[(int)UI.N_Magnet].SetActive(true);   // N極UIを表示
                _uiObjects[(int)UI.S_Magnet].SetActive(false);  // S極UIを非表示
            }
            else if (Magnet.CurrentType == GameConstants.Layer.S_MAGNET)
            {
                _uiObjects[(int)UI.S_Magnet].SetActive(true);   // S極UIを表示
                _uiObjects[(int)UI.N_Magnet].SetActive(false);  // N極UIを非表示
            }

            // ---- 磁力の起動状態（ON/OFF）によるUI切り替え ----
            if (Magnet.IsMagnetBoot)
            {
                _uiObjects[(int)UI.Boot_ON].SetActive(true);   // ON表示を有効化
                _uiObjects[(int)UI.Boot_OFF].SetActive(false); // OFF表示を無効化
            }
            else
            {
                _uiObjects[(int)UI.Boot_OFF].SetActive(true);  // OFF表示を有効化
                _uiObjects[(int)UI.Boot_ON].SetActive(false);  // ON表示を無効化
            }
        }

        /// <summary>
        /// エネルギーゲージのリセット処理
        /// - ゲージを満タン状態に戻す
        /// </summary>
        public void Reset()
        {
            // EnergyGage の Image コンポーネントを取得
            Image gage = _uiObjects[(int)UI.EnergyGage].GetComponent<Image>();

            // fillAmount（0～1）を最大値に設定
            gage.fillAmount = 1.0f;
        }

        /// <summary>
        /// 現在の磁極タイプ（N/S）を取得する
        /// - 外部スクリプトから呼び出される（弾の極判定用）
        /// </summary>
        public GameConstants.Layer GetCurrentMagnetType()
        {
            if (Magnet == null)
            {
                Debug.LogWarning("MagnetManager が見つかりません。N極を返します。");
                return GameConstants.Layer.N_MAGNET;
            }

            return Magnet.CurrentType;
        }
    }
}
