using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MediapipeMiniGameTimeManager : MonoBehaviour
{
    public static MediapipeMiniGameTimeManager Instance;

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

    public float maxTime;

    public TMP_Text currentTimeText;

    public GameObject resultUI;

    public float currentTime = 0;

    Slider timeSlider;

    public bool isStart = false;

    bool isWin = false;

    void Start()
    {
        currentTime = maxTime;

        timeSlider = GetComponentInChildren<Slider>();

        resultUI = GameObject.Find("ResultPanel");

        resultUI.SetActive(false);

        timeSlider.value = 1f;
        UpdateTimerText(currentTime);

        //StartCoroutine(StartGameEvent());
    }

    void Update()
    {
        if(!isStart) return;
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            timeSlider.value = currentTime / maxTime;
            UpdateTimerText(currentTime);
        }
        else if (currentTime < 0)
        {
            currentTime = 0;
            timeSlider.value = 0f;
            UpdateTimerText(0);

            UpdateResultUI("실패");
            //GameEndEvent();
            MediapipeManager.Instance.ProcessKill();
        }
    }

    public void ClearScore()
    {
        // 제한시간 내 깼을 경우
        if (currentTime > 0)
        {
            print($"currentTime: {currentTime}");
            print($"maxTime: {maxTime}");
            UpdateResultUI("성공");
            UIController uIController = GameObject.FindObjectOfType<UIController>();
            uIController.boolEvent = true;
            StartCoroutine(uIController.IChangeBoolEvent(1.5f, false));

            
            // 남은 시간이 전체 제한시간의 40% 이상일 경우
            if (currentTime > maxTime * 0.4f)
            {
                PlayerInfo.instance.PointPlusOrMinus(100); // 제한시간 40% 이상, 포인트 100 추가
                //QuestManger.instance.Questproceed(0, 1);
                //GameEndEvent();
                return;
            }
            // 남은 시간이 전체 제한시간의 20% 이상일 경우
            else if (currentTime > maxTime * 0.2f)
            {
                PlayerInfo.instance.PointPlusOrMinus(60); // 제한시간 20% 이상, 포인트 60 추가
                //QuestManger.instance.Questproceed(0, 1);
                //GameEndEvent();
                return;
            }

            PlayerInfo.instance.PointPlusOrMinus(30); // 제한시간 내 클리어, 포인트 30 추가
            //QuestManger.instance.Questproceed(0, 1);

            //GameEndEvent();
        }
        isStart = false;
    }

    void GameEndEvent()
    {
        
        StartCoroutine(UIController.instance.FadeIn("Canvas_TrashCan", 0.5f));
        StartCoroutine(UIController.instance.FadeIn("ChatCanvas", 0.5f));
        StartCoroutine(UIController.instance.FadeIn("Canvas_Player", 0.5f));
        StartCoroutine(UIController.instance.FadeIn("Canvas_MainUI", 0.5f));
        StartCoroutine(UIController.instance.FadeIn("InteractionCanvas", 0.5f));

        

        Camera.main.transform.localPosition = PlayerInfo.instance.cameraPos;
        CameraRotate cameraRotate = GameObject.FindObjectOfType<CameraRotate>();
        PlayerMove playerMove = GameObject.FindObjectOfType<PlayerMove>();

        cameraRotate.useRotX = true;
        cameraRotate.useRotY = true;
        playerMove.isMove = true;

        if (isWin)
        {
            QuestManger.instance.Questproceed(0, 1);
        }
        transform.parent.gameObject.SetActive(false);
    }




    public void UpdateResultUI(string value)
    {
        resultUI.SetActive(true);
        TMP_Text tMP_Text = resultUI.GetComponentInChildren<TMP_Text>();
        tMP_Text.text = value;
        if (value == "성공")
        {
            isWin = true;
        }
        StartCoroutine(ICloseMediapipeUI(value));

    }

    IEnumerator ICloseMediapipeUI(string isWin)
    {
        yield return new WaitForSeconds(2);
        

        GameEndEvent();
        
    }


    // 시간 값을 "분:초" 형식으로 업데이트하는 함수
    private void UpdateTimerText(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60); // 분 계산
        int seconds = Mathf.FloorToInt(time % 60); // 초 계산
        currentTimeText.text = string.Format("{0:00}:{1:00}", minutes, seconds); // "분:초" 형식으로 출력
    }
}
