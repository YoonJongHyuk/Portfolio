using UnityEngine;

public class OutlineTrigger : MonoBehaviour
{

    private Outline outline;

    [SerializeField] int count = 0;

    private void Start()
    {
        outline = GetComponent<Outline>();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Controller") || other.CompareTag("Tongs"))
        {
            count++;
            outline.OutlineWidth = 2f;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Controller") || other.CompareTag("Tongs"))
        {
            count--;
            if (count == 0)
            {
                outline.OutlineWidth = 0f;
            }
        }
    }

}
