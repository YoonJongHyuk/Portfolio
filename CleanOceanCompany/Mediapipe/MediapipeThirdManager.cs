using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class MediapipeThirdJson
{
    public string lr;
    public int x;
    public int y;
    public bool isFist;
}

public class MediapipeThirdManager : MonoBehaviour
{
    public enum PythonName
    {
        mp
    }

    public bool createBrushTrue = false;
    bool lastFistType;

    public GameObject porpoise;

    private GameObject _brush;

    Vector2 lastPosition;  // 이전 위치 저장
    float radius;          // 반지름


    public float rotationSpeed = 10f;

    public Camera mediapipeCamera;

    public event Action<GameObject> OnChangedBrushObject;

    public BrushTriggerEvent brushTriggerEvent;

    public GameObject BrushObject
    {
        get
        {
            return _brush;
        }
        set
        {
            _brush = value;
            OnChangedBrushObject?.Invoke(_brush);
        }
    }

    //private int currentTrashCount = 0;


    //public TMP_Text currentTrashCountText;

    public PythonName pythonName = PythonName.mp;

    public Process pythonProcess;

    public GameObject leftBrushImage;

    private RectTransform leftBrushRect;

    public CanvasGroup handImageCg;

    MediapipeThirdJson mediapipeThirdJson;


    private ConcurrentQueue<MediapipeThirdJson> jsonQueue = new ConcurrentQueue<MediapipeThirdJson>();

    public MediapipeThirdTimeManager mediapipeThirdTimeManager;

    public void ProcessKill()
    {
        pythonProcess.Kill();
    }
    void Start()
    {
        


        //currentTrashCount = 32;
        //currentTrashCountText.text = $"남은 기름: {currentTrashCount}";
        leftBrushRect = leftBrushImage.GetComponent<RectTransform>();

        leftBrushImage.SetActive(false);


        
    }

    public void CloseOtherUI()
    {
        StartCoroutine(UIController.instance.FadeOut("Canvas_TrashCan", 0.5f));
        StartCoroutine(UIController.instance.FadeOut("ChatCanvas", 0.5f));
        StartCoroutine(UIController.instance.FadeOut("Canvas_Player", 0.5f));
        StartCoroutine(UIController.instance.FadeOut("Canvas_MainUI", 0.5f));
        StartCoroutine(UIController.instance.FadeOut("InteractionCanvas", 0.5f));

        ResetManager resetManager = PlayerInfo.instance.player.transform.GetComponentInChildren<ResetManager>();

        Interaction_UICamera_Controller interaction_UICamera_Controller = resetManager.interaction_UICamera.GetComponent<Interaction_UICamera_Controller>();
        interaction_UICamera_Controller.IsStart = false;

        if (resetManager == null) UnityEngine.Debug.Log("resetManager == null");
        Button mediapipeClearButton = resetManager.buttonList[3];
        if (mediapipeClearButton == null) UnityEngine.Debug.Log("mediapipeClearButton == null");
        mediapipeClearButton.onClick.AddListener(mediapipeThirdTimeManager.ClearScore);

    }

    public void SetRadius()
    {
        //Vector3 parentVec = new Vector3(mediapipeCamera.transform.position.x, 0, 0);
        //Vector3 porpoiseVec = new Vector3(porpoise.transform.position.x, 0, 0);
        //radius = Vector3.Distance(parentVec, porpoiseVec);
        //radius = 3.94714f;
        radius = Vector3.Distance(mediapipeCamera.transform.position, porpoise.transform.position);
        print($"radius : {radius}");
    }

    void SetBrushEvent(bool value, GameObject brush)
    {
        brush.SetActive(value);
    }


    public void ExecutePythonFile()
    {
        // StreamingAssets 폴더에 있는 Python 파일 경로를 가져옴

        string pythonFilePath = Path.Combine(Application.streamingAssetsPath, $"{pythonName.ToString()}.py");
        if (File.Exists(pythonFilePath))
        {
            // Python 실행 파일 경로
            string pythonPath = Path.Combine(Application.streamingAssetsPath, "PythonFile\\Python\\Python38\\python.exe");

            // 프로세스 실행 설정
            ProcessStartInfo processInfo = new ProcessStartInfo();
            processInfo.FileName = pythonPath;
            //processInfo.Arguments = pythonFilePath;
            processInfo.Arguments = $"-u {pythonFilePath}"; // -u 옵션 추가
            processInfo.RedirectStandardOutput = true; // 표준 출력을 리디렉션
            processInfo.RedirectStandardError = true;  // 표준 에러도 리디렉션 가능
            processInfo.StandardOutputEncoding = Encoding.UTF8;
            processInfo.UseShellExecute = false;
            processInfo.CreateNoWindow = true;

            pythonProcess = new Process();
            pythonProcess.StartInfo = processInfo;
            pythonProcess.EnableRaisingEvents = true;

            print($"pythonFilePath : {pythonFilePath}");


            // Python 출력 발생 시 처리할 이벤트 핸들러 연결
            pythonProcess.OutputDataReceived += new DataReceivedEventHandler(OnOutputDataReceived);


            try
            {
                // 프로세스 시작
                pythonProcess.Start();

                pythonProcess.PriorityClass = ProcessPriorityClass.High;
                // 출력 비동기적으로 읽기 시작
                pythonProcess.BeginOutputReadLine();
                pythonProcess.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("Python 실행 중 오류가 발생했습니다: " + ex.Message);
            }
        }
        else
        {
            UnityEngine.Debug.LogError("Python 파일을 찾을 수 없습니다: " + pythonFilePath);
        }
    }




