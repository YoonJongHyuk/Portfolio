using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ZXing;
using UnityEngine.Events;

public class QRScanner : MonoBehaviour
{
    [Header("Preview")]
    [SerializeField] private RawImage preview;

    [Header("Event Settings")]
    public UnityEvent OnStarCountingEvent;
    public UnityEvent OnOtherEvent;
    public UnityEvent OnFailEvent;

    [Header("Scan")]
    [SerializeField] private float scanInterval = 0.2f;
    [SerializeField] private bool autoStart = true;

    [Header("Panels")]
    public GameObject resultPanel;
    public GameObject failPanel;

    [Header("정답 단어")]
    public string successWord;

    [Tooltip("전면 카메라가 없을 때 후면으로 대체")]
    [SerializeField] private bool fallbackToBackCamera = true;

    private WebCamTexture camTex;
    private IBarcodeReader reader;
    private Color32[] buffer;

    private bool isScanning = false;
    private bool isDecoding = false;
    private Coroutine scanRoutine;
    private Coroutine failRoutine;

    private int scanCount = 0;

    private void OnEnable()
    {
        reader = new BarcodeReader
        {
            AutoRotate = true,
            TryInverted = true
        };

        if (autoStart)
            StartCamera();
    }

    private void OnDisable()
    {
        StopScan();
        StopCamera();
    }

    // =========================
    // Camera
    // =========================

    public void StartCamera()
    {
        if (camTex != null && camTex.isPlaying) return;

        var devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("카메라 없음");
            return;
        }

        int frontIndex = -1;
        for (int i = 0; i < devices.Length; i++)
        {
            if (devices[i].isFrontFacing)
            {
                frontIndex = i;
                break;
            }
        }

        int chosenIndex = frontIndex;
        if (chosenIndex < 0)
        {
            if (!fallbackToBackCamera)
            {
                Debug.LogError("전면 카메라를 찾지 못했습니다.");
                return;
            }

            chosenIndex = 0;
            Debug.LogWarning("전면 카메라 없음 → 후면 카메라 사용");
        }

        camTex = new WebCamTexture(devices[chosenIndex].name, 1280, 720, 30);
        preview.texture = camTex;
        camTex.Play();

        StartCoroutine(WaitForCameraReady());
    }

    private IEnumerator WaitForCameraReady()
    {
        while (camTex != null && camTex.width <= 16)
            yield return null;

        if (camTex != null)
        {
            FitWebCamToRawImage(preview, camTex);
            StartScan(); // 👉 카메라 준비 완료 후 자동 스캔 시작
        }
    }

    public void StopCamera()
    {
        if (camTex != null)
        {
            if (camTex.isPlaying)
                camTex.Stop();

            camTex = null;
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

    // =========================
    // Scan Control
    // =========================

    public void StartScan()
    {
        if (isScanning) return;

        if (buffer != null)
            System.Array.Clear(buffer, 0, buffer.Length);

        scanCount = 0;
        isScanning = true;

        if (scanRoutine != null)
            StopCoroutine(scanRoutine);

        scanRoutine = StartCoroutine(DecodeRoutine());
    }

    public void StopScan()
    {
        isScanning = false;
        isDecoding = false;

        if (scanRoutine != null)
        {
            StopCoroutine(scanRoutine);
            scanRoutine = null;
        }
    }

    private IEnumerator DecodeRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(scanInterval);

        while (isScanning)
        {
            if (!isDecoding)
                TryDecode();

            yield return wait;
        }

        scanRoutine = null;
    }

    private void TryDecode()
    {
        if (camTex == null || !camTex.isPlaying) return;
        if (camTex.width <= 16 || camTex.height <= 16) return;

        isDecoding = true;

        int width = camTex.width;
        int height = camTex.height;
        int size = width * height;

        if (buffer == null || buffer.Length != size)
            buffer = new Color32[size];

        camTex.GetPixels32(buffer);

        var result = reader.Decode(buffer, width, height);

        if (result == null)
        {
            isDecoding = false;
            return;
        }

        Debug.Log("QR: " + result.Text);

        if (!TryParseWord(result.Text, out string word))
        {
            ShowFail();
            isDecoding = false;
            return;
        }

        if (!string.IsNullOrEmpty(successWord) && word != successWord)
        {
            ShowFail();
            isDecoding = false;
            return;
        }

        ShowResult(word);

        StopScan();
        isDecoding = false;
    }

    // =========================
    // Result UI
    // =========================

    private void ShowResult(string word)
    {
        Debug.Log("QR 인식 성공: " + word);
        resultPanel.SetActive(true);
        StartCoroutine(IEndEvent());
    }

    private void ShowResult()
    {
        resultPanel.SetActive(true);
        StartCoroutine(IEndEvent());
    }

    private IEnumerator IEndEvent()
    {
        OnOtherEvent?.Invoke();
        yield return new WaitForSeconds(2f);
        OnStarCountingEvent?.Invoke();
    }

    private void ShowFail()
    {
        if (failRoutine != null)
            StopCoroutine(failRoutine);

        failRoutine = StartCoroutine(FailRoutine());
    }

    private IEnumerator FailRoutine()
    {
        StopScan();

        OnFailEvent?.Invoke();

        failPanel.SetActive(true);

        yield return new WaitForSeconds(2f); // 👈 원하는 시간

        failPanel.SetActive(false);

        StartScan(); // 👈 카메라는 이미 켜져있으니 Scan만 다시 시작

        failRoutine = null;
    }



    // =========================
    // QR Parsing
    // =========================

    private bool TryParseWord(string qrText, out string word)
    {
        word = null;

        if (string.IsNullOrEmpty(qrText))
            return false;

        qrText = qrText.Trim();

        if (!qrText.StartsWith("{") || !qrText.EndsWith("}"))
            return false;

        QRJsonData data;

        try
        {
            data = JsonUtility.FromJson<QRJsonData>(qrText);
        }
        catch
        {
            return false;
        }

        if (data == null || string.IsNullOrEmpty(data.word))
            return false;

        word = data.word;
        return true;
    }
}
