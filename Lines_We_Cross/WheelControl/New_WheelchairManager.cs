using Oculus.Interaction;
using System.Collections;
using UnityEngine;

/// <summary>
/// 휠체어의 이동 및 회전을 관리하는 스크립트.
/// 왼손/오른손의 바퀴 회전 상태를 감지하여 이동 방향 및 회전 여부를 결정한다.
/// </summary>
public class New_WheelchairManager : MonoBehaviour
{
    public GameObject player_wheelchair_root; // 휠체어 전체의 루트 오브젝트 (Rigidbody가 붙어 있음)
    public GameObject wheelchairback; // 후방 시야용 오브젝트 (사용처 없음)

    public WheelchairSpeedCheck leftWheelScript;  // 왼쪽 바퀴 감지 스크립트
    public WheelchairSpeedCheck rightWheelScript; // 오른쪽 바퀴 감지 스크립트

    public TunnelingEffect tunnelingEffect; // 터널링 효과 적용을 위한 컴포넌트

    public Rigidbody wheelchairRb; // 휠체어 Rigidbody

    private bool wasDualRolling = false;            // 직전에 양손으로 바퀴를 돌렸는지 여부
    private float preventRotationUntilTime = 0f;    // 회전을 잠시 금지하는 타이머

    // ==== 이동 및 회전 관련 설정값 ====
    private const float MIN_MOVE_SPEED_THRESHOLD = 0.1f;     // 이동으로 인식할 최소 속도
    private const float MIN_ROTATION_SPEED_THRESHOLD = 0.1f; // 회전으로 인식할 최소 속도 차
    [SerializeField] private float MAX_LINEAR_SPEED = 10f;   // 최대 직선 속도
    [SerializeField] private float MAX_ANGULAR_SPEED = 2f;   // 최대 회전 속도

    private const float PREVENT_ROTATION_DURATION = 0.2f;   // 잘못된 회전 입력 후 회전 잠금 시간

    [SerializeField] private float moveForceMultiplier = 10f;         // 전진/후진 힘 배수
    [SerializeField] private float rotationTorqueMultiplier = 2.5f;   // 회전 토크 배수

    [SerializeField] float moveEpsilon = 0.0004f; // 이동 변화 감지 민감도 (거리 제곱)
    [SerializeField] float rotEpsilonDeg = 1.0f;  // 회전 변화 감지 민감도 (도 단위)

    private Vector3 prevPos;          // 이전 프레임 위치
    private Quaternion prevRot;       // 이전 프레임 회전
    private bool prevValid = false;   // 이전 값이 유효한지 여부

    private Transform rootTr;         // 휠체어 루트의 Transform 캐시

    private void Awake()
    {
        // 필수 오브젝트 확인
        if (player_wheelchair_root == null)
        {
            Debug.LogError("player_wheelchair_root not set");
            enabled = false;
            return;
        }
        rootTr = player_wheelchair_root.transform;
    }

