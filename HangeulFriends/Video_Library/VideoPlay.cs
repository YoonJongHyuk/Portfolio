using UnityEngine;
using UnityEngine.Video;
using System;
using UnityEngine.Events;

public class VideoPlay : MonoBehaviour
{
    private VideoPlayer _vp;
    private VideoData _data;

    private bool _isPreparing;
    private bool _isPlaying;

    // 종료 처리 중복 방지
    private bool _finishHandled;

    public bool IsPreparing => _isPreparing;
    public bool IsPlaying => _isPlaying;

    public Action OnStateChanged;
    public UnityEvent OnStarCountingEvent;

    [SerializeField] private double endDetectMargin = 0.15f;

    public void Init(VideoPlayer vp, VideoData data)
    {
        _vp = vp;
        _data = data;

        if (_vp == null || _data == null)
        {
            Debug.LogError("[VideoPlay] Init 실패: VideoPlayer 또는 VideoData가 null");
            return;
        }

        ApplyVideoSource();
        BindEvents();

        _isPreparing = false;
        _isPlaying = false;
        _finishHandled = false;

        OnStateChanged?.Invoke();
    }

    public void SetVideoData(VideoData data, bool stopAndReset = true)
    {
        _data = data;
        if (_vp == null || _data == null) return;

        ApplyVideoSource();

        if (stopAndReset)
        {
            if (_vp.isPlaying)
                _vp.Stop();

            _isPreparing = false;
            _isPlaying = false;
            _finishHandled = false;

            OnStateChanged?.Invoke();
        }
    }

    public void TogglePlayPause()
    {
        if (_vp == null || _data == null)
        {
            Debug.LogWarning("[VideoPlay] TogglePlayPause: Init이 먼저 필요합니다.");
            return;
        }

        if (_isPreparing)
        {
            Debug.Log("[VideoPlay] Prepare 중...");
            return;
        }

        if (_vp.isPlaying)
        {
            _vp.Pause();
            _isPlaying = false;
            OnStateChanged?.Invoke();
            return;
        }

        if (!_vp.isPrepared)
        {
            _isPreparing = true;
            _isPlaying = false;
            _finishHandled = false;

            OnStateChanged?.Invoke();
            _vp.Prepare();
            return;
        }

        _finishHandled = false;
        _vp.Play();
        _isPlaying = true;
        OnStateChanged?.Invoke();
    }

    private void Update()
    {
        if (_vp == null) return;
        if (_isPreparing) return;
        if (!_isPlaying) return;
        if (_finishHandled) return;
        if (!_vp.isPrepared) return;
        if (_vp.isLooping) return;

        // 1. frame 기준 fallback
        if (_vp.frameCount > 0 && _vp.frame >= 0)
        {
            long lastFrame = (long)_vp.frameCount - 1;
            if (_vp.frame >= lastFrame)
            {
                Debug.Log($"[VideoPlay Fallback] frame end detected. frame={_vp.frame}, frameCount={_vp.frameCount}");
                FinishVideo("frame fallback");
                return;
            }
        }

        // 2. time 기준 fallback
        if (_vp.length > 0 && _vp.time >= _vp.length - endDetectMargin)
        {
            Debug.Log($"[VideoPlay Fallback] time end detected. time={_vp.time:F3}, length={_vp.length:F3}");
            FinishVideo("time fallback");
            return;
        }

        // 3. 특정 기기에서 isPlaying만 false 되는 경우
        if (!_vp.isPlaying && _vp.length > 0 && _vp.time >= _vp.length - 0.3f)
        {
            Debug.Log($"[VideoPlay Fallback] stopped near end. time={_vp.time:F3}, length={_vp.length:F3}");
            FinishVideo("stopped near end fallback");
        }
    }

    private void ApplyVideoSource()
    {
        string fileName = _data.fileName;
        if (!fileName.EndsWith(".mp4"))
            fileName += ".mp4";

        _vp.source = VideoSource.Url;
        _vp.url = VideoPathUtil.GetStreamingVideoUrl(fileName);

        Debug.Log($"[VideoPlay] ApplyVideoSource url={_vp.url}");
    }

    private void BindEvents()
    {
        _vp.prepareCompleted -= OnPrepared;
        _vp.errorReceived -= OnVideoError;
        _vp.started -= OnStarted;
        _vp.loopPointReached -= OnLoopPointReached;

        _vp.prepareCompleted += OnPrepared;
        _vp.errorReceived += OnVideoError;
        _vp.started += OnStarted;
        _vp.loopPointReached += OnLoopPointReached;
    }

    private void OnPrepared(VideoPlayer vp)
    {
        Debug.Log($"[VideoPlay] Prepared. length={vp.length}, frameCount={vp.frameCount}, frameRate={vp.frameRate}");

        _isPreparing = false;
        _isPlaying = false;
        _finishHandled = false;

        vp.Play();
    }

    private void OnStarted(VideoPlayer vp)
    {
        Debug.Log("[VideoPlay] Started");

        _isPlaying = true;
        _finishHandled = false;
        OnStateChanged?.Invoke();
    }

    private void OnLoopPointReached(VideoPlayer vp)
    {
        Debug.Log("[VideoPlay] loopPointReached");
        FinishVideo("loopPointReached");
    }

    private void FinishVideo(string reason)
    {
        if (_finishHandled) return;
        _finishHandled = true;

        Debug.Log($"[VideoPlay] FinishVideo reason={reason}, time={_vp.time:F3}, length={_vp.length:F3}, frame={_vp.frame}/{_vp.frameCount}");

        _isPreparing = false;
        _isPlaying = false;

        if (_vp != null)
            _vp.Stop();

        // 끝나면 반드시 호출
        Debug.Log("[VideoPlay] OnStarCountingEvent Invoke");
        OnStarCountingEvent?.Invoke();

        OnStateChanged?.Invoke();
    }

    private void OnVideoError(VideoPlayer vp, string msg)
    {
        Debug.LogError($"[VideoPlay] Video error: {msg}");

        _isPreparing = false;
        _isPlaying = false;
        _finishHandled = true;

        vp.Stop();
        OnStateChanged?.Invoke();
    }

    private void OnDisable()
    {
        if (_vp == null) return;

        _vp.prepareCompleted -= OnPrepared;
        _vp.errorReceived -= OnVideoError;
        _vp.started -= OnStarted;
        _vp.loopPointReached -= OnLoopPointReached;
    }
}