using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
public class ButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public RectTransform target;
    public float hover = 1.04f;
    public float press = 0.93f;
    public float t = 0.08f;

    Vector3 baseScale;
    Coroutine co;

    void Awake()
    {
        if (!target) target = (RectTransform)transform;
        baseScale = target.localScale;
    }

    public void OnPointerEnter(PointerEventData _) => ScaleTo(hover);
    public void OnPointerExit (PointerEventData _) => ScaleTo(1f);
    public void OnPointerDown  (PointerEventData _) => ScaleTo(press);
    public void OnPointerUp    (PointerEventData _) => ScaleTo(hover);

    public void PulseSuccess()
    {
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(CoPulse());
    }

    void ScaleTo(float s)
    {
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(CoScale(baseScale * s));
    }

    IEnumerator CoScale(Vector3 to)
    {
        Vector3 from = target.localScale;
        float a = 0f;
        while (a < 1f)
        {
            a += Time.unscaledDeltaTime / t;
            target.localScale = Vector3.Lerp(from, to, Mathf.SmoothStep(0,1,a));
            yield return null;
        }
        target.localScale = to;
    }

    IEnumerator CoPulse()
    {
        // quick 1.12 punch then back
        Vector3 up = baseScale * 1.12f;
        yield return CoScale(up);
        yield return CoScale(baseScale);
    }
}