    private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            UnityEngine.Debug.LogError("Python 에러: " + e.Data);  // 에러 메시지만 출력
        }
    }


    // Python 출력 처리
    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            // Mediapipe 데이터가 포함된 출력만 처리
            if (e.Data.Contains("{"))  // Mediapipe JSON 데이터가 있을 경우
            {
                MediapipeThirdJson mediapipeJson = JsonUtility.FromJson<MediapipeThirdJson>(e.Data);
                jsonQueue.Enqueue(mediapipeJson);  // 큐에 데이터 추가
            }
            else
            {
                UnityEngine.Debug.Log("Non-JSON Python Output: " + e.Data);  // 기타 Python 출력 처리
            }
        }
    }


    void MoveBrushImage(string lr, int x, int y, bool isFist)
    {
        Vector2 handPos = new Vector2(x, y);
        if (isFist != lastFistType)
        {
            if (isFist) // 브러쉬 조정 (손 핌)
            {
                lastFistType = isFist;
                handImageCg.alpha = 1;
            }
            else if (!isFist) // 회전 (주먹 쥠)
            {
                lastFistType = isFist;
                lastPosition = handPos;
                handImageCg.alpha = 0;
            }
        }

        if (isFist) // 브러쉬 조정 (손 핌)
        {
            lastFistType = isFist;
            if (lr == "Left")
            {
                if (!leftBrushImage.activeSelf)
                {
                    SetBrushEvent(true, leftBrushImage);
                }

                y += 1080;
                leftBrushRect.anchoredPosition = new Vector2(handPos.x, (handPos.y + 1080));
            }
            else if (lr == "Right")
            {
                if (!leftBrushImage.activeSelf)
                {
                    SetBrushEvent(true, leftBrushImage);
                }
                // 다른 처리
                y += 1080;
                leftBrushRect.anchoredPosition = new Vector2(handPos.x, (handPos.y + 1080));
            }
        }
        else if (!isFist) // 회전 (주먹 쥠)
        {
            y += 1080;
            // 변화량 계산
            float deltaX = handPos.x - lastPosition.x; // x 변화량
            float deltaY = handPos.y - lastPosition.y; // y 변화량

            // x축 기준으로 회전
            float speedX = deltaX * Time.deltaTime * rotationSpeed; // 회전 속도 계산
            mediapipeCamera.transform.RotateAround(
                porpoise.transform.position, // 회전 중심
                Vector3.up,                  // Y축 기준 회전
                speedX                       // 회전 각도 (deg)
            );

            // y축 기준으로 회전
            float speedY = deltaY * Time.deltaTime * rotationSpeed; // 회전 속도 계산
            mediapipeCamera.transform.RotateAround(
                porpoise.transform.position, // 회전 중심
                Vector3.right,               // X축 기준 회전
                speedY                       // 회전 각도 (deg)
            );

            // 반지름 고정: 중심에서의 거리 유지
            Vector3 offset = mediapipeCamera.transform.position - porpoise.transform.position;
            offset = offset.normalized * radius; // 반지름으로 위치 고정
            mediapipeCamera.transform.position = porpoise.transform.position + offset;

            // 카메라가 부모를 바라보게 설정
            mediapipeCamera.transform.LookAt(porpoise.transform);

            lastPosition = handPos;
        }
    }

    private void Update()
    {
        // 큐에 데이터가 있으면 처리
        if (jsonQueue.TryDequeue(out MediapipeThirdJson mediapipeJson))
        {
            MoveBrushImage(mediapipeJson.lr, mediapipeJson.x, -mediapipeJson.y, mediapipeJson.isFist);
        }
    }

    public void Event_FinishMediapipe()
    {
        if (pythonProcess != null || !pythonProcess.HasExited)
        {
            print("미디어파이프 프로그램 종료 실행");
            transform.parent.parent.rotation = Quaternion.identity;
            pythonProcess.Kill();
        }
        if (pythonProcess.HasExited)
        {
            print("완전히 종료됨.");
        }
    }


    private void OnApplicationQuit()
    {
        // 애플리케이션이 종료될 때 Python 프로세스가 남아있다면 종료
        Event_FinishMediapipe();
    }
}
