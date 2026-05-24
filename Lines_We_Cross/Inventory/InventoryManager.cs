using UnityEngine;
using System.Collections.Generic;


/// <summary>
/// 인벤토리 내 아이템 관리, 무게 관리 Manager
/// </summary>
public class InventoryManager : MonoBehaviour
{

    public static InventoryManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }


    public List<Item> items;


    public float inventoryWeight;
    public float max_inventoryWeight;
    private GameObject backObj;

    private void Start()
    {
        if (backObj == null)
        {
            backObj = GameManager.instance.new_WheelchairManager.wheelchairback;
        }

        WeightEvent();
    }

    private float ChangeWeight(Item item)
    {
        float weight = item.itemData.weight;
        return weight;
    }

    public void AddItem(Item item)
    {
        items.Add(item);
        inventoryWeight += ChangeWeight(item);
        WeightEvent();
    }

    public void RemoveItem(Item item)
    {
        if (item == null || item.itemData == null)
        {
            Debug.LogWarning("RemoveItem failed: item or itemData is null");
            return;
        }

        item.gameObject.transform.SetParent(null);

        items.Remove(item);
        inventoryWeight -= ChangeWeight(item);
        WeightEvent();
    }

    public void WeightEvent()
    {
        if (backObj == null) return;
        if (inventoryWeight > max_inventoryWeight)
        {
            if (backObj.activeSelf) return;
            backObj.SetActive(true);
        }
        else
        {
            backObj.SetActive(false);
        }
    }

}
