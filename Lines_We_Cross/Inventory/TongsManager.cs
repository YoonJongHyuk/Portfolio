using Oculus.Interaction;
using UnityEngine;

/// <summary>
/// 막집게(Tongs) 이벤트 관리 Manager
/// - 막집게로 태그가 Grabbable 인 오브젝트를 집고/놓는 로직
/// - 손(GrabInteractor)의 입력 트리거/그립을 사용해 동작
/// - 그랩한 오브젝트를 grabAnchor 하위로 붙이면서 월드 스케일을 보존
/// - 다른 손이 직접 물체를 집었을 때(손-그랩) 막집게가 놓도록 처리
/// </summary>
public class TongsManager : MonoBehaviour
{
    // 막집게 본체(옵션: 디버그용/참조용)
    public GameObject _tongs;

    // 막집게에 물체를 붙일 기준 위치(집게 끝 등)
    public Transform grabAnchor;

    // 막집게로 집을 수 있는 대상의 태그(프로젝트 규칙: "Grabbable")
    public string grabbableTag = "Grabbable";

    // 현재 트리거를 누른 손 구분(Left/Right). 문자열 대신 enum을 쓰면 더 안전하지만, 기존 로직 유지.
    private string grabPos;

    // 현재 막집게가 들고 있는(부모가 grabAnchor인) 오브젝트
    private GameObject heldObject;

    // 집기 전 원래 월드 스케일(부모를 바꿔도 시각적 크기 유지하기 위해 저장)
    private Vector3 originalWorldScale;

    // 왼손/오른손 GrabInteractor (플레이어 손의 그랩 인터랙터)
    public GrabInteractor leftInteractor;
    public GrabInteractor rightInteractor;

    // "막집게를 잡고 있는" 손의 그립 버튼(집게 자체를 쥐고 있을 때 쓰는 입력)
    public OVRInput.Button leftHandUseButton = OVRInput.Button.PrimaryHandTrigger;
    public OVRInput.Button rightHandUseButton = OVRInput.Button.SecondaryHandTrigger;


    private void Start()
    {
        // 씬에 GameManager가 없으면 조기 리턴(NullReference 방지)
        if (GameManager.instance == null) return;

        // 게임 매니저에서 좌/우 손 인터랙터 참조 가져오기
        leftInteractor = GameManager.instance.leftInteractor;
        rightInteractor = GameManager.instance.rightInteractor;
    }

    

    private void Update()
    {
        // 집게를 쥐고 있는 손(Left/Right)에 따라, 그 손의 그립 버튼을 눌렀을 때
        // grabAnchor에 붙어 있는(집게가 들고 있는) 오브젝트를 비워주는 처리
        if (grabPos == "Right") EmptyObj(leftInteractor, leftHandUseButton);   // 주의: 변수명과 논리 확인 필요
        if (grabPos == "Left") EmptyObj(rightInteractor, rightHandUseButton); // (기존 코드 유지)

        // 트리거(검지) 버튼을 놓을 때: 들고 있던 오브젝트를 놓기
        CheckReleaseByTrigger();

        // Hand Trigger(그립)을 놓을 때: 들고 있던 오브젝트(있다면) 놓기
        // + 필요시 막집게 자체도 손에서 놓도록 확장 가능(주석된 코드 참고)
        CheckReleaseByHandGrip();
    }

    private void LateUpdate()
    {
        // 다른 손(플레이어의 GrabInteractor)이 heldObject를 직접 Grab한 경우
        // => 막집게는 해당 오브젝트를 놓는다(부모를 해제)
        if (heldObject != null && IsHeldByHand(heldObject))
        {
            EmptingObjEvent(heldObject);
        }
    }

    /// <summary>
    /// 막집게의 트리거 콜라이더가 Grabbable 오브젝트와 접촉했을 때 호출(TriggerStay/Enter 등 외부에서 호출 가정)
    /// </summary>
    public void TryGrab(Collider other)
    {
        // 1) 이미 다른 손이 이 물체를 잡고 있다면(손-그랩 우선) -> 막집게는 비운다
        if (heldObject != null && IsHeldByHand(heldObject))
        {
            EmptingObjEvent(heldObject);
            return;
        }

        // 2) 막집게가 이미 뭔가를 들고 있으면 새로 집지 않음
        if (heldObject != null) return;

        // 3) 이번 프레임에 트리거 버튼을 누른 경우에만 집기 시도
        if (!IsTriggerPressedDown()) return;

        // 4) 어느 손의 트리거가 눌렸는지 판정(검지 트리거)
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
            grabPos = "Left";
        else if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
            grabPos = "Right";

        // 5) 대상이 Grabbable 태그인지 확인
        if (other.CompareTag(grabbableTag))
        {
            GameObject candidate = other.gameObject;

            // 이미 플레이어 손(GrabInteractor)에 잡혀있는 상태인지 검사
            var grabbable = candidate.GetComponent<Grabbable>();
            if (grabbable != null && grabbable.GrabPoints.Count > 0) return;

            // 여기서부터 실제 집기 수행
            heldObject = candidate;

            Rigidbody rigidbody = candidate.GetComponent<Rigidbody>();

            rigidbody.isKinematic = true;

            // 월드 스케일 보존을 위해 현재 스케일 저장
            originalWorldScale = heldObject.transform.lossyScale;

            // 부모를 grabAnchor로 변경(월드 좌표 유지 모드 사용)
            heldObject.transform.SetParent(grabAnchor, true);

            // 위치/회전을 앵커에 정확히 맞춤(집게 끝에 "착" 붙이는 효과)
            heldObject.transform.position = grabAnchor.position;
            heldObject.transform.rotation = grabAnchor.rotation;

            // 부모가 바뀌면 로컬 스케일이 달라질 수 있으므로,
            // 부모(grabAnchor)의 월드 스케일을 나누어 최종 월드 스케일을 원래대로 유지
            Vector3 parentScale = grabAnchor.lossyScale;
            heldObject.transform.localScale = new Vector3(
                originalWorldScale.x / parentScale.x,
                originalWorldScale.y / parentScale.y,
                originalWorldScale.z / parentScale.z
            );
        }
    }

