using UnityEngine;

public class TurnModel : MonoBehaviour
{
    [Header("회전 속도 (도/초)")]
    public float rotationSpeed = 100f;

    void Update()
    {
        // 왼쪽 조이스틱 입력 받기 (PrimaryThumbstick = 왼손)
        Vector2 stickInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        float horizontal = stickInput.x; // 좌우 방향만 사용

        // 조이스틱 좌우 입력에 따라 Y축 회전
        if (Mathf.Abs(horizontal) > 0.1f) // 너무 민감하지 않게 DeadZone 설정
        {
            transform.Rotate(0f, horizontal * rotationSpeed * Time.deltaTime, 0f);
        }
    }
}
