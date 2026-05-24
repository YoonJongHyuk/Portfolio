using UnityEngine;



public class PlayerStatus : MonoBehaviour
{
    public float Tiredness = 100;
    public float Hunger = 100;
    public float Stamina = 100;
    public float Illness = 100;
    public float Physical = 100;

    private void Update()
    {
        // 시간이 지나면 상태가 감소합니다.
        Tiredness -= Time.deltaTime * 0.1f;
        Hunger -= Time.deltaTime * 0.2f;
        Illness -= Time.deltaTime * 0.05f;
        Physical -= Time.deltaTime * 0.05f;

        // 스테미나가 천천히 상승합니다.
        Stamina += Time.deltaTime * 0.1f;

        // 상태가 0 이하로 떨어지지 않도록 합니다.
        Tiredness = Mathf.Max(Tiredness, 0);
        Hunger = Mathf.Max(Hunger, 0);
        Stamina = Mathf.Max(Stamina, 0);
        Illness = Mathf.Max(Illness, 0);
        Physical = Mathf.Max(Physical, 0);

        // 상태가 100을 초과하지 않도록 합니다.
        Tiredness = Mathf.Min(Tiredness, 100);
        Hunger = Mathf.Min(Hunger, 100);
        Stamina = Mathf.Min(Stamina, 100);
        Illness = Mathf.Min(Illness, 100);
        Physical = Mathf.Min(Physical, 100);

        // 상태가 0 이하로 떨어지면 플레이어에게 영향을 줄 수 있습니다.
        if (Tiredness <= 0 || Hunger <= 0 || Stamina <= 0 || Illness <= 0 || Physical <= 0)
        {
            Debug.LogWarning("[PlayerStatus] 플레이어 상태가 위험합니다! 상태를 회복하세요.");
        }

    }

    public void SetStatus(float tiredness, float hunger, float stamina, float illness, float physical)
    {
        Tiredness = tiredness;
        Hunger = hunger;
        Stamina = stamina;
        Illness = illness;
        Physical = physical;
    }

    public void ChangeStatus(ItemType type, float amount)
    {
        int intAmount = Mathf.RoundToInt(amount);

        switch (type)
        {
            case ItemType.Tiredness:
                Tiredness += intAmount;
                Debug.Log($"[PlayerStatus] 피곤 {intAmount:+#;-#;0} → 현재: {Tiredness}");
                break;

            case ItemType.Hunger:
                Hunger += intAmount;
                Debug.Log($"[PlayerStatus] 허기 {intAmount:+#;-#;0} → 현재: {Hunger}");
                break;

            case ItemType.Stamina:
                Stamina += intAmount;
                Debug.Log($"[PlayerStatus] 스테미나 {intAmount:+#;-#;0} → 현재: {Stamina}");
                break;

            case ItemType.Physical:
                Physical += intAmount;
                break;

            case ItemType.Illness:
                Illness += intAmount;
                break;

            default:
                Debug.LogWarning($"[PlayerStatus] 알 수 없는 상태 타입: {type}");
                break;
        }
    }

}
