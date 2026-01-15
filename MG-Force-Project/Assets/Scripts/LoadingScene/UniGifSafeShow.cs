
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UniGifSafeShow : MonoBehaviour
{
    public RawImage rawImage;
    [SerializeField]
    private float showDelay = 0.1f; // ← インスペクターで調整

    IEnumerator Start()
    {
        rawImage.enabled = false;
        yield return null; // 1フレーム待つ
        yield return new WaitForSeconds(showDelay);
        rawImage.enabled = true;
    }
}

