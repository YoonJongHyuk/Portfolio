using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AudioTimeBar : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Slider timeBar;
    [SerializeField] private float currentTime = 0f;
    [SerializeField] private IconChange iconChange;
    [SerializeField] private AudioPlayer audioPlayer;

    private bool isDragging = false;
    private bool wasPlayingBeforeDrag = false;

    void Start()
    {
        // 슬라이더가 이미 0이 아닌 위치에 있다면 첫 재생 시작 위치로 반영
        if (timeBar.value > 0f)
            audioPlayer.pendingStartTime = timeBar.value;

        timeBar.onValueChanged.AddListener(OnSliderChanged);

        var trigger = timeBar.gameObject.GetComponent<EventTrigger>()
                   ?? timeBar.gameObject.AddComponent<EventTrigger>();

        // 터치/클릭 시작: 재생 중이면 멈추고, 즉시 시간 이동
        var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        pointerDown.callback.AddListener(_ =>
        {
            isDragging = true;
            wasPlayingBeforeDrag = audioSource.isPlaying;
            if (wasPlayingBeforeDrag) audioPlayer.PauseAudio();
            ApplyTime(timeBar.value); // 터치 지점으로 즉시 이동
        });
        trigger.triggers.Add(pointerDown);

        // 터치/클릭 종료: 원래 재생 중이었으면 재개
        var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        pointerUp.callback.AddListener(_ =>
        {
            isDragging = false;
            if (wasPlayingBeforeDrag) audioPlayer.ResumeAudio();
        });
        trigger.triggers.Add(pointerUp);
    }

    void Update()
    {
        if (audioSource == null || audioSource.clip == null) return;

        iconChange.ChangeIcon(audioSource.isPlaying);

        // 드래그 중이 아닐 때만 오디오 시간 → 슬라이더 업데이트
        if (!isDragging)
        {
            currentTime = audioSource.time;
            timeBar.value = currentTime / audioSource.clip.length;
        }
    }

    private void OnSliderChanged(float value)
    {
        if (!isDragging) return;
        ApplyTime(value);
    }

    // 실제 시간 이동 처리 (터치/드래그 공통)
    private void ApplyTime(float normalizedValue)
    {
        if (audioSource.clip != null)
        {
            // 클립이 이미 로드된 경우: 바로 시간 이동
            audioSource.time = normalizedValue * audioSource.clip.length;
        }
        else
        {
            // 첫 재생 전: normalized 값을 AudioPlayer에 저장
            // LoadAndPlay에서 clip.length를 곱해 실제 시간으로 적용
            audioPlayer.pendingStartTime = normalizedValue;
        }
    }

    void OnDestroy()
    {
        timeBar.onValueChanged.RemoveListener(OnSliderChanged);
    }
}