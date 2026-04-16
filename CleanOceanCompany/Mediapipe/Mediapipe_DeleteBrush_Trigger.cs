using UnityEngine;

public class Mediapipe_DeleteBrush_Trigger : MonoBehaviour
{
    public MediapipeThirdManager mediapipeThirdManager;

    private void Start()
    {
        if (mediapipeThirdManager == null) mediapipeThirdManager = GameObject.FindAnyObjectByType<MediapipeThirdManager>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("LeftBrush") && mediapipeThirdManager.createBrushTrue)
        {
            mediapipeThirdManager.createBrushTrue = false;
            Destroy(collision.gameObject);
            mediapipeThirdManager.BrushObject = null;
        }
    }
}
