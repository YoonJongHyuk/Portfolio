using Unity.VisualScripting;
using UnityEngine;

public class WheelchairSpeedCheck : MonoBehaviour
{
    // ControllerSpeedCheck 컴포넌트 참조
    private ControllerSpeedCheck currentControllerSpeedCheck;

    // WheelchairSpeedCheck 스크립트가 붙은 바퀴 모델
    public GameObject wheel_model; // 바퀴 시각 모델 (Inspector에서 할당)

    [Header("Current Status")]
    public float speed = 0; // 이 바퀴가 만들어내는 현재 속도
    public bool isRolling = false; // 바퀴 모델이 돌고 있을 경우 true

    private bool isWheelTriggerEntered = false; // 바퀴 collider에 컨트롤러가 접촉했는지 여부
    private bool isHandTriggerPressed = false; // 핸드 그랩 버튼이 현재 눌려있는지 여부 (Update에서 설정)

    bool isHandPressed;

    bool isIndexPressed;

    private void Start()
    {
        isWheelTriggerEntered = true;
        MeshSize();
    }

    // Update는 사용자 입력 처리 및 프레임별 시각적 업데이트에 사용
    void Update()
    {
        // 1. 컨트롤러 접촉 여부 확인
        if (!isWheelTriggerEntered || currentControllerSpeedCheck == null)
        {
            isHandTriggerPressed = false; // 컨트롤러가 없으면 버튼도 안 눌림
            return;
        }

        OVRInput.Button handTriggerButton = currentControllerSpeedCheck.type == ControllerSpeedCheck.ControllerType.Left
        ? OVRInput.Button.PrimaryHandTrigger
        : OVRInput.Button.SecondaryHandTrigger;


        isHandPressed = OVRInput.Get(handTriggerButton);


        // 🟡 트리거를 처음 눌렀을 때만 초기화
        if (isHandPressed && !isHandTriggerPressed)
        {
            currentControllerSpeedCheck.ResetLastLocalPosition();
            //Debug.Log("🎯 Reset lastLocalPos on first grab");
        }


        OVRInput.Button indexTriggerButton = currentControllerSpeedCheck.type == ControllerSpeedCheck.ControllerType.Left
        ? OVRInput.Button.PrimaryIndexTrigger
        : OVRInput.Button.SecondaryIndexTrigger;



        isIndexPressed = OVRInput.Get(indexTriggerButton);





        isHandTriggerPressed = isHandPressed;

    }

    // FixedUpdate는 물리 계산 및 Rigidbody 관련 로직에 사용
    void FixedUpdate()
    {
        RatationModel();

        // 인덱스 트리거 버튼 클릭 시 speed 는 0으로 브레이크 기능
        if (isIndexPressed)
        {
            speed = 0;
            return;
        }
        // Update에서 감지된 그랩 버튼 상태와 접촉 여부에 따라 속도 계산
        if (!isWheelTriggerEntered || !isHandTriggerPressed || currentControllerSpeedCheck == null)
        {
            speed = 0; // 접촉 없거나 버튼 안 눌렸으면 속도 0
        }
        else
        {
            if (currentControllerSpeedCheck.gameObject.activeSelf)
            {
                currentControllerSpeedCheck.CandidateReturn();
                // 그랩 버튼이 눌려있고 접촉 중일 때만 GetControllerSignedSpeed 호출
                speed = currentControllerSpeedCheck.GetControllerSignedSpeed();
            }
        }


        // 속도에 따라 바퀴 모델 회전 업데이트 (시각적 + isRolling 상태 업데이트)
        RollingCheck();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Controller")) // 태그는 "controller"가 아니라 "Controller"가 일반적입니다. 확인 필요.
        {
            currentControllerSpeedCheck = other.transform.GetComponent<ControllerSpeedCheck>();
            if (currentControllerSpeedCheck != null)
            {
                isWheelTriggerEntered = true;
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Controller") && !isWheelTriggerEntered)
        {
            isWheelTriggerEntered = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 컨트롤러가 제대로 할당되었고, 해당 컨트롤러가 벗어났을 때만 초기화
        if (other.CompareTag("Controller") && currentControllerSpeedCheck != null && other.transform.GetComponent<ControllerSpeedCheck>() == currentControllerSpeedCheck)
        {
            currentControllerSpeedCheck = null;
            isWheelTriggerEntered = false;
            speed = 0; // 컨트롤러가 떨어지면 속도 0
            isRolling = false; // 컨트롤러가 떨어지면 회전 멈춤
            isHandTriggerPressed = false; // 컨트롤러가 떨어지면 버튼도 안 눌림
        }
    }


    float wheelRadius = 0;

    void MeshSize()
    {
        // 월드 단위로 바운딩 박스를 가져옴
        Bounds bounds = wheel_model.GetComponent<Renderer>().bounds;

        // Y축 기준 높이의 절반 → 반지름 추정
        wheelRadius = bounds.size.y * 0.5f;

        //Debug.Log($"계산된 바퀴 반지름: {wheelRadius:F3} m");

    }

    void RatationModel()
    {
        if (wheel_model == null || GameManager.instance == null) return;

        Rigidbody wheelchairRb = GameManager.instance.new_WheelchairManager.wheelchairRb;
        if (wheelchairRb == null || wheelRadius <= 0.001f) return;

        // === 1) 직선 이동 기반 회전량 ===
        Vector3 localVel = transform.InverseTransformDirection(wheelchairRb.linearVelocity);
        float linearRotation = (localVel.z / wheelRadius) * Mathf.Rad2Deg * Time.fixedDeltaTime;

        // === 2) 제자리 회전(y축 회전) 기반 회전량 ===
        float angularY = wheelchairRb.angularVelocity.y; // y축 회전 속도 (rad/s)

        // 바퀴와 휠체어 중심 거리 (반지름 형태로 설정)
        float halfAxleLength = 0.3f; // ← 휠체어 중심부터 바퀴까지 거리(m)로 측정 필요
        float angularRotation = (angularY * halfAxleLength / wheelRadius) * Mathf.Rad2Deg * Time.fixedDeltaTime;

        // === 3) 왼/오 바퀴 구분하여 회전 방향 설정 ===
        bool isLeftWheel = gameObject.name.ToLower().Contains("left");

        float totalRotation = isLeftWheel
            ? linearRotation - angularRotation
            : linearRotation + angularRotation;

        // 회전
        wheel_model.transform.Rotate(Vector3.right, totalRotation, Space.Self);
    }


    void RollingCheck()
    {
        if (wheel_model == null)
        {
            //Debug.LogWarning($"[{gameObject.name}] Wheel model이 할당되지 않았습니다!");
            isRolling = false;
            return;
        }

        // Dead zone
        const float minEffectiveSpeed = 0.05f;
        if (Mathf.Abs(speed) < minEffectiveSpeed || wheelRadius <= 0.001f)
        {
            isRolling = false;
            return;
        }

        isRolling = true;

        // 바퀴 회전량 계산 (speed는 signed forward 이동)
        float rotationAmount = (speed / wheelRadius) * Mathf.Rad2Deg * Time.fixedDeltaTime;

        // 바퀴는 일반적으로 X축을 기준으로 회전 (상황에 따라 수정)
        wheel_model.transform.Rotate(Vector3.right, rotationAmount, Space.Self);
    }

}