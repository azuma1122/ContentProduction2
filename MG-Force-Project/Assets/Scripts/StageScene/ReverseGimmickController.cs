using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.StageScene;

public class ReverseGimmickController : MonoBehaviour
{
    // 対象となるボタン
    private ButtonController _button;

    // 表示／非表示を切り替えるブロック
    [SerializeField] private GameObject _fixedBox;

    private void Start()
    {
        // このスクリプトがどのオブジェクトに付いているか確認用
        Debug.Log("このスクリプトがアタッチされているオブジェクト: " + gameObject.name);

        // ボタンを探す
        TryFindButton();

        // 初期状態ではブロックを非表示にする
        if (_fixedBox != null)
        {
            _fixedBox.SetActive(false);
            Debug.Log("ReverseGimmickController: 初期状態でブロックを非表示にしました");
        }
    }

    private void Update()
    {
        // ブロックが未設定 or 破壊されている場合は処理しない
        if (_fixedBox == null)
        {
            Debug.LogWarning("_fixedBox が破壊されているか設定されていません");
            return;
        }

        // ボタンが見つからない or 破壊されている場合は再取得を試みる
        if (_button == null)
        {
            TryFindButton();
            return;
        }

        // 逆ギミックの本処理
        // ボタンが押されている（下がっている）時だけブロックを表示する
        _fixedBox.SetActive(!_button.GetIsUpButton());
    }

    /// <summary>
    /// シーン内から Button(Clone) を探して ButtonController を取得する
    /// </summary>
    private void TryFindButton()
    {
        // 名前でボタンオブジェクトを検索
        GameObject obj = GameObject.Find("Button(Clone)");

        if (obj != null)
        {
            // ButtonController を取得
            _button = obj.GetComponent<ButtonController>();

            if (_button != null)
            {
                Debug.Log("Button を取得しました: " + obj.name);
            }
            else
            {
                Debug.LogWarning("ButtonController コンポーネントが見つかりません");
            }
        }
        else
        {
            // ボタンが存在しない（破壊された）場合
            _button = null;
        }
    }
}
