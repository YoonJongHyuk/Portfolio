using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{

    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        // 이미 인스턴스가 존재하면 중복 파괴
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


    private Main_UI main_UI;
    public Dinoinfo_UI dinoinfo_UI;
    public Select_DinoList select_DinoList;

    public List<GameObject> uiObjects;

    public int currentdinosaurnum = 0;

    public int number = 0;

    public Button loginButton;
    public Button joinButton;
    public Button joinpostButton;

    private Button currentButton;
    private bool hasSelected = false;


    private void Start()
    {
        SetOnOffUI(0, true);
    }

    private void Update()
    {

        if (number == 0 && OVRInput.GetDown(OVRInput.RawButton.A))
        {
            main_UI = uiObjects[0].GetComponent<Main_UI>();
            main_UI.ManiUI_GetMenu_btn();
        }
        else if (number == 1 && OVRInput.GetDown(OVRInput.RawButton.A))
        {
            main_UI.Go_to_List_GetMenu_btn();
        }
        else if (number == 2)
        {
            SelectLoginOrJoin();
        }
        else if (number == 3)
        {
            CurrentButtonChange(joinpostButton);
            CurrentButtonClick(joinpostButton);
        }
        else if (number == 4)
        {

        }
        else if (number == 5 && OVRInput.GetDown(OVRInput.RawButton.A))
        {
            GameManager.Instance.MoveScene("MapScene");
        }
        else if (number == 5 && OVRInput.GetDown(OVRInput.RawButton.B))
        {
            dinoinfo_UI.dinoInfoList[currentdinosaurnum].SetActive(false);
            select_DinoList.CloseModel(currentdinosaurnum);
            number = 4;
            SetOnOffUI(1, false);
            SetOnOffUI(0, true);
        }
    }

    public void SetDinosaurUI(int num, bool open)
    {
        if (num == 0)
        {
            dinoinfo_UI.dinoInfoList[num].SetActive(open);
        }
        else if (num == 1)
        {
            dinoinfo_UI.dinoInfoList[num].SetActive(open);
        }
        else if (num == 2)
        {
            dinoinfo_UI.dinoInfoList[num].SetActive(open);
        }
    }

    public void SelectLoginOrJoin()
    {
        Vector2 rightStick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

        if (!hasSelected)
        {
            if (rightStick.x < -0.5f)
            {
                print("왼쪽 이동");
                CurrentButtonChange(loginButton);
                hasSelected = true;
            }
            else if (rightStick.x > 0.5f)
            {
                print("오른쪽 이동");
                CurrentButtonChange(joinButton);
                hasSelected = true;
            }
        }

        // 조이스틱이 중립으로 돌아오면 다시 선택 가능
        if (rightStick.magnitude < 0.2f)
        {
            hasSelected = false;
        }

        if (currentButton != null && OVRInput.GetDown(OVRInput.RawButton.A))
        {
            CurrentButtonClick(currentButton);
        }
    }


    public void CurrentButtonChange(Button button)
    {
        button.Select();
        currentButton = button;
    }

    public void CurrentButtonClick(Button button)
    {
        if (button != null && OVRInput.GetDown(OVRInput.RawButton.A))
        {
            button.onClick.Invoke();
        }
    }






    public void SetOnOffUI(int num, bool open)
    {
        uiObjects[num].SetActive(open);
    }

    

}
