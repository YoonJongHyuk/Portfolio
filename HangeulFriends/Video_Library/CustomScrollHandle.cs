using UnityEngine;
using UnityEngine.UI;

public class CustomScrollHandle : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform handle;      // 핸들 이미지
    [SerializeField] private RectTransform track;       // 핸들이 움직일 트랙 영역

    private float trackHeight;
    private float handleHeight;

    void Start()
    {
        Canvas.ForceUpdateCanvases();
        scrollRect.onValueChanged.AddListener(OnScroll);
        OnScroll(scrollRect.normalizedPosition);
    }

    void OnScroll(Vector2 normalizedPos)
    {
        float trackHeight = track.rect.height;
        float handleHeight = handle.rect.height;
        float movableRange = trackHeight - handleHeight;

        // 앵커가 상단(Y:1)이므로 0 ~ -movableRange 범위로 클램프
        float posY = (normalizedPos.y - 1f) * movableRange;
        posY = Mathf.Clamp(posY, -movableRange, 0f);

        handle.anchoredPosition = new Vector2(handle.anchoredPosition.x, posY);
    }

    void OnDestroy()
    {
        scrollRect.onValueChanged.RemoveListener(OnScroll);
    }

}
