using System.Collections.Generic;
using UnityEngine;

public class Bomb : SubWeapon
{
    Animator anim;
    Rigidbody rb;

    [Header(" === Bomb parts that need to match the player color ===")]
    [SerializeField]
    private List<Renderer> rend = new List<Renderer>();

    [Header("Painting")]

    public float strength = 1;
    public float hardness = 1;

    [Header("Other")]
    [SerializeField] float minYaxis = -20;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        // Set Direction??
        rb.AddForce(transform.forward * range, ForceMode.Impulse);

        // Set Bomb Color
        if (rend.Count > 0)
        {
            if (tag == "AllyBomb")
            {
                foreach (Renderer r in rend)
                {
                    r.material.color = GameObject.FindGameObjectWithTag("SceneManager").GetComponent<SceneManagerScript>().allyColor;
                }
            }
            else
            {
                foreach (Renderer r in rend)
                {
                    r.material.color = GameObject.FindGameObjectWithTag("SceneManager").GetComponent<SceneManagerScript>().enemyColor;
                }
            }
        }
    }

    void Update()
    {
        if (transform.position.y < minYaxis)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        anim.SetTrigger("megumin");
    }

    public void OnExplosion()
    {
        // Lethal Radius

        // A�ADIR Q CUANDO HITTEE AL PLAYER LANCE 2 RAYCAST, UNO A LA CABEZA Y OTRO A LOS PIES, SI ESTAN EN EL AREA DE EXPLOSION Y EL RAYCAST NO HA CHOCADO CON ALGUNA COVERTURA YENDO A ESOS PUNTOS DESDE LA BOMBA
        // ENTONCES DA�AR� AL PLAYER, SI NO NO

        Collider[] colliders = Physics.OverlapSphere(transform.position, lethalRadius);

        foreach (Collider hit in colliders)
        {
            if (hit.CompareTag("Player") && this.CompareTag("EnemyBomb"))
            {
                hit.GetComponent<PlayerStats>().HP -= dmg;
            }

            if (hit.CompareTag("Enemy") && this.CompareTag("AllyBomb"))
            {
                hit.GetComponent<Dummy>().HP -= dmg;
            }

            // Paint only objects affected by lethal dmg area???
            Paintable p = hit.GetComponent<Paintable>();
            if (p != null)
            {
                Vector3 pos = hit.ClosestPointOnBounds(transform.position);
                PaintManager.instance.paint(p, pos, paintRadius, hardness, strength, rend[0].material.color);
            }
        }

        // Splash Radius

        colliders = Physics.OverlapSphere(transform.position, nonLethalRadius);

        foreach (Collider hit in colliders)
        {
            if (hit.CompareTag("Player") && this.CompareTag("EnemyBomb"))
            {
                hit.GetComponent<PlayerStats>().HP -= splashDmg;
            }

            if (hit.CompareTag("Enemy") && this.CompareTag("AllyBomb"))
            {
                hit.GetComponent<Dummy>().HP -= splashDmg;
            }
        }

        // Paint all in paint Radius
        //colliders = Physics.OverlapSphere(transform.position, paintRadius);

        //foreach (Collider hit in colliders)
        //{
        //    Paintable p = hit.GetComponent<Paintable>();
        //    if (p != null)
        //    {
        //        Vector3 pos = hit.ClosestPointOnBounds(transform.position);
        //        PaintManager.instance.paint(p, pos, paintRadius, hardness, strength, rend[0].material.color);
        //    }
        //}

        Destroy(gameObject);
    }
}
