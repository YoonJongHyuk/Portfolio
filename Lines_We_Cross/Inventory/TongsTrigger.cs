using UnityEngine;

public class TongsTrigger : MonoBehaviour
{
    public TongsManager tongsManager;

    private void OnTriggerStay(Collider other)
    {
        tongsManager.TryGrab(other);
    }
}
