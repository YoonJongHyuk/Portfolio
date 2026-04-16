using UnityEngine;
using UnityEngine.UI;

public class BrushTriggerEvent : MonoBehaviour
{
    Color brushColor;

    Color currentColor;


    float r;
    float g;
    float b;

    public int cleanCount = 1500;


    RawImage currentBrush;

    private void Start()
    {
        currentBrush = transform.GetComponent<RawImage>();
        brushColor = currentBrush.color;
        currentColor = new Color(brushColor.r, brushColor.g, brushColor.b, brushColor.a);

        r = brushColor.r / cleanCount;
        g = brushColor.g / cleanCount;
        b = brushColor.b / cleanCount;
    }

    public void BrushEvent()
    {
        cleanCount--; // cleanCount를 감소시킴

        // 색을 점점 어둡게 변경
        currentColor = new Color(currentColor.r - r, currentColor.g - g, currentColor.b - b);
        currentBrush.color = currentColor; // 브러시 색 업데이트
    }

}
