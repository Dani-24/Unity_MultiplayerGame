using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    public string teamTag;

    [Header("Debug")]
    [SerializeField] bool infiniteInk = false;
    [SerializeField] bool infiniteHP = false;
    [SerializeField] string presetTeam;

    [Header("HP")]
    public float HP = 100.0f;
    [HideInInspector] public float maxHP;

    public LifeState lifeState = LifeState.alive;
    public LifeState netLifeState = LifeState.alive;
    [SerializeField] string lastDmgCause;
    [SerializeField] string lastPlayerHitYou;

    [SerializeField][Tooltip("Time without taking dmg needed to start regen HP")][Range(0f, 3f)] float recoveryTime = 1.5f;
    [SerializeField][Range(1f, 10f)] float regenHPSpeed = 2f;
    [SerializeField][Range(1f, 20f)] float regenHPSpeedOnInk = 2f;
    float lastFrameHP;
    float regenCount;

    [SerializeField]
    [Range(1f, 10f)] float respawnTime;
    [SerializeField] float timeUntilRespawn;

    [Header("Ink")]
    public float ink = 100.0f;
    float inkCapacity;

    [SerializeField][Range(0.1f, 20f)] float inkReloadSpeed = 1f;
    [SerializeField][Range(1f, 30f)] float inkReloadSpeedOnInk = 5f;

    [Header("Other")]

    public bool playerInputEnabled;

    public Vector3 spawnPos = Vector3.zero;

    private CharacterController controller;

    [SerializeField][Range(-25f, 0f)] float minYaxis = -20;

    [SerializeField] MeshRenderer teamColorGO;

    [Header("Death Related Things")]
    [SerializeField] GameObject DeathInkExplosion;
    [SerializeField] AudioClip deathSFX;

    [SerializeField] GameObject respawnCanvas;
    [SerializeField] TMP_Text respawnText;
    [SerializeField] Slider respawnSlider;

    void Start()
    {
        lastFrameHP = maxHP = HP;
        inkCapacity = ink;

        controller = GetComponent<CharacterController>();

        teamTag = SceneManagerScript.Instance.SetTeam(gameObject, presetTeam);

        timeUntilRespawn = respawnTime;
        respawnCanvas.SetActive(false);
    }

    void Update()
    {
        // Check Color & Team
        teamColorGO.material.color = SceneManagerScript.Instance.GetTeamColor(teamTag);

        if (GetComponent<PlayerNetworking>().isOwnByThisInstance)
        {
            // Autoset teams on lobby
            for (int i = 0; i < ConnectionManager.Instance.playerPackages.Count; i++)
            {
                if (ConnectionManager.Instance.playerPackages[i].netID == GetComponent<PlayerNetworking>().networkID)
                {
                    if (i % 2 == 0) ChangeTag("Alpha"); else ChangeTag("Beta");
                    break;
                }
            }

            // Avoid Own Player Bugs
            netLifeState = lifeState;
        }

        /*
          
        Toca Ajustar todo el switch este para que desde RED se actualice el lifestate siguiendo el orden. 
        Si el paquete recibe que RESPAWNING y este est� ALIVE, este tendr� que pasar por DEATH y luego ir a RESPAWNING
        Vease, que sea ciclico todo y no pueda saltarse pasos
         
         */

        switch (lifeState)
        {
            case LifeState.alive:

                if (controller.enabled == false)
                {
                    controller.enabled = true;
                    HP = maxHP;
                    GetComponent<PlayerMovement>().playerBody.SetActive(true);
                }

                if (netLifeState != lifeState)
                    lifeState = LifeState.death;

                if (!GetComponent<PlayerNetworking>().isOwnByThisInstance) return;

                // Check Death
                if (transform.position.y < minYaxis || HP <= 0) lifeState = LifeState.death;

                // Healing
                if (HP != maxHP)
                    RegenHealth();

                if (HP > maxHP) { HP = maxHP; }

                // Reloading
                ReloadInk();

                break;
            case LifeState.death:

                lifeState = LifeState.respawning;
                timeUntilRespawn = respawnTime;

                transform.parent = null;
                controller.enabled = false;

                GetComponent<PlayerMovement>().playerBody.SetActive(false);

                GameObject deathAnimFX = Instantiate(DeathInkExplosion, transform);
                deathAnimFX.GetComponent<AudioSource>().clip = deathSFX;
                deathAnimFX.GetComponent<AudioSource>().Play();
                deathAnimFX.transform.parent = null;
                deathAnimFX.GetComponent<Explosive>().maxRadius = 5;
                deathAnimFX.GetComponent<Renderer>().material.color = SceneManagerScript.Instance.GetTeamColor(SceneManagerScript.Instance.GetRivalTag(teamTag));

                break;
            case LifeState.respawning:

                if (timeUntilRespawn > 0)
                {
                    timeUntilRespawn -= Time.deltaTime;

                    playerInputEnabled = false;

                    if (GetComponent<PlayerNetworking>().isOwnByThisInstance)
                    {
                        respawnCanvas.SetActive(true);
                        respawnText.text = timeUntilRespawn.ToString("F0");
                        respawnSlider.minValue = 0;
                        respawnSlider.maxValue = respawnTime;

                        float value = respawnTime - timeUntilRespawn;
                        respawnSlider.value = value;
                    }

                    // Sync Netcode
                    if (netLifeState != lifeState)
                        Respawn();

                    break;
                }

                Respawn();

                break;
        }

        // Debug
        if (infiniteHP) { HP = maxHP; }
        if (infiniteInk) { ink = inkCapacity; }

        // Net
        if (!GetComponent<PlayerNetworking>().isOwnByThisInstance) { infiniteInk = true; }
    }

    void Respawn()
    {
        lifeState = LifeState.alive;

        transform.SetPositionAndRotation(spawnPos, Quaternion.Euler(Vector3.zero));
        respawnCanvas.SetActive(false);
        playerInputEnabled = true;
        ink = inkCapacity;
    }

    void ReloadInk()
    {
        if (ink < inkCapacity && !GetComponent<PlayerArmament>().weaponShooting /*&& !GetComponent<PlayerMovement>().subWeaponShooting*/)
        {
            if (GetComponent<PlayerMovement>().groundInk == PlayerMovement.GroundInk.AllyInk && GetComponent<PlayerMovement>().isRunning)
                ink += inkReloadSpeedOnInk * Time.deltaTime;
            else
                ink += inkReloadSpeed * Time.deltaTime;
        }

        if (ink > inkCapacity) { ink = inkCapacity; }
    }

    void RegenHealth()
    {
        if (HP == lastFrameHP)
        {
            regenCount -= Time.deltaTime;

            if (regenCount <= 0)
            {
                if (GetComponent<PlayerMovement>().groundInk == PlayerMovement.GroundInk.AllyInk && GetComponent<PlayerMovement>().isRunning)
                    HP += regenHPSpeedOnInk * Time.deltaTime;
                else
                    HP += regenHPSpeed * Time.deltaTime;
            }
        }
        else
            regenCount = recoveryTime;

        lastFrameHP = HP;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("teamChanger"))    // This one should be disabled
        {
            ChangeTag(SceneManagerScript.Instance.GetRivalTag(teamTag));
        }

        if (other.CompareTag("Death"))
        {
            lifeState = LifeState.death;
        }
    }

    public void ChangeTag(string newTag)
    {
        if (newTag != teamTag)
        {
            teamTag = SceneManagerScript.Instance.SetTeam(gameObject, newTag);
        }
    }

    public void OnDMGReceive(string whatDealsTheDMG, float DMG, string whoDealsTheDMG)
    {
        // NETCODE
        if (!GetComponent<PlayerNetworking>().isOwnByThisInstance)
        {
            Package dmgPckg = ConnectionManager.Instance.WritePackage();
            dmgPckg.dmGPackage = new DMGPackage
            {
                dmg = DMG,
                cause = whatDealsTheDMG,
                dealer = whoDealsTheDMG,
                receiverID = GetComponent<PlayerNetworking>().networkID
            };

            ConnectionManager.Instance.SendPackage(dmgPckg);
            return;
        }

        if (lifeState != LifeState.alive) return;

        HP -= DMG;
        lastPlayerHitYou = whatDealsTheDMG;
        lastDmgCause = whoDealsTheDMG;
    }

    public enum LifeState
    {
        alive,
        death,
        respawning
    }
}