    void Start()
    {
        if (player_wheelchair_root == null) return;

        wheelchairRb = player_wheelchair_root.GetComponent<Rigidbody>();
        if (wheelchairRb == null)
        {
            Debug.LogError("❌ 휠체어 Root 오브젝트에 Rigidbody가 없습니다!");
            enabled = false;
        }

        if (leftWheelScript == null || rightWheelScript == null)
        {
            Debug.LogError("❌ 왼쪽 또는 오른쪽 WheelchairSpeedCheck 스크립트가 할당되지 않았습니다.");
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        // 이전 프레임 기준 이동/회전 변화 감지하여 터널링 효과 실행
        if (!prevValid)
        {
            prevPos = rootTr.position;
            prevRot = rootTr.rotation;
            prevValid = true;
            return;
        }

        Vector3 pos = rootTr.position;
        Quaternion rot = rootTr.rotation;

        bool moved = (pos - prevPos).sqrMagnitude > moveEpsilon;

        float dot = Mathf.Abs(Quaternion.Dot(rot, prevRot));
        float cosHalf = Mathf.Cos(0.5f * rotEpsilonDeg * Mathf.Deg2Rad);
        bool rotated = dot < cosHalf;

        if (moved || rotated)
        {
            tunnelingEffect.KickTunneling();
            prevPos = pos;
            prevRot = rot;
        }
    }

    void FixedUpdate()
    {
        // 바퀴 회전 상태가 없으면 무시
        if (player_wheelchair_root == null) return;
        if (leftWheelScript == null) return;
        if (rightWheelScript == null) return;

        // 현재 각 바퀴의 속도 및 회전 여부 가져오기
        float leftSpeed = leftWheelScript?.speed ?? 0;
        float rightSpeed = rightWheelScript?.speed ?? 0;

        bool isLeftRolling = leftWheelScript?.isRolling ?? false;
        bool isRightRolling = rightWheelScript?.isRolling ?? false;

        // 임계값 이상인 경우에만 속도로 인정
        float effectiveLeftSpeed = Mathf.Abs(leftSpeed) > MIN_MOVE_SPEED_THRESHOLD ? leftSpeed : 0f;
        float effectiveRightSpeed = Mathf.Abs(rightSpeed) > MIN_MOVE_SPEED_THRESHOLD ? rightSpeed : 0f;

        // ✅ 양손 모두 회전 → 전진 or 후진
        if (isLeftRolling && isRightRolling)
        {
            wasDualRolling = true;

            float speedDiff = Mathf.Abs(effectiveLeftSpeed - effectiveRightSpeed);
            bool speedsAreClose = speedDiff < MIN_ROTATION_SPEED_THRESHOLD;
            bool sameDirection = Mathf.Sign(effectiveLeftSpeed) == Mathf.Sign(effectiveRightSpeed);

            if (sameDirection && effectiveLeftSpeed != 0 && effectiveRightSpeed != 0)
            {
                float dominantSpeed = Mathf.Abs(effectiveLeftSpeed) > Mathf.Abs(effectiveRightSpeed)
                                    ? effectiveLeftSpeed : effectiveRightSpeed;

                float avgSpeed = (effectiveLeftSpeed + effectiveRightSpeed) / 2f;
                float finalSpeed = speedsAreClose ? avgSpeed : dominantSpeed;

                Vector3 moveDir = player_wheelchair_root.transform.forward;
                Vector3 force = moveDir * finalSpeed * moveForceMultiplier;

                if (wheelchairRb.linearVelocity.magnitude <= MAX_LINEAR_SPEED)
                {
                    wheelchairRb.AddForce(force, ForceMode.Force);

                    // y축 회전 제거 → 직진 유지
                    Vector3 angVel = wheelchairRb.angularVelocity;
                    angVel.y = 0f;
                    wheelchairRb.angularVelocity = angVel;
                }

                // 최대 속도 제한
                if (wheelchairRb.linearVelocity.magnitude > MAX_LINEAR_SPEED)
                {
                    Vector3 clampedVelocity = wheelchairRb.linearVelocity.normalized * MAX_LINEAR_SPEED;
                    wheelchairRb.linearVelocity = clampedVelocity;
                    //Debug.Log($"⛔ 속도 제한 적용됨: {clampedVelocity.magnitude:F2}");
                }
            }
            else
            {
                // 서로 반대 방향 회전 시 → 이동 금지 + 회전 잠금
                //Debug.Log($"⚠️ 서로 방향 다름 → 이동 무시됨. L:{leftSpeed:F2}, R:{rightSpeed:F2}");
                Vector3 angVel = wheelchairRb.angularVelocity;
                angVel.y = 0f;
                wheelchairRb.angularVelocity = angVel;
                preventRotationUntilTime = Time.time + PREVENT_ROTATION_DURATION;
            }
        }

        // ✅ 한 손만 회전 → 회전 동작
        else if ((isLeftRolling && !isRightRolling) || (!isLeftRolling && isRightRolling))
        {
            // 최근 회전 금지 타이머 작동 중이면 무시
            if (Time.time < preventRotationUntilTime)
            {
                //Debug.Log("⏳ 최근 이동 무시됨 → 회전 잠시 보류");
                return;
            }

            // 바로 전까지 양손 이동 중이었다면 회전 금지
            if (wasDualRolling)
            {
                if (wheelchairRb.linearVelocity.magnitude >= 0.05f)
                {
                    //Debug.Log("🛑 회전 금지: 직전까지 양손 이동 중이었고 아직 속도 있음");
                    return;
                }
                else
                {
                    wasDualRolling = false;
                    //Debug.Log("✅ 회전 허용: 이동 종료 확인됨");
                }
            }

            // 회전 세기 계산
            float rotationPower = 0f;
            if (isLeftRolling)
                rotationPower = Mathf.Sign(effectiveLeftSpeed) * Mathf.Pow(Mathf.Abs(effectiveLeftSpeed), 1.3f);
            else
                rotationPower = -Mathf.Sign(effectiveRightSpeed) * Mathf.Pow(Mathf.Abs(effectiveRightSpeed), 1.3f);

            // y축 회전 토크 적용
            Vector3 torque = player_wheelchair_root.transform.up * rotationPower * rotationTorqueMultiplier;
            wheelchairRb.AddTorque(torque, ForceMode.Force);

            // 회전 속도 제한
            Vector3 angVel = wheelchairRb.angularVelocity;
            if (Mathf.Abs(angVel.y) > MAX_ANGULAR_SPEED)
            {
                angVel.y = Mathf.Sign(angVel.y) * MAX_ANGULAR_SPEED;
                wheelchairRb.angularVelocity = angVel;
                //Debug.Log($"⛔ 회전 속도 제한 적용됨: {angVel.y:F2}");
            }
        }

        // ✅ 아무 입력 없을 때 → 정지
        else
        {
            wheelchairRb.angularVelocity = Vector3.zero;
        }
    }
}
