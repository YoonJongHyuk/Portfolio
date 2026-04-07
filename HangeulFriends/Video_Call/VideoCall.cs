using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class VideoCall : MonoBehaviour
{
    [SerializeField] private VideoPlay videoPlay;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] VideoData videoData;
    [SerializeField] private List<GameObject> videoPanels;
    private Coroutine callRoutine;

    void Awake()
    {
        videoPlay.Init(videoPlayer, videoData);
        CallVideo();
    }

    public void VideoOnOff()
    {
        // ✅ 인자 없이 토글
        videoPlay.TogglePlayPause();
    }

    public void StopButton()
    {
        videoPlayer.Stop();
    }

    public void CallVideo()
    {
        if(callRoutine != null)
        {
            StopCoroutine(callRoutine);
        }

        callRoutine = StartCoroutine(ICallVideo());
    }

    IEnumerator ICallVideo()
    {
        yield return new WaitForSeconds(3f);
        videoPanels[0].SetActive(false);
        videoPanels[1].SetActive(true);
        videoPlay.TogglePlayPause();
    }
}
