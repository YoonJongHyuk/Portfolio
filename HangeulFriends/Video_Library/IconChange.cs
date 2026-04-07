using System.Collections.Generic;
using UnityEngine;

public class IconChange : MonoBehaviour
{
    [SerializeField] private List<GameObject> iconPanels;
    [SerializeField] private bool isIconChanged = false;

    public void ChangeIcon(bool shouldChange)
    {
        isIconChanged = shouldChange;
        iconPanels[0].SetActive(!isIconChanged);
        iconPanels[1].SetActive(isIconChanged);
    }
}
