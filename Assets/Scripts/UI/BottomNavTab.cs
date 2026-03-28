using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BottomNavTab : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform selectionBg;
    [SerializeField] private RectTransform iconTransform;
    [SerializeField] private TextMeshProUGUI label;

    private float bgNormalY;
    private float iconNormalY;
    private float labelNormalY;
    private Coroutine animCoroutine;
    private bool isSelected;

    private void Awake()
    {
        bgNormalY = selectionBg.anchoredPosition.y;
        iconNormalY = iconTransform.anchoredPosition.y;
        if (label != null)
            labelNormalY = label.rectTransform.anchoredPosition.y;
    }

    public void SetSelected(bool selected, float bgSlideUp, float iconSlideUp, float textSlideUp, float duration)
    {
        if (isSelected == selected) return;
        isSelected = selected;

        if (animCoroutine != null)
            StopCoroutine(animCoroutine);

        animCoroutine = StartCoroutine(AnimateTab(selected, bgSlideUp, iconSlideUp, textSlideUp, duration));
    }

    public void SetSelectedImmediate(bool selected, float bgSlideUp, float iconSlideUp, float textSlideUp)
    {
        isSelected = selected;

        SetPositionY(selectionBg, selected ? bgNormalY + bgSlideUp : bgNormalY);
        SetPositionY(iconTransform, selected ? iconNormalY + iconSlideUp : iconNormalY);

        if (label != null)
        {
            SetPositionY(label.rectTransform, selected ? labelNormalY + textSlideUp : labelNormalY);
            label.gameObject.SetActive(selected);
            var c = label.color;
            c.a = selected ? 1f : 0f;
            label.color = c;
        }
    }

    private IEnumerator AnimateTab(bool selected, float bgSlideUp, float iconSlideUp, float textSlideUp, float duration)
    {
        float bgStartY = selectionBg.anchoredPosition.y;
        float bgTargetY = selected ? bgNormalY + bgSlideUp : bgNormalY;

        float iconStartY = iconTransform.anchoredPosition.y;
        float iconTargetY = selected ? iconNormalY + iconSlideUp : iconNormalY;

        float labelStartY = 0f, labelTargetY = 0f;
        if (label != null)
        {
            labelStartY = label.rectTransform.anchoredPosition.y;
            labelTargetY = selected ? labelNormalY + textSlideUp : labelNormalY;
        }

        float startAlpha = label != null ? label.color.a : 0f;
        float targetAlpha = selected ? 1f : 0f;

        if (selected && label != null)
            label.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutBack(Mathf.Clamp01(elapsed / duration));
            float tLinear = Mathf.Clamp01(elapsed / duration);

            SetPositionY(selectionBg, Mathf.LerpUnclamped(bgStartY, bgTargetY, t));
            SetPositionY(iconTransform, Mathf.LerpUnclamped(iconStartY, iconTargetY, t));

            if (label != null)
            {
                SetPositionY(label.rectTransform, Mathf.LerpUnclamped(labelStartY, labelTargetY, t));
                var c = label.color;
                c.a = Mathf.Lerp(startAlpha, targetAlpha, tLinear);
                label.color = c;
            }

            yield return null;
        }

        SetPositionY(selectionBg, bgTargetY);
        SetPositionY(iconTransform, iconTargetY);
        if (label != null)
            SetPositionY(label.rectTransform, labelTargetY);

        if (!selected && label != null)
            label.gameObject.SetActive(false);

        animCoroutine = null;
    }

    private static void SetPositionY(RectTransform rt, float y)
    {
        var pos = rt.anchoredPosition;
        pos.y = y;
        rt.anchoredPosition = pos;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
