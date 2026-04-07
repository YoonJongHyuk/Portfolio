using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;
using System.Collections.Generic;


public class Video_Volume : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private GameObject volumeSliderObject;

    [SerializeField] private Slider volumeSlider;

    [Header("Auto Hide Block")]
    [SerializeField] private Video_Screen_Manager screenManager;

    private bool isDragging = false;

    void Awake()
    {
        var trigger = volumeSliderObject.AddComponent<EventTrigger>();
        AddEvent(trigger, EventTriggerType.PointerDown, OnPointerDown);
         AddEvent(trigger, EventTriggerType.PointerUp, OnPointerUp);
    }

    void AddEvent(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        var entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }

    // -----------------------
    // 드래그 시작
    // -----------------------
    public void OnPointerDown(BaseEventData eventData)
    {
        isDragging = true;

        if (screenManager != null)
            screenManager.BeginInteractionBlock();

    }

    // -----------------------
    // 드래그 종료
    // -----------------------
    public void OnPointerUp(BaseEventData eventData)
    {
        isDragging = false;

        if (screenManager != null)
            screenManager.EndInteractionBlock();

    }


    private void Start()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer is not assigned in the inspector.");
        }
        // 슬라이더의 범위를 0~1로 설정
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        
        // 현재 볼륨으로 슬라이더 초기화
        volumeSlider.value = videoPlayer.GetDirectAudioVolume(0);

        // 이벤트 리스너 등록
        volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    public void ToggleVolume()
    {
        if (volumeSlider != null)
        {
            volumeSliderObject.SetActive(!volumeSliderObject.activeSelf);
        }
    }

    public void SetVolume(float volume)
    {
        if (videoPlayer != null)
        {
            videoPlayer.SetDirectAudioVolume(0, volume);
        }
    }
}