    /// <summary>
    /// 검지 트리거 버튼을 놓았을 때(Up) 들고 있던 오브젝트를 놓는다.
    /// </summary>
    private void CheckReleaseByTrigger()
    {
        if (heldObject == null) return;

        // 왼손 트리거를 뗀 경우
        if (grabPos == "Left" && OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger))
        {
            EmptingObjEvent(heldObject);
        }
        // 오른손 트리거를 뗀 경우
        else if (grabPos == "Right" && OVRInput.GetUp(OVRInput.Button.SecondaryIndexTrigger))
        {
            EmptingObjEvent(heldObject);
        }
    }

    /// <summary>
    /// Hand Trigger(그립)을 놓았을 때 동작.
    /// - 막집게를 쥐던 손의 그립을 놓으면, 들고 있던 오브젝트가 있으면 놓는다.
    /// - (주석) 필요하면 GrabInteractor.ForceRelease()로 막집게 자체를 손에서 떼는 처리도 가능.
    /// </summary>
    private void CheckReleaseByHandGrip()
    {
        if (grabPos == "Left" && OVRInput.GetUp(leftHandUseButton))
        {
            if (heldObject != null) EmptingObjEvent(heldObject);
            //leftInteractor.ForceRelease(); // 왼손이 tongs 자체를 놓고 싶다면 사용
        }
        else if (grabPos == "Right" && OVRInput.GetUp(rightHandUseButton))
        {
            if (heldObject != null) EmptingObjEvent(heldObject);
            //rightInteractor.ForceRelease(); // 오른손이 tongs 자체를 놓고 싶다면 사용
        }
    }

    /// <summary>
    /// 실제 "놓기" 동작
    /// - 부모 해제
    /// - 원래 월드 스케일 복원
    /// - heldObject 캐시 초기화
    /// </summary>
    public void EmptingObjEvent(GameObject obj)
    {
        obj.transform.SetParent(null);
        obj.transform.localScale = originalWorldScale;

        Rigidbody rigidbody = obj.GetComponent<Rigidbody>();

        rigidbody.isKinematic = false;

        if (heldObject == obj)
        {
            heldObject = null;
        }
    }

    /// <summary>
    /// 집게를 쥐고 있는 손의 그립 버튼을 "누르는 순간" grabAnchor 자식인 오브젝트가 있으면 비우는 처리
    /// - 손이 집게를 쥔 상태에서 버튼을 눌러 특정 상황(예: UI 상호작용)에서 물체를 내려놓게 할 때 사용
    /// </summary>
    private void EmptyObj(GrabInteractor interactor, OVRInput.Button inputButton)
    {
        // 그립 버튼 Down 시점에만 반응
        if (OVRInput.GetDown(inputButton))
        {
            // 현재 손이 선택(그랩) 중인 인터랙터블
            var interactable = interactor.SelectedInteractable;
            if (interactable == null) return;

            // 그 인터랙터블이 Grabbable인지 확인
            var grabbable = interactable.PointableElement as Grabbable;
            if (grabbable == null) return;

            var heldObj = grabbable.gameObject;

            // 집게의 기준 트랜스폼(GrabAnchor) 하위로 붙어 있는 물체만 대상으로 함
            if (heldObj.transform.parent != null && heldObj.transform.parent.CompareTag("GrabAnchor"))
            {
                EmptingObjEvent(heldObj);
            }
        }
    }

    /// <summary>
    /// 이번 프레임에 왼/오른 트리거 버튼이 눌렸는지
    /// </summary>
    private bool IsTriggerPressedDown()
    {
        return OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger) ||
               OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger);
    }

    /// <summary>
    /// 대상 아이템이 이미 손(GrabInteractor)에 의해 잡혀 있는지 판단
    /// - Grabbable.GrabPoints.Count > 0 이면 손이 잡고 있는 상태로 간주
    /// </summary>
    private bool IsHeldByHand(GameObject item)
    {
        var grabbable = item.GetComponent<Grabbable>();
        return grabbable != null && grabbable.GrabPoints.Count > 0;
    }

    // 사용되지 않지만 참고용(필요 시 재사용)
    private bool IsTriggerReleased()
    {
        return OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger) ||
               OVRInput.GetUp(OVRInput.Button.SecondaryIndexTrigger);
    }
}
