using Oculus.Interaction;
using System.Collections;
using UnityEngine;

public class StandEvent : MonoBehaviour
{

    public Vector3 standPosition = Vector3.zero;
    public Vector3 standRotation = Vector3.zero;
    private bool isStand = false;

    public string tagName = null;

    private GameObject player;

    private GrabInteractor leftInteractor;
    private GrabInteractor rightInteractor;


    private GameObject visualObject;

    private GameObject standedItem;

    private void Start()
    {
        leftInteractor = GameManager.instance.leftInteractor;
        rightInteractor = GameManager.instance.rightInteractor;

        visualObject = transform.GetChild(0).gameObject;

        player = GameObject.FindWithTag("Player");
    }

    private void FixedUpdate()
    {
        if (standedItem == null) return;

        float distance = Vector3.Distance(standedItem.transform.position, player.transform.position);

        if (distance >= 4f)
        {
            StandObject(true, standedItem);
        }
    }


    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(tagName)) return;

        StandItem item = other.GetComponent<StandItem>();
        if (item == null || item.grabbable == null) return;

        Grabbable grabbable = item.grabbable;

        // 오른손 입력
        if (OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger))
        {
            if (rightInteractor.Interactable != null &&
                (object)rightInteractor.Interactable.PointableElement == grabbable)
            {
                StandObject(false, item);
            }
        }

        // 왼손 입력
        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger))
        {
            if (leftInteractor.Interactable != null &&
                (object)leftInteractor.Interactable.PointableElement == grabbable)
            {
                StandObject(false, item);
            }
        }

        // 세우기
        if (!isStand)
        {
            if (OVRInput.GetUp(OVRInput.Button.SecondaryHandTrigger))
            {
                if (rightInteractor.Interactable != null &&
                    (object)rightInteractor.Interactable.PointableElement == grabbable)
                {
                    StandObject(true, item);
                }
            }

            if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger))
            {
                if (leftInteractor.Interactable != null &&
                    (object)leftInteractor.Interactable.PointableElement == grabbable)
                {
                    StandObject(true, item);
                }
            }
        }
    }

    private IEnumerator StandAfterDelay(GameObject obj)
    {
        obj.transform.SetParent(transform, true); // SetParent 후 현재 위치 유지하지 않음

        yield return new WaitForSeconds(0.1f); // 0.1초 대기

        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        obj.transform.localPosition += standPosition;
        obj.transform.localRotation *= Quaternion.Euler(standRotation);
    }


    void StandObject(bool isTrue, GameObject value)
    {

        StandItem item = value.GetComponent<StandItem>();


        GameObject obj = item.mine;

        standedItem = item.mine;

        Debug.Log("standItem 발생!");






        if (isTrue)
        {
            StartCoroutine(StandAfterDelay(obj));
            Debug.Log("standItem True");
            isStand = true;
            visualObject.SetActive(false);
        }
        else
        {
            obj.transform.SetParent(null, true);
            Debug.Log("standItem false");
            isStand = false;
            visualObject.SetActive(true);
        }
    }

    void StandObject(bool isTrue, StandItem item)
    {

        if (item == null || item.mine == null)
        {
            Debug.LogWarning("StandItem 또는 mine이 null입니다.");
            return;
        }

        GameObject obj = item.mine;

        standedItem = item.mine;

        Debug.Log("standItem 발생!");






        if (isTrue)
        {
            StartCoroutine(StandAfterDelay(obj));
            Debug.Log("standItem True");
            isStand = true;
            visualObject.SetActive(false);
        }
        else
        {
            obj.transform.SetParent(null, true);
            Debug.Log("standItem false");
            isStand = false;
            visualObject.SetActive(true);
        }
    }


}
