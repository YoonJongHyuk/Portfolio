using UnityEngine;
using UnityEngine.UI;

public class SliderSafeFill : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider slider;
    [SerializeField] private RectTransform timeBarRect;   // 전체 바 (width 기준)
    [SerializeField] private Image fillImage;             // Fill Image (Sliced 권장)
    [SerializeField] private CanvasGroup fillCanvasGroup; // Fill 또는 FillArea에 부착

    [Header("Option")]
    [SerializeField] private bool hideWhenZero = true;

    private float safeValue;   // border 때문에 fill이 깨지지 않는 최소값
    private float lastWidth = -1f;
    private float lastValue = -1f;
    private bool lastShown = true;

    // =========================================================
    // 초기화
    // =========================================================
    private void Awake()
    {
        if (slider == null || timeBarRect == null || fillCanvasGroup == null)
        {
            Debug.LogError("[SliderSafeFill] Reference missing.");
            enabled = false;
            return;
        }

        RecalculateSafeValue(true);

        // 값 변경 이벤트
        slider.onValueChanged.AddListener(_ => RefreshIfNeeded());

        lastShown = fillCanvasGroup.alpha > 0.5f;

        // 초기 상태 강제 적용
        RefreshIfNeeded(force: true);
    }

    // =========================================================
    // 매 프레임 체크 (레이아웃 변경 & Notify 없는 값 변경 대응)
    // =========================================================
    private void Update()
    {
        if (!isActiveAndEnabled) return;

        // width 변경 감지 (해상도 / orientation / layout rebuild)
        float width = timeBarRect.rect.width;
        if (!Mathf.Approximately(width, lastWidth))
        {
            RecalculateSafeValue();
            RefreshIfNeeded(force: true);
        }

        // 외부 코드에서 slider.value 변경 대응
        if (!Mathf.Approximately(slider.value, lastValue))
        {
            RefreshIfNeeded();
        }
    }

    // =========================================================
    // safeValue 계산 (9-slice border 대응)
    // =========================================================
    private void RecalculateSafeValue(bool log = false)
    {
        float width = timeBarRect.rect.width;
        lastWidth = width;

        if (width <= 0f)
        {
            safeValue = 0f;
            return;
        }

        float borderL = 0f;
        float borderR = 0f;

        if (fillImage != null && fillImage.sprite != null)
        {
            Vector4 b = fillImage.sprite.border; // px
            borderL = b.x;
            borderR = b.z;
        }

        safeValue = (borderL + borderR) / width;

        if (log)
        {
            Debug.Log($"[SliderSafeFill] width:{width}  borderL:{borderL} borderR:{borderR} safeValue:{safeValue}");
        }
    }

    // =========================================================
    // 표시/숨김 처리 (NaN 완벽 방어)
    // =========================================================
    private void RefreshIfNeeded(bool force = false)
    {
        float value = slider.value;

        // 🔥 NaN / Infinity 방어 (핵심)
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            Debug.LogWarning("[SliderSafeFill] Slider value is NaN/Infinity → reset to 0");
            value = 0f;
            slider.SetValueWithoutNotify(0f);
        }

        lastValue = value;

        bool shouldShow = value >= safeValue;

        if (hideWhenZero && value <= 0f)
            shouldShow = false;

        if (force || shouldShow != lastShown)
        {
            lastShown = shouldShow;

            fillCanvasGroup.alpha = shouldShow ? 1f : 0f;
            fillCanvasGroup.blocksRaycasts = shouldShow;
            fillCanvasGroup.interactable = shouldShow;
        }
    }
}
