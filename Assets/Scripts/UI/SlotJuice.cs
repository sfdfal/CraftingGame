using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
public class SlotJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
                          IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("Targets")]
    public RectTransform root;      // usually the Slot RectTransform
    public Image bg;                // background Image (slot frame)
    public Image icon;              // icon image (child)

    [Header("Style")]
    public Color hoverColor = new Color(1f, 1f, 1f, 0.08f);
    public float hoverScale = 1.05f;
    public float pressScale = 0.94f;
    public float animTime = 0.08f;

    Color bgBase;
    Vector3 scaleBase;
    Coroutine colorCo, scaleCo;

    void Awake()
    {
        if (!root) root = (RectTransform)transform;
        if (!bg)   bg = GetComponent<Image>();
        if (!icon) icon = transform.Find("Icon")?.GetComponent<Image>();

        bgBase = bg ? bg.color : Color.white;
        scaleBase = root.localScale;
    }

    public void OnPointerEnter(PointerEventData _)  => TweenTo(hoverScale, hoverColor);
    public void OnPointerExit(PointerEventData _)   => TweenTo(1f,        bgBase);
    public void OnPointerDown(PointerEventData _)   => TweenTo(pressScale, bg ? bg.color : Color.white);
    public void OnPointerUp(PointerEventData _)     => TweenTo(hoverScale, bg ? bg.color : Color.white);

    public void OnBeginDrag(PointerEventData _)     => TweenTo(1.08f, hoverColor);
    public void OnEndDrag(PointerEventData _)       => TweenTo(1f, bgBase);

    public void Shake()
    {
        StopAllCoroutines();
        StartCoroutine(CoShake());
    }

    void TweenTo(float scale, Color c)
    {
        if (scaleCo != null) StopCoroutine(scaleCo);
        if (colorCo != null) StopCoroutine(colorCo);
        scaleCo = StartCoroutine(CoScale(scale));
        if (bg) colorCo = StartCoroutine(CoColor(c));
    }

    IEnumerator CoScale(float target)
    {
        float t = 0f;
        Vector3 from = root.localScale;
        Vector3 to   = scaleBase * target;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / animTime;
            root.localScale = Vector3.Lerp(from, to, Mathf.SmoothStep(0,1,t));
            yield return null;
        }
        root.localScale = to;
    }

    IEnumerator CoColor(Color target)
    {
        float t = 0f;
        Color from = bg.color;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / animTime;
            bg.color = Color.Lerp(from, target, Mathf.SmoothStep(0,1,t));
            yield return null;
        }
        bg.color = target;
    }

    IEnumerator CoShake()
    {
        // subtle “invalid drop” shake
        float dur = 0.18f;
        float mag = 6f;
        float t = 0f;
        Vector2 basePos = root.anchoredPosition;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float f = (1f - t/dur);
            root.anchoredPosition = basePos + new Vector2(Mathf.Sin(t*60f), 0f) * mag * f;
            yield return null;
        }
        root.anchoredPosition = basePos;
        TweenTo(1f, bgBase);
    }
}
