using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.StageScene
{
    /// <summary>
    /// ボタン（踏みスイッチ）の ON / OFF 状態を管理するクラス。
    /// 
    /// ● 主な機能
    /// - プレイヤー or Moving オブジェクトが踏むとボタンが「DOWN」になる
    /// - 離れると「UP」に戻る（現在はコメントアウトで OFF）
    /// - ボタンの見た目（上/下）の切り替え
    /// - 初回生成時に位置のオフセット調整が可能
    /// </summary>
    public class ButtonController : MonoBehaviour
    {
        [SerializeField] private GameObject _buttonUp;   // ボタンが上がっている状態のモデル
        [SerializeField] private GameObject _buttonDown; // ボタンが下がっている状態のモデル

        [Header("位置調整")]
        [Tooltip("生成時にY座標を調整する量（負の値で下がる）")]
        [SerializeField] private float _initialYOffset = -0.5f;

        [Tooltip("初回起動時のみ位置調整を行う")]
        [SerializeField] private bool _adjustPositionOnStart = true;

        // ボタンの現在の状態（true = 上がっている / false = 下がっている）
        private bool isUpButton = true;

        // 位置調整を一度だけ行うためのフラグ
        private bool _hasAdjustedPosition = false;


        // ---------------------------------------------------------
        // 初期化処理：必要ならボタン位置を初期オフセットで調整
        // ---------------------------------------------------------
        private void Start()
        {
            // 初回だけ位置調整を行う
            if (_adjustPositionOnStart && !_hasAdjustedPosition)
            {
                AdjustInitialPosition();
                _hasAdjustedPosition = true;
            }
        }


        // ---------------------------------------------------------
        // 毎フレーム：ボタンの見た目を状態によって切り替える
        // ---------------------------------------------------------
        private void Update()
        {
            if (isUpButton)
            {
                // ボタンが上がっているとき
                _buttonDown.SetActive(false);
                _buttonUp.SetActive(true);
            }
            else
            {
                // ボタンが下がっているとき
                _buttonUp.SetActive(false);
                _buttonDown.SetActive(true);
            }
        }


        // ---------------------------------------------------------
        // 何かがボタンに乗っている間、ボタンを押し下げる状態にする
        // ---------------------------------------------------------
        private void OnTriggerStay(Collider collider)
        {
            // UNTAGGED は無視
            if (collider.CompareTag(GameConstants.Tag.UNTAGGED)) return;

            // デバッグで何が乗っているか確認
            // Debug.LogWarning(collider.gameObject.tag);

            // プレイヤー と Moving ブロックのときボタンを押す
            if (collider.gameObject.CompareTag(GameConstants.Tag.MOVING) ||
                collider.gameObject.CompareTag(GameConstants.Tag.PLAYER))
            {

                //ボタンが押し上がっているので
                if (isUpButton)
                {
                    //SE障害物のボタンを押した時はこの一行（必要時にコメントアウト

                    SEManager.instance.PlaySE(SEManager.Obstacle.ButtonPress);

                    //ここまで

                    //ボタンを押し下げる
                    isUpButton = false;

                }
                
            }
        }


        // ---------------------------------------------------------
        // ボタンから離れたときに呼ばれる
        // ※ 現状では戻らない仕様なのでコメントアウト
        // ---------------------------------------------------------
        private void OnTriggerExit(Collider collider)
        {
            if (collider.CompareTag(GameConstants.Tag.UNTAGGED)) return;

            if (collider.gameObject.CompareTag(GameConstants.Tag.MOVING) ||
                collider.gameObject.CompareTag(GameConstants.Tag.PLAYER))
            {
                // isUpButton = true; 
                
            }
        }


        // ---------------------------------------------------------
        // 外部から現在の状態を取得するための関数
        // ---------------------------------------------------------
        public bool GetIsUpButton()
        {
            return isUpButton;
        }


        // ---------------------------------------------------------
        // 初期位置の調整。生成直後に Y 軸方向へ移動する
        // ---------------------------------------------------------
        private void AdjustInitialPosition()
        {
            Vector3 currentPos = transform.position;

            // 指定したオフセットを適用
            currentPos.y += _initialYOffset;
            transform.position = currentPos;

            // デバッグログ
            Debug.Log(
                $"ButtonController: 位置調整完了 最終Position={transform.position}, オフセット={_initialYOffset}"
            );
        }
    }
}
