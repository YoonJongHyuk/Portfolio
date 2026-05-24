using UnityEngine;

public class Wheelchair_Move_Test : MonoBehaviour
{
    public Rigidbody rb;
    public float speed;
    public float flspeed = 0.000001234567f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.W))
        {
            rb.AddForce(transform.forward * (flspeed * speed * 10000000), ForceMode.Force);
        }
        else if (Input.GetKey(KeyCode.S))
        {
            rb.AddForce(transform.forward * -(flspeed * speed * 10000000), ForceMode.Force);
        }
        else if (Input.GetKey(KeyCode.A))
        {
            transform.Rotate(transform.up, -(speed * 10000000 * Time.deltaTime * flspeed));
        }
        else if (Input.GetKey(KeyCode.D))
        {
            transform.Rotate(transform.up, (speed * 10000000 * Time.deltaTime * flspeed));
        }
    }
}
