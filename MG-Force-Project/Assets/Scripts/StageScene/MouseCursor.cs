using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// マウスカーソルの表示
/// </summary>
public class MouseCursor : MonoBehaviour
{
    [SerializeField] private Texture2D cursorTexture; // カーソル画像

    // Start is called before the first frame update
    void Start()
    {
        // カーソルを常に表示
        Cursor.visible = true;

        // ロック解除（自由に動かせる）
        Cursor.lockState = CursorLockMode.None;
    }
}
