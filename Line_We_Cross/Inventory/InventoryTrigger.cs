using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 인벤토리 내 일부 구역 collider에서 Trigger 이벤트 전용 클래스
/// </summary>
public class InventoryTrigger : MonoBehaviour
{
    public Transform in_Inventory;

    private void OnTriggerEnter(Collider other)
    {
        if (!in_Inventory.gameObject.activeSelf) return;
        Item item = other.gameObject.GetComponent<Item>();
        if (item == null) return;

        // 막집게에 쥐어진 상태라면 enter 처리하지 않음
        if (IsHeldByTongs(other.gameObject))
        {
            return;
        }

        InventoryManager.Instance.AddItem(item);

        GameObject heldObject = other.gameObject;

        var rb = heldObject.GetComponent<Rigidbody>();
        rb.isKinematic = true;
        Debug.Log($"OnTriggerEnter isKinematic: {rb.isKinematic}");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!in_Inventory.gameObject.activeSelf) return;

        var item = other.GetComponent<Item>();
        if (item == null) return;
        if (IsHeldByTongs(other.gameObject)) return;

        int id = other.attachedRigidbody ? other.attachedRigidbody.GetInstanceID()
                                         : other.transform.root.GetInstanceID();
        if (_exited.Contains(id)) return;        // ✅중복 방지
        _exited.Add(id);

        if (other.CompareTag("Grabbable"))
        {
            InventoryManager.Instance.RemoveItem(item);

            LeaveItemTransform(other);
        }

        StartCoroutine(ClearExitMark(id));
    }

    private IEnumerator ClearExitMark(int id)
    {
        yield return new WaitForSeconds(0.1f);   // 짧은 디바운스 창
        _exited.Remove(id);
    }

    bool IsTrulyOutside(Collider zone, Collider target)
    {
        // 빠른 방법: Bounds 체크(완벽하진 않지만 싸고 충분)
        return !zone.bounds.Intersects(target.bounds);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!in_Inventory.gameObject.activeSelf) return;
        var item = other.gameObject.GetComponent<Item>();
        if (item == null) return;

        // 이미 막집게에 잡혀 있는 경우라면 무시
        if (IsHeldByTongs(other.gameObject)) return;

        if (!IsHandReleased())
        {
            CarryItemTransform(other);
        }
    }

    // 막집게에 쥐어져 있는지 확인
    private bool IsHeldByTongs(GameObject obj)
    {
        Transform p = obj.transform.parent;
        while (p != null)
        {
            if (p.CompareTag("Tongs")) return true; // ✅기준 단일화
            p = p.parent;
        }
        return false;
    }

    private readonly HashSet<int> _exited = new HashSet<int>();

    void LeaveItemTransform(Collider other)
    {
        if (IsHeldByTongs(other.gameObject)) return;

        var tr = other.transform;
        if (tr.parent == null) return;         // ✅이미 분리되어 있으면 아무것도 안함

        tr.SetParent(null, true);              // ✅월드 좌표 유지
        var rb = other.attachedRigidbody ?? other.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;
    }

    void CarryItemTransform(Collider other)
    {
        var tr = other.transform;
        if (tr.parent == in_Inventory) return; // ✅중복 SetParent 방지

        Vector3 originalWorldScale = tr.lossyScale;
        tr.SetParent(in_Inventory, true);
        Vector3 p = in_Inventory.lossyScale;
        tr.localScale = new Vector3(originalWorldScale.x / p.x, originalWorldScale.y / p.y, originalWorldScale.z / p.z);
    }

    // 트리거 버튼을 풀어서 아이템 떨구는 함수
    private bool IsHandReleased()
    {
        return OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger) ||
               OVRInput.GetUp(OVRInput.Button.SecondaryHandTrigger);
    }


}
