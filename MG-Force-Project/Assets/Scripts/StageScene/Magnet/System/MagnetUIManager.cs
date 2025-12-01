using UnityEngine;
using UnityEngine.UI;

namespace Game.StageScene.Magnet
{
    /// <summary>
    /// マグネット関連のUIを管理するクラス
    /// - 磁力のON/OFFやN/S極の切り替えをUIに反映
    /// </summary>
    public class MagnetUIManager : MonoBehaviour
    {
        // MagnetManager の参照
        private MagnetManager Magnet;

        // UI 要素のインデックス管理
        private enum UI
        {
            EnergyGage,
            Boot_ON,
            Boot_OFF,
            N_Magnet,
            S_Magnet,
            MAX,
        }

        // UI 要素を Inspector でまとめて設定する配列
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

        private void Start()
        {
            // MagnetManager をシーンから検索して取得
            Magnet = GameObject.Find("MagnetManager")
                .GetComponent<MagnetManager>();

            if (Magnet == null)
            {
                Debug.LogError("MagnetManager が見つかりません!");
            }

            // 初期状態の設定（S極を表示、N極を非表示）
            UpdateMagnetTypeUI();
        }

        private void Update()
        {
            if (Magnet == null) return;

            // ------------------------------------------------
            // 極（N / S）UI の更新
            // ------------------------------------------------
            UpdateMagnetTypeUI();

            // ------------------------------------------------
            // BOOT ON/OFF UI 更新
            // ------------------------------------------------
            _uiObjects[(int)UI.Boot_ON].SetActive(Magnet.IsMagnetBoot);
            _uiObjects[(int)UI.Boot_OFF].SetActive(!Magnet.IsMagnetBoot);
        }

        /// <summary>
        /// 磁石の極UIを更新（N極とS極を切り替え）
        /// </summary>
        private void UpdateMagnetTypeUI()
        {
            if (Magnet.CurrentType == GameConstants.Layer.N_MAGNET)
            {
                // N極を表示、S極を非表示
                _uiObjects[(int)UI.N_Magnet].SetActive(true);
                _uiObjects[(int)UI.S_Magnet].SetActive(false);
            }
            else // S_MAGNET
            {
                // S極を表示、N極を非表示
                _uiObjects[(int)UI.S_Magnet].SetActive(true);
                _uiObjects[(int)UI.N_Magnet].SetActive(false);
            }
        }

        /// <summary>
        /// エネルギーゲージを満タンに戻したいときに使用
        /// </summary>
        public void Reset()
        {
            Image gage = _uiObjects[(int)UI.EnergyGage].GetComponent<Image>();
            gage.fillAmount = 1.0f;
        }

        /// <summary>
        /// 現在の磁石の極（N または S）を返す
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