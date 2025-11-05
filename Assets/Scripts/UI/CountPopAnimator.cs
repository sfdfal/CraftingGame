using UnityEngine;
using TMPro;
using System.Collections;

public class CountPopAnimator : MonoBehaviour
{
    public TextMeshProUGUI tmp;
    public float popScale = 1.18f;
    public float popTime  = 0.08f;

    int lastShown = -999;
    Vector3 baseScale;

    void Awake()
    {
        if (!tmp) tmp = GetComponent<TextMeshProUGUI>();
        baseScale = transform.localScale;
    }

    public void SetCount(int amount, bool showWhenOne = false)
    {
        string s = (amount > 1 || showWhenOne) ? amount.ToString() : "";
        if (tmp) tmp.text = s;

        if (amount != lastShown)
        {
            lastShown = amount;
            StopAllCoroutines();
            StartCoroutine(CoPop());
        }
    }

    IEnumerator CoPop()
    {
        // scale up fast, then ease back
        float up = popTime, down = popTime * 1.4f;
        transform.localScale = baseScale * popScale;
        float t = 0f;
        while (t < down)
        {
            t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(baseScale * popScale, baseScale, t/down);
            yield return null;
        }
        transform.localScale = baseScale;
    }
}
