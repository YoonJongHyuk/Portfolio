using System.Collections.Generic;
using UnityEngine.UIElements;

public static class ItemEffectEventTable
{
    private static readonly Dictionary<ItemType, ItemEffectEvent> eventMap = new()
    {
        { ItemType.Tiredness, new TirednessEvent() },
        { ItemType.Stamina, new StaminaEvent() },
        { ItemType.Hunger, new HungerEvent() },

        // 추가하고 싶은 타입은 여기 계속 추가
    };

    public static bool TryGetEvent(ItemType type, out ItemEffectEvent effectEvent)
    {
        return eventMap.TryGetValue(type, out effectEvent);
    }
}
