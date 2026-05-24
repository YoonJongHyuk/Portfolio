using Oculus.Interaction;
using UnityEngine;

public class ItemEventManager : MonoBehaviour
{
    public GrabInteractor leftInteractor;
    public GrabInteractor rightInteractor;

    // 원하는 입력 키 (왼손 오른손에 따라 다르게 설정 가능)
    public OVRInput.Button leftHandUseButton = OVRInput.Button.PrimaryIndexTrigger;
    public OVRInput.Button rightHandUseButton = OVRInput.Button.SecondaryIndexTrigger;

    void Update()
    {
        if(leftInteractor == null ||  rightInteractor == null) return;
        // 왼손으로 잡은 아이템 사용
        TryUseItem(leftInteractor, leftHandUseButton);

        // 오른손으로 잡은 아이템 사용
        TryUseItem(rightInteractor, rightHandUseButton);
    }

    private void TryUseItem(GrabInteractor interactor, OVRInput.Button inputButton)
    {
        if (OVRInput.GetDown(inputButton))
        {
            var interactable = interactor.SelectedInteractable;
            if (interactable == null) return;

            var grabbable = interactable.PointableElement as Grabbable;
            if (grabbable == null) return;

            var heldObj = grabbable.gameObject;

            var item = heldObj.GetComponent<Item>();

            if (item != null)
            {
                Debug.Log($"[ItemEventManager] {item.name} → Use()");
                item.Use();
            }
        }
    }
}
