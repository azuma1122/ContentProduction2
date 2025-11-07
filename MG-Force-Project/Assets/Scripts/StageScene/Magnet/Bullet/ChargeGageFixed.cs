using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChargeGageFixed : MonoBehaviour
{
    void LateUpdate()
    {
        // スケール反転（x=-1）されても常に正方向に保つ
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x);
        transform.localScale = scale;
    }
}
