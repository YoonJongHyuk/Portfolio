using Unity.XRTools.Rendering;
using UnityEngine;


/// <summary>
/// 외부 오브젝트와 트리거로 인터렉션 할때 쓸 스크립트
/// </summary>
public class PlayerInputManger : MonoBehaviour
{
    /// <summary> 플레이어의 오른손 오브젝트 </summary>
    GameObject rightHand;

    /// <summary> 플레이어의 왼손 오브젝트 </summary>
    GameObject leftHand;

    /// <summary> 플레이어가 가장 최근에 사용한 손 </summary>
    GameObject usedHand;

    /// <summary> 어느쪽 손을 사용중인지 판단할 Bool 변수 </summary>
    bool isUseRight;

    /// <summary> 상호작용 가능한 오브젝트에 Raycast가 Hit 했는지 판단할 bool 변수 </summary>
    bool isHit;

    /// <summary> 해당 컴포넌트가 존재하는 게임오브젝트에 현재 상호작용 중인 컴포넌트 </summary>
    GameUI_Interaction subscribed_UI;

    /// <summary> VR 전용 LineRenderer  </summary>
    XRLineRenderer lineRenderer;

    /// <summary> 중복 입력 방지 쿨타임  </summary>
    float coolTime = 1.0f;

    /// <summary> 현재 쿨타임 체크 타임 </summary>
    float currnetCoolTime = 0;

    private void Start()
    {
        // 필요한 변수들 초기 값 할당
        rightHand = GameObject.FindGameObjectWithTag("RightHand");
        leftHand = GameObject.FindGameObjectWithTag("LeftHand");
        lineRenderer = GetComponent<XRLineRenderer>();
    }

    private void Update()
    {
        // 최근 사용한 손을 바탕으로 사용될 손 할당
        usedHand = isUseRight ? rightHand : leftHand;
        if (currnetCoolTime > coolTime)
        {
            Interaction();
        }
        else
        {
            currnetCoolTime += Time.deltaTime;
        }
        //LineRender();
        Raycast();
    }

    /// <summary>
    /// 트리거 입력을 감지하여 사용할 손을 감지하고 인터렉션의 실행 여부를 판단함
    /// </summary>
    public void Interaction()
    {
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
        {
            if (isUseRight == false && subscribed_UI != null)
            {

            }

            isUseRight = false;
            currnetCoolTime = 0;
        }
        else if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
        {
            if (isUseRight == true && subscribed_UI != null)
            {

            }

            isUseRight = true;
            currnetCoolTime = 0;
        }

    }

    /// <summary>
    /// 상호작용이 가능한 게임오브젝트가 감지되면 LineRender를 그려줄 함수
    /// </summary>
    public void LineRender()
    {
        if (isHit)
        {
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, usedHand.transform.position);

            Vector3 endPosition = usedHand.transform.position + usedHand.transform.forward * 30.0f;
            lineRenderer.SetPosition(1, endPosition);
        }
        else
        {
            lineRenderer.enabled = false;
        }
    }

    /// <summary>
    /// 레이캐스트를 실행시켜 감지 로직을 처리함
    /// </summary>
    public void Raycast()
    {
        RaycastHit hit;

        if (Physics.Raycast(usedHand.transform.position, usedHand.transform.forward, out hit, 30.0f, 1 << 7))
        {
            GameUI_Interaction gameUI_Interaction;
            gameUI_Interaction = hit.collider.GetComponent<GameUI_Interaction>();

            if (subscribed_UI != gameUI_Interaction)
            {
                if (subscribed_UI != null)
                {
                    subscribed_UI.inputActivation = false;
                }

                subscribed_UI = gameUI_Interaction;

                if (subscribed_UI != null)
                {
                    subscribed_UI.inputActivation = true;
                    subscribed_UI.is_CanvasPopup = true;
                    subscribed_UI.currentTime = 0;
                }
            }
        }
        else
        {
            if (subscribed_UI != null) subscribed_UI.inputActivation = false;
            subscribed_UI = null;
        }

        isHit = (subscribed_UI != null);
    }

}
