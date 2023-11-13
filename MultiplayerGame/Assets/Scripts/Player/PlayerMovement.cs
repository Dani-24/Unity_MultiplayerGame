using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static Unity.VisualScripting.Member;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;

    [SerializeField] private GameObject playerBody;

    [Header("Movement")]
    [SerializeField] private Vector2 moveInput;

    [SerializeField] private float moveSpeed = 10.0f;

    [Tooltip("Velocidad al correr")]
    [SerializeField] private float runSpeed = 20.0f;

    [Tooltip("Velocidad al correr sin estar potenciado")]
    [SerializeField] private float runSlowSpeed = 5.0f;

    [SerializeField] private float rotationSpeed = 10.0f;

    public bool isRunning = false;

    [HideInInspector] public Camera cam;

    [SerializeField] float maxFallingSpeed = 1.0f;

    float fallingSpeed = 0f;

    [Header("Jumping")]
    [SerializeField] float jumpForce = 5f;

    [SerializeField] bool jumping = false;

    [SerializeField] float groundCheckDist = 0.1f;

    public LayerMask groundLayer;

    [Header("Weapon")]
    public GameObject weapon;
    public bool weaponShooting = false;

    [Header("SubWeapon")]
    public bool subWeaponShooting = false;

    [Header("DEBUG Texture")]
    [SerializeField] Texture debugTexture;
    [SerializeField] Texture2D debugTexture2d;
    [SerializeField] GameObject debugGameObjectHit;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>();

        GameObject weaponSpawnPoint = GameObject.FindGameObjectWithTag("WeaponSpawn");
        Instantiate(weapon, weaponSpawnPoint.transform);

        fallingSpeed = -maxFallingSpeed;
    }

    void Update()
    {
        // Camera Rotation & applying it to the player model
        Vector3 forward = cam.transform.forward;
        Vector3 right = cam.transform.right;

        forward.y = right.y = 0;

        forward = forward.normalized;
        right = right.normalized;

        Vector3 forwardRelativeVerticalInput = moveInput.y * forward;
        Vector3 rigthRelativeHorizontalInput = moveInput.x * right;

        Vector3 moveDir = forwardRelativeVerticalInput + rigthRelativeHorizontalInput;

        // !!!!!!!!!!!!!!
        // Al resetear la camara habria que hacer una transicion smooth desde la posicion actual a la nueva para que no cambie de golpe el movimiento
        // !!!!!!!!!!!!!!

        if (moveDir != Vector3.zero || weaponShooting || subWeaponShooting)
        {
            Quaternion rotDes = Quaternion.identity;

            if (!weaponShooting && !subWeaponShooting)
            {
                rotDes = Quaternion.LookRotation(moveDir, Vector3.up);
                playerBody.transform.rotation = Quaternion.Slerp(playerBody.transform.rotation, rotDes, rotationSpeed * Time.deltaTime);
            }
            else
            {
                rotDes = Quaternion.LookRotation(forward, Vector3.up);

                playerBody.transform.rotation = Quaternion.Slerp(playerBody.transform.rotation, rotDes, rotationSpeed * 100 * Time.deltaTime);
            }
        }

        if (!isRunning)
        {
            controller.Move(moveDir * Time.deltaTime * moveSpeed);
        }
        else
        {
            controller.Move(moveDir * Time.deltaTime * runSpeed);
        }

        bool isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDist, groundLayer);

        if (isGrounded && jumping)
        {
            fallingSpeed = jumpForce;
        }

        if(fallingSpeed > -maxFallingSpeed)
        {
            fallingSpeed -= Time.deltaTime * 20f;
        }

        controller.Move(new Vector3(0, fallingSpeed * Time.deltaTime, 0));

        CheckGroundPaint();
    }

    void CheckGroundPaint()
    {
        // Lanzar un raycast hac�a abajo y mirar si es suelo pintable

        RaycastHit hit;

        if (Physics.Raycast(transform.position, new Vector3(0, -1, 0), out hit, Mathf.Infinity, groundLayer))
        {
            Renderer hitRenderer = hit.collider.GetComponent<Renderer>();

            if (hitRenderer != null)
            {
                Vector2 uvCoord = hit.textureCoord;
                Material material = hitRenderer.material;

                //Texture2D texture = material.mainTexture as Texture2D;

                Texture texture = material.GetTexture("_MaskTexture");

                debugGameObjectHit = hit.collider.gameObject;
                debugTexture = texture;

                // Crea un nuevo objeto Texture2D
                Texture2D texture2D = null; //TextureToText2D(texture);

                debugTexture2d = texture2D;

                if (texture2D != null)
                {
                    Color pixelColor = texture2D.GetPixelBilinear(uvCoord.x, uvCoord.y);

                    Debug.Log("Color: " + pixelColor);

                    // Comprobar el color del suelo y mirar si es aliado o enemigo

                    // Si es color aliado con shift puedes correr + rapido y recargas rapido

                    // Si es color enemigo te mueves mas lento, recibes un pelin de da�o y usar shift te frena mas

                    if (pixelColor == SceneManagerScript.Instance.allyColor)
                    {
                        Debug.Log("Ally Ink");
                    }
                    else if (pixelColor == SceneManagerScript.Instance.enemyColor)
                    {
                        Debug.Log("Enemy Ink");
                    }
                }
                else
                {
                    Debug.Log("Texture null");
                }
            }
        }
    }

    Texture2D TextureToText2D(Texture source)
    {
        // !!! Dentro de lo que cabe funciona pero Unity muere en el proceso

        RenderTexture renderTex = RenderTexture.GetTemporary(
               source.width,
               source.height,
               0,
               RenderTextureFormat.Default,
        RenderTextureReadWrite.Linear);

        Graphics.Blit(source, renderTex);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTex;
        Texture2D readableText = new Texture2D(source.width, source.height);
        readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
        readableText.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTex);
        return readableText;
    }

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void OnJump(InputValue value)
    {
        jumping = value.isPressed;
    }

    void OnFire(InputValue value)
    {
        if(weapon == null)
        {
            Debug.Log("There is no weapon to shoot");
            return;
        }

        weaponShooting = value.isPressed;
    }

    void OnSubFire(InputValue value)
    {
        subWeaponShooting = value.isPressed;
    }

    void OnCamReset(InputValue value)
    {
        cam.GetComponent<OrbitCamera>().ResetCamera(value.isPressed);
    }

    void OnRun(InputValue value)
    {
        isRunning = value.isPressed;
    }
}
