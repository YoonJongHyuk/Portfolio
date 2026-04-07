using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;

public class QuizeSliderSetting : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoData videoData;

    [Tooltip("퀴즈 패널들(0번=1번째 퀴즈, 1번=2번째...)")]
    [SerializeField] private List<GameObject> quizPanels;

    [Header("Option")]
    [Tooltip("도달 판정 허용 오차(초). 0.1~0.25 추천")]
    [SerializeField] private float hitEpsilon = 0.15f;

    [Header("Quiz Option")]
    [SerializeField] private bool enableQuiz = true;

    private bool quizOpen = false;
    private int currentQuizIndex = -1;

    private readonly HashSet<int> fired = new HashSet<int>();
    private int nextIndex = 0;

    private void Awake()
    {
        HideAllPanels();
    }

    public void SetQuizEnabled(bool enabled)
    {
        enableQuiz = enabled;

        if (!enableQuiz)
        {
            // 퀴즈 기능 끄면 열려있는 패널은 닫기
            CloseQuizAndResume();
            return;
        }

        // ✅ 켤 때: 현재 시간 기준으로 nextIndex를 재정렬해서
        // 이미 지난 퀴즈가 즉시 뜨는 현상 방지
        SyncNextIndexToCurrentTime();
    }

    private void SyncNextIndexToCurrentTime()
    {
        if (videoPlayer == null || !videoPlayer.isPrepared) return;
        if (videoData == null || videoData.quizTiming == null || videoData.quizTiming.quizTime == null) return;

        var times = videoData.quizTiming.quizTime;
        if (times.Count == 0) return;

        float now = (float)videoPlayer.time;

        // fired는 그대로 유지(이미 푼 퀴즈는 다시 안 뜨게)
        // nextIndex는 "지금 이후에 나올 첫 퀴즈"로 이동
        int i = 0;
        while (i < times.Count)
        {
            if (!fired.Contains(i) && times[i] >= now - hitEpsilon)
                break;
            i++;
        }

        nextIndex = i;
    }



    public void SetenableQuiz(bool enable)
    {
        enableQuiz = enable;
    }

    public void Init(VideoPlayer vp, VideoData vd)
    {
        videoPlayer = vp;
        videoData = vd;

        // ✅ 데이터가 세팅되면 패널 개수 검증
        ValidatePanelCount();
    }

    private void Update()
    {
        if (!enableQuiz) return;
        if (quizOpen) return;
        if (videoPlayer == null || videoData == null) return;
        if (videoData.quizTiming == null || videoData.quizTiming.quizTime == null) return;

        if (!videoPlayer.isPrepared) return;
        if (!videoPlayer.isPlaying) return;

        var times = videoData.quizTiming.quizTime;
        if (times.Count == 0) return;



        double t = videoPlayer.time;

        while (nextIndex < times.Count && fired.Contains(nextIndex))
            nextIndex++;

        if (nextIndex >= times.Count) return;

        float target = times[nextIndex];

        if (Mathf.Abs((float)t - target) <= hitEpsilon)
        {
            TriggerQuiz(nextIndex, (float)t, reason: "PlaybackReached");
        }

    }

    /// <summary>
    /// 슬라이더 Seek 이후 호출 (외부에서 seekTime 전달)
    /// </summary>
    public void OnSeekToTime(float seekTime)
    {
        if (!enableQuiz) return;
        if (quizOpen) return;
        if (videoData == null || videoData.quizTiming == null || videoData.quizTiming.quizTime == null) return;

        var times = videoData.quizTiming.quizTime;
        if (times.Count == 0) return;

        if (quizPanels == null || quizPanels.Count < times.Count)
        {
            Debug.LogWarning($"[QuizTrigger] quizPanels 개수({quizPanels?.Count ?? 0})가 quizTime 개수({times.Count})보다 적습니다.");
            return;
        }

        for (int i = 0; i < times.Count; i++)
        {
            if (fired.Contains(i)) continue;

            float qt = times[i];

            if (Mathf.Abs(seekTime - qt) <= hitEpsilon)

            {
                TriggerQuiz(i, seekTime, reason: "Seek");
                nextIndex = Mathf.Max(nextIndex, i + 1);
                break;
            }
        }
    }

    private void TriggerQuiz(int index, float currentTime, string reason)
    {
        if (!enableQuiz) return;
        if (quizOpen) return;
        if (fired.Contains(index)) return;

        fired.Add(index);
        quizOpen = true;
        currentQuizIndex = index;

        if (videoPlayer != null)
            videoPlayer.Pause();

        ShowOnlyPanel(index);
        QuizManager.instance.SetQuiz(index);
        Debug.Log($"[QuizTrigger] Quiz #{index} OPEN at {currentTime:0.00}s (reason={reason})");
    }

    /// <summary>
    /// 퀴즈 정답 처리 후: 현재 패널 닫고 영상 재개
    /// </summary>
    public void CloseQuizAndResume()
    {
        HideAllPanels();

        quizOpen = false;
        currentQuizIndex = -1;

        if (videoPlayer != null && videoPlayer.isPrepared)
            videoPlayer.Play();
    }

    public void ResetQuizState()
    {
        fired.Clear();
        nextIndex = 0;
        quizOpen = false;
        currentQuizIndex = -1;

        HideAllPanels();
    }

    private void ShowOnlyPanel(int index)
    {
        if (quizPanels == null) return;

        if(quizPanels.Count == 1)
        {
            quizPanels[0].SetActive(true);
            return;
        }
        for (int i = 0; i < quizPanels.Count; i++)
        {
            if (quizPanels[i] == null) continue;
            quizPanels[i].SetActive(i == index);
        }
    }

    private void HideAllPanels()
    {
        if (quizPanels == null) return;

        if(quizPanels.Count == 1)
        {
            quizPanels[0].SetActive(false);
            return;
        }

        for (int i = 0; i < quizPanels.Count; i++)
        {
            if (quizPanels[i] != null)
                quizPanels[i].SetActive(false);
        }
    }

    private void ValidatePanelCount()
    {
        if (videoData == null || videoData.quizTiming == null || videoData.quizTiming.quizTime == null) return;

        int quizCount = videoData.quizTiming.quizTime.Count;
        if (quizCount <= 0) return;

        if (quizPanels == null || quizPanels.Count == 0)
        {
            Debug.LogWarning($"[QuizTrigger] quizPanels가 비어있습니다. (quizCount={quizCount})");
            return;
        }

        //if (quizPanels.Count != quizCount)
        //{
        //    Debug.LogWarning($"[QuizTrigger] quizPanels 개수({quizPanels.Count})와 quizTime 개수({quizCount})가 다릅니다. " +
        //                     $"0번 패널=1번째 퀴즈 규칙으로 맞춰주세요.");
        //}
    }
}
