using System.Collections.Generic;
using UnityEngine;

public class ExplosiveBullet : Bullet
{
    [Header("Explosive Propierties")]
    public float oneShotRadius;
    public float splashRadius;

    public float splashMaxDmg;
    public float splashMinDmg;

    [SerializeField] GameObject explosionObject;

    List<Collider> bigDmgColliders = new();

    void Start()
    {
        rb.velocity = transform.forward * speed;

        rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            rend.material.color = SceneManagerScript.Instance.GetTeamColor(teamTag);
        }

        meshScaleVec.Set(meshScale, meshScale, meshScale);

        meshTransform.localScale = meshScaleVec;

        gameObject.tag = teamTag + "Bullet";

        initPos = transform.position;
    }

    private void FixedUpdate()
    {
        if (Vector3.Distance(transform.position, initPos) > range)
        {
            OnExplosion();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        OnExplosion();
    }

    public void OnExplosion()
    {
        // Visual Effect
        GameObject explo = Instantiate(explosionObject, transform.position, transform.rotation);
        explo.GetComponent<Explosive>().maxRadius = splashRadius * 1.5f;
        explo.GetComponent<Renderer>().material.color = SceneManagerScript.Instance.GetTeamColor(teamTag);

        // Lethal Radius

        Collider[] colliders = Physics.OverlapSphere(transform.position, oneShotRadius);

        foreach (Collider hit in colliders)
        {
            if (hit.CompareTag(SceneManagerScript.Instance.GetRivalTag(teamTag)) && this.CompareTag(teamTag + "Bullet"))
            {
                hit.GetComponent<PlayerStats>().HP -= DMG;
            }

            // Paint only objects affected by lethal dmg area???
            Paintable p = hit.GetComponent<Paintable>();
            if (p != null)
            {
                Vector3 pos = hit.ClosestPointOnBounds(transform.position);
                PaintManager.instance.Paint(p, pos, pRadius, pHardness, pStrength, rend.material.color);
            }

            bigDmgColliders.Add(hit);
        }

        // Splash Radius

        colliders = Physics.OverlapSphere(transform.position, splashRadius);

        foreach (Collider hit in colliders)
        {
            if (!bigDmgColliders.Contains(hit))
            {
                if (hit.CompareTag(SceneManagerScript.Instance.GetRivalTag(teamTag)) && this.CompareTag(teamTag + "Bullet"))
                {
                    float dist = Vector3.Distance(transform.position, hit.ClosestPointOnBounds(transform.position));

                    float dmgpercent = 1f - (dist / splashRadius);
                    dmgpercent = Mathf.Clamp01(dmgpercent);

                    float dmgToDeal = dmgpercent * splashMaxDmg;

                    if(dmgToDeal < splashMinDmg) dmgToDeal = splashMinDmg;

                    hit.GetComponent<PlayerStats>().HP -= dmgToDeal;
                }
            }
        }

        bigDmgColliders.Clear();

        Destroy(gameObject);
    }
}
