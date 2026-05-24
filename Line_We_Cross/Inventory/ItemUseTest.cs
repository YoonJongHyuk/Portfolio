using UnityEngine;

/// <summary>
/// 아이템 사용 테스트용 함수
/// </summary>
public class ItemUseTest : MonoBehaviour
{
    public Item current_item;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            current_item.Use(); // 플레이어가 자기 자신 대상으로 사용
        }
    }

}
