using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class Quiz_UI_Setting : MonoBehaviour
{
    [Header("Data")]
    public VideoData videoData;

    [Header("Sprite")]
    [SerializeField] private Sprite polygonSprite;

    [Header("References")]
    [SerializeField] private RectTransform sliderRect;
    [SerializeField] private RectTransform polygonsRoot;
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Placement")]
    [SerializeField] private float posY = 25f;
    [SerializeField] private float polygonSize = 20f;

    private readonly List<GameObject> polygonList = new();
    private bool built;
    private Coroutine waitCoroutine;

    private void OnEnable()
    {
        if (videoPlayer != null)
            videoPlayer.prepareCompleted += OnPrepared;

        TryBuildNowOrWait();
    }

    private void OnDisable()
    {
        if (videoPlayer != null)
            videoPlayer.prepareCompleted -= OnPrepared;

        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }
    }

    private void OnPrepared(VideoPlayer vp)
    {
        Debug.Log($"[Quiz_UI_Setting] OnPrepared length={vp.length}");
        TryBuildNowOrWait();
    }

    public void SetQuizTiming(QuizTiming timing)
    {
        if (videoData == null) return;

        videoData.quizTiming = timing;

        built = false;
        TryBuildNowOrWait();
    }

    public void TryBuildNowOrWait()
    {
        if (built) return;
        if (videoData == null) return;

        float resolvedLength = ResolveVideoLength();

        Debug.Log($"[Quiz_UI_Setting] TryBuildNowOrWait resolvedLength={resolvedLength}");

        if (IsValidLength(resolvedLength))
        {
            bool success = BuildPolygons(resolvedLength);
            if (success)
            {
                built = true;
                return;
            }
        }

        StartWaitIfNeeded();
    }

    private void StartWaitIfNeeded()
    {
        if (waitCoroutine != null) return;
        waitCoroutine = StartCoroutine(WaitForValidLengthThenBuild());
    }

    private IEnumerator WaitForValidLengthThenBuild()
    {
        while (!built)
        {
            float resolvedLength = ResolveVideoLength();

            if (IsValidLength(resolvedLength))
            {
                bool success = BuildPolygons(resolvedLength);
                if (success)
                {
                    built = true;
                    break;
                }
            }

            yield return null;
        }

        waitCoroutine = null;
    }

    private float ResolveVideoLength()
    {
        // 1순위: 실제 VideoPlayer 길이
        if (videoPlayer != null && videoPlayer.isPrepared)
        {
            float playerLen = (float)videoPlayer.length;
            if (IsValidLength(playerLen))
                return playerLen;
        }

        // 2순위: 데이터에 저장된 길이
        if (videoData != null)
        {
            float dataLen = videoData.videoLength;
            if (IsValidLength(dataLen))
                return dataLen;
        }

        return 0f;
    }

    private bool IsValidLength(float value)
    {
        return value > 0.0001f && !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public bool BuildPolygons(float videoLength)
    {
        if (videoData == null)
        {
            Debug.LogWarning("[Quiz_UI_Setting] videoData is null");
            return false;
        }

        if (videoData.quizTiming == null || videoData.quizTiming.quizTime == null || videoData.quizTiming.quizTime.Count == 0)
        {
            Debug.LogWarning("[Quiz_UI_Setting] quizTiming is empty");
            return false;
        }

        if (sliderRect == null || polygonSprite == null)
        {
            Debug.LogWarning("[Quiz_UI_Setting] sliderRect or polygonSprite is null");
            return false;
        }

        if (!IsValidLength(videoLength))
        {
            Debug.LogWarning($"[Quiz_UI_Setting] invalid videoLength: {videoLength}");
            return false;
        }

        RectTransform parent = polygonsRoot != null ? polygonsRoot : sliderRect;

        ClearPolygons();

        float width = sliderRect.rect.width;

        foreach (float t in videoData.quizTiming.quizTime)
        {
            if (float.IsNaN(t) || float.IsInfinity(t))
                continue;

            float normalized = Mathf.Clamp01(t / videoLength);
            float posX = (normalized * width) - (width * 0.5f);

            GameObject go = new GameObject("QuizPolygon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            Image img = go.GetComponent<Image>();
            img.sprite = polygonSprite;

            RectTransform rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(polygonSize, polygonSize);
            rt.anchoredPosition = new Vector2(posX, posY);

            polygonList.Add(go);
        }

        Debug.Log($"[Quiz_UI_Setting] BuildPolygons success. length={videoLength}, count={polygonList.Count}");
        return true;
    }

    public void ClearPolygons()
    {
        for (int i = 0; i < polygonList.Count; i++)
        {
            if (polygonList[i] != null)
                Destroy(polygonList[i]);
        }

        polygonList.Clear();
    }
}