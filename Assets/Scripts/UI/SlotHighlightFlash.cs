using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Image))]
public class SlotHighlightFlash : MonoBehaviour
{
    public Color flash = new Color(1f, 1f, 0.4f, 0.65f);
    public float time = 0.25f;

    Image img;
    Color baseC;

    void Awake() { img = GetComponent<Image>(); baseC = img.color; }

    public void Flash()
    {
        StopAllCoroutines();
        StartCoroutine(CoFlash());
    }

    IEnumerator CoFlash()
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float u = 1f - (t / time);
            img.color = Color.Lerp(baseC, flash, u);
            yield return null;
        }
        img.color = baseC;
    }
}
