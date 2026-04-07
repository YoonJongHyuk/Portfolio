using System.Collections.Generic;
using UnityEngine;

public class VideoPassEvent : MonoBehaviour
{
    [SerializeField] private List<GameObject> windowObjects;
    [SerializeField] private List<GameObject> fullScreenObjects;

    public void SetPassed(bool passed)
    {
        windowObjects[0].SetActive(!passed);
        windowObjects[1].SetActive(passed);
        fullScreenObjects[0].SetActive(!passed);
        fullScreenObjects[1].SetActive(passed);
    }
}
