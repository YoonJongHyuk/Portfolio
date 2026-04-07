using System.Collections.Generic;
using UnityEngine;

public class ShuffleButton : MonoBehaviour
{
    [SerializeField] private List<GameObject> shufflePanels;
    [SerializeField] private bool isShuffled = false;

    public void OnShuffleButtonClicked()
    {
        isShuffled = !isShuffled;
        shufflePanels[0].SetActive(!isShuffled);
        shufflePanels[1].SetActive(isShuffled);
    }
}
