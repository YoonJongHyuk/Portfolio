using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 효과를 적용하거나 특정 동작을 트리거하는 데 사용할 수 있는 게임 내 아이템을 나타냅니다.
/// </summary>
/// <remarks> <see cref="Item"/> 클래스는 아이템의 데이터와 효과를 포함하는 연관된 <see cref="ItemSO"/> 객체와 함께 작동하도록 설계되었습니다.
/// <see cref="Use"/> 메서드를 호출하기 전에 <see cref="itemData"/> 속성이 할당되었는지
/// 확인하십시오. 데이터가 할당되지 않으면 경고가 기록됩니다.</remarks>
public class Item : MonoBehaviour
{
    [InlineEditor(Expanded = true, DrawHeader = true, MaxHeight = 500)]
    [InfoBox("아이템 정보 변수를 넣어주세요.", InfoMessageType.Info)]
    public ItemSO itemData;

    private void Start()
    {
        if (itemData.isMutipleMesh)
        {
            int count = itemData.itemMeshes.Count;
            int value = Random.Range(0, count);

            MeshFilter meshFilter = GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            
            meshFilter.mesh = itemData.itemMeshes[value];
            meshRenderer.material = itemData.itemMaterials[value];
        }
    }

    public void Use()
    {
        if (itemData == null)
        {
            Debug.LogWarning("아이템 데이터가 없습니다.");
            return;
        }

        Debug.Log($"[Item] {itemData.itemName} 사용됨");

        foreach (var effect in itemData.GetEffects())
        {
            ApplyEffect(effect);
        }
    }

    private void ApplyEffect(ItemTypeData effect)
    {
        if (ItemEffectEventTable.TryGetEvent(effect.Type, out var effectEvent))
        {
            effectEvent.Apply(effect);
        }
        else
        {
            Debug.LogWarning($"[Item] 처리되지 않은 타입: {effect.Type}");
        }
    }
}
