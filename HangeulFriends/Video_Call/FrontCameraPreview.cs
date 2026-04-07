using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FrontCameraPreview : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private List<RawImage> preview;

    [Header("Camera Options")]
    [SerializeField] private int requestedWidth = 1280;
    [SerializeField] private int requestedHeight = 720;
    [SerializeField] private int requestedFPS = 30;

    [Tooltip("전면 카메라가 없을 때 후면으로 대체")]
    [SerializeField] private bool fallbackToBackCamera = true;

    private WebCamTexture camTex;

    void OnEnable()
    {
        StartFrontCamera();
    }

    void OnDisable()
    {
        StopCamera();
    }

    // =========================
    // Public
    // =========================

    public void StartFrontCamera()
    {
        if (preview == null)
        {
            Debug.LogError("preview(RawImage) 가 연결되지 않았습니다.");
            return;
        }

        StopCamera();

        var devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0)
        {
            Debug.LogError("카메라 디바이스가 없습니다.");
            return;
        }

        // 1) 전면 카메라 찾기
        int frontIndex = -1;
        for (int i = 0; i < devices.Length; i++)
        {
            if (devices[i].isFrontFacing)
            {
                frontIndex = i;
                break;
            }
        }

        // 2) 없으면 후면 대체
        int chosenIndex = frontIndex;
        if (chosenIndex < 0)
        {
            if (!fallbackToBackCamera)
            {
                Debug.LogError("전면 카메라를 찾지 못했습니다.");
                return;
            }
            chosenIndex = 0;
            Debug.LogWarning("전면 카메라 없음 → 첫 번째 카메라로 대체합니다.");
        }

        camTex = new WebCamTexture(devices[chosenIndex].name, requestedWidth, requestedHeight, requestedFPS);
        foreach (var p in preview)
        {
            p.texture = camTex;
        }
        camTex.Play();

        StartCoroutine(WaitForCameraReadyThenFit());
    }

    public void StopCamera()
    {
        if (camTex != null)
        {
            if (camTex.isPlaying) camTex.Stop();
            camTex = null;
        }
    }

    // =========================
    // Internal
    // =========================

    private IEnumerator WaitForCameraReadyThenFit()
    {
        // 카메라 준비될 때까지 대기
        while (camTex != null && camTex.width <= 16)
            yield return null;

        if (camTex == null) yield break;

        foreach (var p in preview)
        {
            FitWebCamToRawImage(p, camTex);
        }

        // 모바일에서 전면은 대개 미러처럼 보이는 게 자연스러움
        // 필요하면 아래 줄을 주석 처리해서 '거울효과'를 끌 수 있음
        foreach (var p in preview)
        {
            ApplyMirrorIfFrontFacing(p, camTex);
        }
    }

    private void FitWebCamToRawImage(RawImage raw, WebCamTexture cam)
    {
        float camAspect = (float)cam.width / cam.height;
        float rawAspect = raw.rectTransform.rect.width / raw.rectTransform.rect.height;

        Rect uv = new Rect(0, 0, 1, 1);

        if (camAspect > rawAspect)
        {
            float scale = rawAspect / camAspect;
            uv.x = (1f - scale) / 2f;
            uv.width = scale;
        }
        else
        {
            float scale = camAspect / rawAspect;
            uv.y = (1f - scale) / 2f;
            uv.height = scale;
        }

        raw.uvRect = uv;
    }

    private void ApplyMirrorIfFrontFacing(RawImage raw, WebCamTexture cam)
    {
        // WebCamTexture는 “현재 사용 중인 디바이스가 전면인지” 정보를 직접 주지 않아서
        // 가장 안전한 방식은: 지금 선택한 디바이스가 전면일 때만 스케일을 뒤집는 것.
        // 여기선 cam.deviceName을 기반으로 devices를 다시 찾아 판별한다.

        var devices = WebCamTexture.devices;
        bool isFront = false;

        for (int i = 0; i < devices.Length; i++)
        {
            if (devices[i].name == cam.deviceName)
            {
                isFront = devices[i].isFrontFacing;
                break;
            }
        }

        var rt = raw.rectTransform;
        Vector3 s = rt.localScale;

        // 전면이면 X를 -로 뒤집어서 거울처럼 보이게
        if (isFront)
        {
            rt.localScale = new Vector3(-Mathf.Abs(s.x), Mathf.Abs(s.y), s.z);
        }
        else
        {
            rt.localScale = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), s.z);
        }
    }
}
