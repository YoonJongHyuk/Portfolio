using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerNotebook : MonoBehaviour
{
    [HorizontalGroup("Lists")]
    public List<Button> tabList;

    [HorizontalGroup("Lists")]
    public List<GameObject> pageList;

    public List<GameObject> buildList;

    private void Start()
    {
        TabButtonClick(0);
    }

    [Button]
    public void TabButtonClick(int index)
    {
        // 버튼 활성화
        for (int i = 0; i < tabList.Count; i++)
        {
            tabList[i].interactable = true;
        }

        // 누른 버튼 비활성화
        tabList[index].interactable = false;

        // 모든 페이지 비 활성화
        for (int i = 0; i < pageList.Count; i++)
        {
            pageList[i].SetActive(false);
        }

        // 누른 페이지 활성화
        pageList[index].SetActive(true);
    }

    public void build(int index)
    {
        GameObject go = Instantiate(buildList[index], Camera.main.transform.forward * 0.3f, Quaternion.identity);
    }

}
