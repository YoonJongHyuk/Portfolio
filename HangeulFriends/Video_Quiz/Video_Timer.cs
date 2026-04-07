using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class Video_Timer : MonoBehaviour
{
    [SerializeField] float time;

    [Header("Play Button Icons")]
    [SerializeField] private List<GameObject> playArrowIcon; // ▶
    [SerializeField] private List<GameObject> pauseIcon;     // ⏸ 
    [SerializeField] private GameObject videoStateIcon; // ⏸ or ▶
    private Coroutine hideCoroutine;
    [SerializeField] private VideoPlayer videoPlayer;

    private void Awake()
    {
        SetTime(5f);
        ApplyButtonIcon(VideoButtonState.Stopped);
    }

    // 버튼 상태 정의
    public enum VideoButtonState
    {
        Stopped,     // 시작 전 or Stop됨
        Preparing,   // Prepare 중
        Playing,     // 재생 중
        Paused       // 일시정지
    }


    private void OnEnable()
    {
        videoPlayer.loopPointReached += OnVideoLoop;
    }

    private void OnDisable()
    {
        videoPlayer.loopPointReached -= OnVideoLoop;
    }

    private void OnVideoLoop(VideoPlayer vp)
    {
        if (vp.isLooping)
        {
            // 🔥 loop로 다시 시작 → 실제로는 재생 중이므로 Pause 아이콘
            ApplyButtonIcon(VideoButtonState.Playing);
        }
        else
        {
            // loop 안 하면 종료 상태
            ApplyButtonIcon(VideoButtonState.Stopped);
        }
    }


    /// <summary>
    /// 버튼 아이콘 + 상태 팝업 아이콘 모두 관리
    /// </summary>
    public void RefreshPlayButtonIcon(VideoPlayer vp, bool isPreparing, bool isPlaying)
    {
        VideoButtonState state;

        if (isPreparing)
            state = VideoButtonState.Preparing;
        else if (isPlaying)
            state = VideoButtonState.Playing;
        else if (vp.isPrepared && vp.time > 0)
            state = VideoButtonState.Paused;
        else
            state = VideoButtonState.Stopped;

        ApplyButtonIcon(state);
    }





    // ============================
    // 버튼 아이콘 관리
    // ============================
    private void ApplyButtonIcon(VideoButtonState state)
    {
        // -------- 버튼 아이콘 --------
        foreach (var icon in playArrowIcon)
        {
            if (icon) icon.SetActive(false);
        }
        foreach (var icon in pauseIcon)
        {
            if (icon) icon.SetActive(false);
        }

        switch (state)
        {
            case VideoButtonState.Playing:
                foreach (var icon in pauseIcon)
                    if (icon) icon.SetActive(true);
                break;

            default:
                foreach (var icon in playArrowIcon)
                    if (icon) icon.SetActive(true);
                break;
        }

        // -------- 중앙 상태 아이콘 --------
        switch (state)
        {
            case VideoButtonState.Playing:
                SetStateIconVisible(true, 1f); // 재생 중: 잠깐 보여주고 숨김
                break;

            case VideoButtonState.Preparing:
                SetStateIconVisible(true);            // 준비 중: 계속 표시
                break;

            case VideoButtonState.Paused:
                SetStateIconVisible(true);            // ✅ 일시정지: 다시 표시(숨김 코루틴 중지 포함)
                break;

            case VideoButtonState.Stopped:
            default:
                //SetStateIconVisible(false);
                break;
        }

    }

    // ============================
    // 중앙 상태 아이콘 관리 (통합)
    // ============================
    private void SetStateIconVisible(bool visible, float? hideAfterSeconds = null)
    {
        if (videoStateIcon == null) return;

        // 기존 숨김 예약이 있으면 정리
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        videoStateIcon.SetActive(visible);

        // visible 이고, hideAfterSeconds 가 있으면 숨김 예약
        if (visible && hideAfterSeconds.HasValue)
        {
            hideCoroutine = StartCoroutine(HideIconAfterDelay(hideAfterSeconds.Value));
        }
    }

    private IEnumerator HideIconAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        if (videoStateIcon != null)
            videoStateIcon.SetActive(false);

        hideCoroutine = null;
    }




    // --------------------- 기존 퀴즈 타이머 로직 ---------------------

    public void QuizStart(GameObject obj, VideoPlayer video)
    {
        if (video.time >= time)
        {
            video.Stop();
            obj.SetActive(true);
        }
    }

    public void QuizEnd(GameObject obj, VideoPlayer video)
    {
        video.Play();
        obj.SetActive(false);
    }

    public void SetTime(float t) => time = t;

    public IEnumerator StartTimer(GameObject obj, VideoPlayer video)
    {
        Debug.Log("Timer started");
        float timer = 0f;
        while (true)
        {
            timer += Time.deltaTime;
            if (timer >= time)
            {
                QuizStart(obj, video);
                yield break;
            }
            yield return null;
        }
    }
}
