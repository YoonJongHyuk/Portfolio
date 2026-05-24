using Unity.VisualScripting;
using UnityEngine;


/// <summary>
/// 플레이어 스크립트 관리 Manager
/// </summary>
public class PlayerManager : MonoBehaviour
{

    public PlayerStatus playerStatus;

    public static PlayerManager Instance;

    private void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
        }
        else if(Instance != this)
        {
            Destroy(gameObject);
        }
    }


    private void Start()
    {
        playerStatus = transform.AddComponent<PlayerStatus>();

        playerStatus.SetStatus(100, 100, 100, 100, 100);
    }

}
