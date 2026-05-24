using Oculus.Interaction;
using UnityEngine;

public class ControllerSpeedCheck : MonoBehaviour
{
    public enum ControllerType
    {
        Left,
        Right
    }

    public ControllerType type;

    public GameObject player_model; // 이 모델의 forward 방향이 기준이 됩니다.

    private Rigidbody rb; // 이 스크립트가 붙은 컨트롤러의 Rigidbody
    private Transform player_transform; // player_model의 Transform 캐싱

    private Vector3 lastLocalPos;

    public GrabInteractor grabInteractor;


    void Start()
    {
        rb = GetComponent<Rigidbody>(); // 이 컨트롤러의 Rigidbody
        if (player_model != null)
        {
            player_transform = GameManager.instance.new_WheelchairManager.player_wheelchair_root.GetComponent<Transform>();
            //player_transform = player_model.GetComponent<Transform>(); // 플레이어 모델의 Transform 캐싱
        }
        else
        {
            Debug.Log("ControllerSpeedCheck: player_model이 할당되지 않았습니다!");
            enabled = false;
            return;
        }
        if (rb == null)
        {
            Debug.Log("ControllerSpeedCheck: Rigidbody가 이 컴포넌트에 없습니다!");
            enabled = false;
            return;
        }
        if (grabInteractor == null)
        {
            grabInteractor = transform.parent.GetComponent<GrabInteractor>();
        }

        lastLocalPos = player_transform.InverseTransformPoint(transform.position);


    }

    public void ResetLastLocalPosition()
    {
        if (player_transform != null)
        {
            lastLocalPos = player_transform.InverseTransformPoint(transform.position);
        }
    }

    public void CandidateReturn()
    {
        if (grabInteractor.Candidate != null) return;
    }


    public float GetControllerSignedSpeed()
    {
        // 현재 프레임에서의 컨트롤러 위치 (휠체어 기준)
        Vector3 currentLocalPos = player_transform.InverseTransformPoint(transform.position);

        // 로컬 기준 이동량
        Vector3 deltaLocal = (currentLocalPos - lastLocalPos) / Time.fixedDeltaTime;
        lastLocalPos = currentLocalPos;

        // 이동량이 거의 없으면 0 처리
        if (deltaLocal.sqrMagnitude < 0.0001f)
            return 0f;

        // 휠체어 기준으로 전후 방향은 z축 (local forward)
        float signedSpeed = deltaLocal.z;
        //Debug.Log($"local delta: {deltaLocal}, signedSpeed: {signedSpeed:F3}");
        Debug.DrawRay(transform.position, player_transform.forward * 0.3f, Color.blue);
        Debug.DrawRay(transform.position, player_transform.TransformDirection(deltaLocal.normalized) * 0.3f, Color.red);

        return signedSpeed;
    }



}