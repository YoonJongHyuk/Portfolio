using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class VideoManager : MonoBehaviour
{
    public VideoData videoData;
    [SerializeField] private VideoPlayer videoPlayer;

    public GameObject videoPanel;
    [SerializeField] private GameObject quizPanel;

    [SerializeField] private Video_Timer video_Timer;
    [SerializeField] private VideoPlay videoPlay;
    [SerializeField] private List<Quiz_UI_Setting> quiz_UI_Settings;
    [SerializeField] private QuizeSliderSetting quizeSliderSetting;
    public static VideoManager Instance { get; private set; }


    void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
        }
        // ✅ 여기서만 세팅 (중복 제거)
        videoPlay.Init(videoPlayer, videoData);
        quizeSliderSetting.Init(videoPlayer, videoData);
        videoPlay.OnStateChanged += RefreshIcon;
    }

    void Start()
    {
        foreach (Quiz_UI_Setting setting in quiz_UI_Settings)
        {
            setting.videoData = videoData; // ✅ VideoData 직접 참조
        }
    }

    private void RefreshIcon()
    {
        video_Timer.RefreshPlayButtonIcon(
            videoPlayer,
            videoPlay.IsPreparing,
            videoPlay.IsPlaying
        );
    }

    public void VideoOnOff()
    {
        // ✅ 인자 없이 토글
        videoPlay.TogglePlayPause();
    }

    public void LoopVideo()
    {
        if (videoPlayer != null)
            videoPlayer.isLooping = !videoPlayer.isLooping;
    }

    public void Click_OnOffPanel(GameObject obj)
    {
        obj.SetActive(!obj.activeSelf);
    }
}
