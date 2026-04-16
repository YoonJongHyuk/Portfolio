using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class BrushManager : MonoBehaviour
{
    Color brushColor;

    Color currentColor;


    float r;
    float g;
    float b;

    int cleanCount = 25;


    Image currentBrush;

    private void Start()
    {
        currentBrush = transform.GetComponent<Image>();
        brushColor = currentBrush.color;
        currentColor = new Color(brushColor.r, brushColor.g, brushColor.b, brushColor.a);

        r = brushColor.r / cleanCount;
        g = brushColor.g / cleanCount;
        b = brushColor.b / cleanCount;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Trash") && cleanCount > 0)
        {
            if (cleanCount == 0)
            {
                print("브러시 다 씀!");
                return;
            }

            cleanCount--; // cleanCount를 감소시킴

            // 색을 점점 어둡게 변경
            currentColor = new Color(currentColor.r - r, currentColor.g - g, currentColor.b - b);
            currentBrush.color = currentColor; // 브러시 색 업데이트

            // MediapipeTrash 컴포넌트가 null이 아닌지 확인
            MediapipeTrash trash = collision.gameObject.GetComponent<MediapipeTrash>();
            if (trash == null)
            {
                print("MediapipeTrash 컴포넌트를 찾지 못했습니다.");
                return;
            }

            trash.CleanTrashEvent();
        }
    }
}
