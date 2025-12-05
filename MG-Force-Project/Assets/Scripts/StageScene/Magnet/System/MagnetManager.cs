using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.StageScene.Magnet
{
    /// <summary>
    /// プレイヤーの磁力操作を一括管理するクラス
    /// - 磁力のON/OFF(BOOT)
    /// - 極の切り替え(N / S)
    /// - 現在の磁力強度
    /// 
    /// Rキー(極切り替え)とBキー(磁力起動・停止)を処理
    /// </summary>
    public class MagnetManager : MonoBehaviour
    {
        private GameSystem.InputHandler _input;

        /// <summary>
        /// BOOT(磁力起動)状態  
        /// true = 磁力 ON(吸着・反発が働く)  
        /// false = OFF
        /// </summary>
        public bool IsMagnetBoot { get; private set; }

        /// <summary>
        /// 現在選択されている磁石の極(N または S)
        /// </summary>
        public GameConstants.Layer CurrentType { get; private set; }

        /// <summary>
        /// 現在の磁力強度(必要に応じて使用)
        /// </summary>
        public int CurrentPower { get; private set; }

        private void Start()
        {
            // InputHandler をシングルトンから取得
            _input = GameSystem.InputHandler.Instance;

            if (_input == null)
            {
                Debug.LogError("InputHandler が見つかりません！");
            }

            // 初期化
            IsMagnetBoot = false;                        // 磁力は最初OFF
            CurrentType = GameConstants.Layer.S_MAGNET;  // 初期はS極
            CurrentPower = 1;                            // 基本強度

            Debug.Log($"MagnetManager 初期化完了 - 初期極: {CurrentType}");
        }

        private void Update()
        {
            if (_input == null) return;

            // ------------------------------------------------
            // 極切り替え(Rキー)
            // ※BOOT中は切り替え不可
            // ------------------------------------------------
            // 直接Rキーで切り替え
            if (Input.GetKeyDown(KeyCode.R) && !IsMagnetBoot)
            {
                ChangeMagnetType();


            }

            // ------------------------------------------------
            // BOOT起動(MAGNET_BOOT = Bキー)
            // ON↔OFF切り替え
            // ------------------------------------------------
            if (_input.IsActionPressed(InputConstants.Action.MAGNET_BOOT))
            {
                ChangeMagnetBoot();


                //磁力起動しているかどうかでSEの音源を切り替えて鳴らす
                if (IsMagnetBoot)
                {
                    //SE磁力起動時はこの一行（必要時にコメントアウト

                    SEManager.instance.PlaySE(SEManager.Magnet.MAGNET_ACTIVATE);

                    //ここまで
                }
                else
                {
                    //SE磁力リセットはこの一行（必要時にコメントアウト

                    SEManager.instance.PlaySE(SEManager.Magnet.MAGNET_RESET);
                    //ここまで

                }

            }
        }

        /// <summary>
        /// 極性(N / S)を切り替える
        /// ※BOOT中は切り替えできない仕様
        /// </summary>
        public void ChangeMagnetType()
        {
            // BOOT中は切り替えを許可しない
            if (IsMagnetBoot)
            {
                Debug.LogWarning("BOOT起動中は極を切り替えられません");
                return;
            }

            GameConstants.Layer previousType = CurrentType;

            CurrentType = (CurrentType == GameConstants.Layer.N_MAGNET)
                ? GameConstants.Layer.S_MAGNET
                : GameConstants.Layer.N_MAGNET;

            Debug.Log($"磁石の極を切り替え: {previousType} → {CurrentType}");
        }

        /// <summary>
        /// BOOT(磁力ON/OFF)を切り替える
        /// </summary>
        public void ChangeMagnetBoot()
        {
            IsMagnetBoot = !IsMagnetBoot;
            Debug.Log($"磁力BOOT: {(IsMagnetBoot ? "ON" : "OFF")}");
        }

        /// <summary>
        /// 外部から BOOT 状態を直接セットしたい時に使用
        /// </summary>
        public void SetMagnetBoot(bool value)
        {
            IsMagnetBoot = value;
        }
    }
}