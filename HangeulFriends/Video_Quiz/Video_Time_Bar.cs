using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections.Generic;

public class Video_Time_Bar : MonoBehaviour
{
    [Header("Refs")]
    public VideoPlayer videoPlayer;
    public List<Slider> timeSliders;
    public List<TMP_Text> totalTimeTexts;
    public TMP_Text currentTimeText;

    [Header("Auto Hide Block")]
    [SerializeField] private Video_Screen_Manager screenManager;

    // 상태 캐시
    private bool _isDragging = false;
    private bool _isPrepared = false;
    private int  _lastTimeSec      = -1;
    private float _lastNormalized  = -1f;

    // GC-Free 문자열 빌더
    private readonly StringBuilder _sb = new(8);

    // -------------------------------------------------------
    void Awake()
    {
        foreach (var slider in timeSliders)
        {
            slider.onValueChanged.AddListener(OnSliderChanged);

            var trigger = slider.gameObject.AddComponent<EventTrigger>();
            AddEvent(trigger, EventTriggerType.PointerDown, OnPointerDown);
            AddEvent(trigger, EventTriggerType.PointerUp,   OnPointerUp);
        }

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted += OnPrepareCompleted;
        }
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.prepareCompleted -= OnPrepareCompleted;
    }

    // -------------------------------------------------------
    // 영상 준비 완료 → 총 길이는 여기서만 갱신
    // -------------------------------------------------------
    private void OnPrepareCompleted(VideoPlayer vp)
    {
        _isPrepared    = true;
        _lastTimeSec   = -1;
        _lastNormalized = -1f;

        string lengthStr = FormatTime((float)vp.length);
        foreach (var t in totalTimeTexts)
            t.SetText(lengthStr);

        // 슬라이더·현재시간 초기화
        if (currentTimeText != null)
            currentTimeText.SetText("00:00");

        foreach (var slider in timeSliders)
            if (slider != null && slider.gameObject.activeInHierarchy)
                slider.SetValueWithoutNotify(0f);
    }

    // -------------------------------------------------------
    // 외부에서 클립 교체 시 호출 (선택)
    // -------------------------------------------------------
    public void ResetPlayer()
    {
        _isPrepared     = false;
        _lastTimeSec    = -1;
        _lastNormalized = -1f;

        foreach (var t in totalTimeTexts)
            t.SetText("00:00");

        if (currentTimeText != null)
            currentTimeText.SetText("00:00");

        foreach (var slider in timeSliders)
            if (slider != null && slider.gameObject.activeInHierarchy)
                slider.SetValueWithoutNotify(0f);
    }

    // -------------------------------------------------------
    void Update()
    {
        // 준비 안 됐으면 즉시 탈출
        if (!_isPrepared || videoPlayer == null) return;

        float length = (float)videoPlayer.length;
        if (length <= 0.0001f) return;

        float current    = (float)videoPlayer.time;
        int   currentSec = Mathf.FloorToInt(current);

        // 현재 시간: 초가 바뀔 때만 텍스트 갱신
        if (currentSec != _lastTimeSec)
        {
            if (currentTimeText != null)
                currentTimeText.SetText(FormatTime(current));
            _lastTimeSec = currentSec;
        }

        // 드래그 중이면 슬라이더는 건드리지 않음
        if (_isDragging) return;

        float normalized = Mathf.Clamp01(current / length);

        // 슬라이더: 임계값 이상 변했을 때만 갱신
        if (Mathf.Abs(normalized - _lastNormalized) > 0.0001f)
        {
            foreach (var slider in timeSliders)
                if (slider != null && slider.gameObject.activeInHierarchy)
                    slider.SetValueWithoutNotify(normalized);

            _lastNormalized = normalized;
        }
    }

    // -------------------------------------------------------
    void AddEvent(EventTrigger trigger, EventTriggerType type,
                  UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }

    // -------------------------------------------------------
    // Slider → Seek
    // -------------------------------------------------------
    public void OnSliderChanged(float value)
    {
        if (!_isPrepared || videoPlayer == null) return;

        videoPlayer.time = value * videoPlayer.length;

        foreach (var slider in timeSliders)
            slider.SetValueWithoutNotify(value);

        // seek 후 캐시 무효화 → 다음 Update에서 텍스트 즉시 갱신
        _lastTimeSec    = -1;
        _lastNormalized = -1f;
    }

    // -------------------------------------------------------
    // 드래그 시작
    // -------------------------------------------------------
    public void OnPointerDown(BaseEventData eventData)
    {
        _isDragging = true;
        videoPlayer.Pause();

        if (screenManager != null)
            screenManager.BeginInteractionBlock();
    }

    // -------------------------------------------------------
    // 드래그 종료
    // -------------------------------------------------------
    public void OnPointerUp(BaseEventData eventData)
    {
        _isDragging = false;

        if (_isPrepared && videoPlayer != null)
            videoPlayer.Play();

        if (screenManager != null)
            screenManager.EndInteractionBlock();
    }

    // -------------------------------------------------------
    // GC-Free 시간 포맷 (StringBuilder 재사용)
    // -------------------------------------------------------
    private string FormatTime(float time)
    {
        int m = Mathf.FloorToInt(time / 60f);
        int s = Mathf.FloorToInt(time % 60f);

        _sb.Clear();
        if (m < 10) _sb.Append('0');
        _sb.Append(m);
        _sb.Append(':');
        if (s < 10) _sb.Append('0');
        _sb.Append(s);

        return _sb.ToString(); // 최종 string 생성은 1회만
    }
}