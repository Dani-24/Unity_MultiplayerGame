using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Rigidbody rb;

    public float speed = 10f;

    public float travelDistance = 5f;

    public float DMG = 35f;

    public float customGravity = -9.81f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Start()
    {        
        rb.velocity = transform.forward * speed;

        float acc = (speed * speed) / (2 * travelDistance);

        customGravity = acc / -9.81f;
    }

    private void FixedUpdate()
    {
        rb.velocity += new Vector3(0, customGravity, 0);

        if(transform.position.y < -20)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag != "Player" && other.tag != "Bullet")
        {
            Destroy(gameObject);
        }
    }
}