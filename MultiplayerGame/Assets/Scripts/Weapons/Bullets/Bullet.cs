using UnityEngine;

public class Bullet : MonoBehaviour
{
    public string teamTag;

    private Rigidbody rb;
    private Renderer rend;

    #region Propierties

    [Header("Propierties")]
    public float speed = 10f;
    public float range = 5f;
    public float DMG = 35f;
    public float customGravity = -9.81f;

    public float meshScale = 1f;
    Vector3 meshScaleVec;

    #endregion

    #region Paint

    [Header("Painting")]
    public float radius = 1;
    public float strength = 1;
    public float hardness = 1;

    #endregion

    [Header("Other")]

    [SerializeField] float minYaxis = -20;
    [SerializeField] Transform meshTransform;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Start()
    {
        rb.velocity = transform.forward * speed;
        float acc = (speed * speed) / (2 * range);
        customGravity = acc / -9.81f;

        rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            rend.material.color = SceneManagerScript.Instance.GetTeamColor(teamTag);
        }

        meshScaleVec.Set(meshScale, meshScale, meshScale);

        meshTransform.localScale = meshScaleVec;

        gameObject.tag = teamTag + "Bullet";
    }

    private void FixedUpdate()
    {
        rb.velocity += new Vector3(0, customGravity, 0);

        if (transform.position.y < minYaxis)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);

        foreach(Collider collider in colliders)
        {
            Paintable p = collider.gameObject.GetComponent<Paintable>();
            if (p != null)
            {
                Vector3 pos = other.ClosestPointOnBounds(transform.position);
                PaintManager.instance.Paint(p, pos, radius, hardness, strength, rend.material.color);
            }
        }
        Destroy(gameObject);
    }
}