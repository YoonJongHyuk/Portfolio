using UnityEngine;
using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using System.Collections.Concurrent;
using System.Reflection.Emit;

[Serializable]
public class MediapipeJson
{
    public string lr;
    public int x;
    public int y;
    public bool isFist;
}

public class MediapipeManager : MonoBehaviour
{
    public enum PythonName
    {
        test,
        nullType,
        ref3,
        mp
    }

    public static MediapipeManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    public bool createBrushTrue = false;

    private int currentTrashCount = 0;


    public TMP_Text currentTrashCountText;

    public PythonName pythonName = PythonName.test;


    public Process pythonProcess;

    public GameObject leftBrushImage;
    //public GameObject rightBrushImage;

    private RectTransform leftBrushRect;
    //private RectTransform rightBrushRect;

    MediapipeJson mediapipeJson;

    Vector2 leftPos;

    private ConcurrentQueue<MediapipeJson> jsonQueue = new ConcurrentQueue<MediapipeJson>();

    MediapipeTrashFactory trashFactory;

    public void ProcessKill()
    {
        pythonProcess.Kill();
    }

    

    public void CleanTrashEvent()
    {
        currentTrashCount--;

        if (currentTrashCount == 0)
        {
            print($"currentTrashCount: {currentTrashCount}");
            MediapipeMiniGameTimeManager.Instance.ClearScore();
            currentTrashCountText.text = $"남은 기름: {currentTrashCount}";
            print("게임 끝");
            ProcessKill();
            return;
        }
        else
        {
            currentTrashCountText.text = $"남은 기름: {currentTrashCount}";
        }
    }



    void Start()
    {
        ResetManager resetManager = PlayerInfo.instance.player.transform.GetComponentInChildren<ResetManager>();
        if (resetManager == null) UnityEngine.Debug.Log("resetManager == null");
        Button mediapipeClearButton = resetManager.buttonList[3];
        if (mediapipeClearButton == null) UnityEngine.Debug.Log("mediapipeClearButton == null");
        mediapipeClearButton.onClick.AddListener(MediapipeMiniGameTimeManager.Instance.ClearScore);

        trashFactory = GameObject.FindWithTag("TrashParent").GetComponent<MediapipeTrashFactory>();

        CloseOtherUI();


        currentTrashCount = 32;
        currentTrashCountText.text = $"남은 기름: {currentTrashCount}";
        leftBrushRect = leftBrushImage.GetComponent<RectTransform>();

        leftBrushImage.SetActive(false);



        ExecutePythonFile();
    }

    void CloseOtherUI()
    {
        StartCoroutine(UIController.instance.FadeOut("Canvas_TrashCan", 0.5f));
        StartCoroutine(UIController.instance.FadeOut("ChatCanvas", 0.5f));
        StartCoroutine(UIController.instance.FadeOut("Canvas_Player", 0.5f));
        StartCoroutine(UIController.instance.FadeOut("Canvas_MainUI", 0.5f));
        StartCoroutine(UIController.instance.FadeOut("InteractionCanvas", 0.5f));
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

            // Python 출력 발생 시 처리할 이벤트 핸들러 연결
            pythonProcess.OutputDataReceived += new DataReceivedEventHandler(OnOutputDataReceived);


            try
            {
                // 프로세스 시작
                pythonProcess.Start();
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
                MediapipeJson mediapipeJson = JsonUtility.FromJson<MediapipeJson>(e.Data);
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
        if (lr == "Left")
        {
            if(!leftBrushImage.activeSelf)
            {
                SetBrushEvent(true, leftBrushImage);
            }

            y += 1080;
            Vector2 leftPos = new Vector2(x, y);
            leftBrushRect.anchoredPosition = leftPos;
        }
        else if (lr == "Right")
        {
            if (!leftBrushImage.activeSelf)
            {
                SetBrushEvent(true, leftBrushImage);
            }
            // 다른 처리
            y += 1080;
            Vector2 rightPos = new Vector2(x, y);
            leftBrushRect.anchoredPosition = rightPos;
        }
        UnityEngine.Debug.Log($"isFist : {isFist}");
    }

    private void Update()
    {
        // 큐에 데이터가 있으면 처리
        if (jsonQueue.TryDequeue(out MediapipeJson mediapipeJson))
        {
            MoveBrushImage(mediapipeJson.lr, mediapipeJson.x, -mediapipeJson.y, mediapipeJson.isFist);
        }
    }

    private void OnApplicationQuit()
    {
        // 애플리케이션이 종료될 때 Python 프로세스가 남아있다면 종료
        if (pythonProcess != null || !pythonProcess.HasExited)
        {
            print("0");
            pythonProcess.Kill();
        }
    }
}
