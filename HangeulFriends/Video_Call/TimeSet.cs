using TMPro;
using UnityEngine;
using UnityEngine.Video;

public class TimeSet : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private TMP_Text timeText;

    private string FormatTime(float time)
    {
        int m = Mathf.FloorToInt(time / 60f);
        int s = Mathf.FloorToInt(time % 60f);
        return $"{m:00}:{s:00}";
    }

    // Update is called once per frame
    void Update()
    {
        if (!this.gameObject.activeInHierarchy) return;
        float currentTime = (float)videoPlayer.time;
        timeText.text = FormatTime(currentTime);
    }
}
