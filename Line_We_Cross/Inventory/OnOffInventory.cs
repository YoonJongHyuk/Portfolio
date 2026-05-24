using UnityEngine;

public class OnOffInventory : MonoBehaviour
{
    private bool isOpen;
    public GameObject in_inventory;

    private void Update()
    {
        // 현재 상태를 확인
        bool currentlyOpen = transform.localEulerAngles.x > 20f;

        // 상태가 바뀌었을 때만 처리
        if (currentlyOpen != isOpen)
        {
            isOpen = currentlyOpen;

            if (!isOpen)
            {
                // 닫힘
                in_inventory.SetActive(false);
            }
            else
            {
                // 열림
                in_inventory.SetActive(true);
            }
        }
    }

}
