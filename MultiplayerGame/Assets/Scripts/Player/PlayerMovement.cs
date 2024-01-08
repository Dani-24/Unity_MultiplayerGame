using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    CharacterController controller;
    PlayerInput input;

    public GameObject playerBody;

    public bool isUsingGamepad;

    #region Horizontal Movement Propierties

    [Header("Movement")]
    [SerializeField] private Vector2 moveInput;

    Vector3 forward;
    Vector3 moveDir;

    [SerializeField][Range(0.1f, 25f)] private float moveSpeed = 10.0f;
    [SerializeField][Range(0.1f, 25f)] private float runSpeed = 20.0f;
    //[SerializeField] private float slowSpeed = 5.0f;
    [Range(1f, 10f)] public float weaponSpeedMultiplier = 1.0f;

    public bool isRunning = false;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 10.0f;
    [SerializeField] private float rotationSpeedWhileShooting = 100.0f;

    #endregion

    #region Vertical Movement Propierties

    [Header("Gravity")]
    [SerializeField] float gravity = 9.8f;
    [SerializeField] float gravityMultiplier = 1.0f;
    [SerializeField] float fallSpeed;
    [SerializeField] float maxFallSpeed;
    [SerializeField] float groundedFallSpeed = -3.0f;

    public bool isGrounded;
    float originalStepOffset;

    [Header("Jumping")]
    [SerializeField] float jumpForce = 5f;
    [SerializeField] bool isJumping = false;

    [SerializeField][Range(0f, 2f)] float groundCheckDist = 0.1f;
    public List<LayerMask> groundLayers = new List<LayerMask>();

    #endregion

    [Header("Network")]
    [SerializeField] int interpolationSpeed = 20;

    [Header("Ground Paint")]
    [SerializeField] Texture maskT;
    [SerializeField] Texture2D texture;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInput>();

        originalStepOffset = controller.stepOffset;

        texture = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
    }

    void Update()
    {
        UIInputs();

        //CheckGroundPaint();
    }

    private void FixedUpdate()
    {
        if (GetComponent<PlayerNetworking>().isOwnByThisInstance)
        {
            if (GetComponent<PlayerStats>().playerInputEnabled)
            {
                GetMovementDirection();
                BodyRotation();
                Movement();
                JumpingAndFalling();
            }
            else
            {
                Falling();

                moveDir.Set(0, fallSpeed, 0);
            }
        }
        controller.Move(moveDir * Time.deltaTime);
    }

    #region Player Movement

    void GetMovementDirection()
    {
        forward = GetComponent<PlayerOrbitCamera>().GetCameraTransform().forward;
        Vector3 right = GetComponent<PlayerOrbitCamera>().GetCameraTransform().right;

        forward.y = right.y = 0;

        forward = forward.normalized;
        right = right.normalized;

        Vector3 forwardRelativeVerticalInput = moveInput.y * forward;
        Vector3 rigthRelativeHorizontalInput = moveInput.x * right;

        moveDir = forwardRelativeVerticalInput + rigthRelativeHorizontalInput;
        moveDir.Normalize();
    }

    void Movement()
    {
        if (moveInput != Vector2.zero)
        {
            if (GetComponent<PlayerArmament>().weaponShooting || GetComponent<PlayerArmament>().subWeaponShooting)
            {
                moveDir *= moveSpeed * weaponSpeedMultiplier;
            }
            else if (!isRunning)
            {
                moveDir *= moveSpeed;
            }
            else
            {
                moveDir *= runSpeed;
            }
        }
    }

    void BodyRotation()
    {
        if (moveDir != Vector3.zero || GetComponent<PlayerArmament>().weaponShooting || GetComponent<PlayerArmament>().subWeaponShooting)
        {
            Quaternion rotDes = Quaternion.identity;

            if (!GetComponent<PlayerArmament>().weaponShooting && !GetComponent<PlayerArmament>().subWeaponShooting)
            {
                rotDes = Quaternion.LookRotation(moveDir, Vector3.up);
                playerBody.transform.rotation = Quaternion.Slerp(playerBody.transform.rotation, rotDes, rotationSpeed * Time.deltaTime);
            }
            else
            {
                rotDes = Quaternion.LookRotation(forward, Vector3.up);
                playerBody.transform.rotation = Quaternion.Slerp(playerBody.transform.rotation, rotDes, rotationSpeedWhileShooting * Time.deltaTime);
            }
        }
    }

    void Falling()
    {
        if (fallSpeed > maxFallSpeed)
        {
            fallSpeed += gravity * gravityMultiplier * Time.deltaTime;
        }
        else
        {
            fallSpeed = maxFallSpeed;
        }
    }

    void JumpingAndFalling()
    {
        Falling();

        foreach (var layer in groundLayers)
        {
            isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDist, layer);

            if (isGrounded) break;
        }

        if (isGrounded)
        {
            controller.stepOffset = originalStepOffset;
            fallSpeed = groundedFallSpeed;

            if (isJumping)
            {
                fallSpeed = jumpForce;
            }
        }
        else
        {
            controller.stepOffset = 0;
        }

        moveDir.y += fallSpeed;
    }

    #endregion

    #region Ground Paint

    void CheckGroundPaint()
    {
        // Lanzar un raycast hac�a abajo y mirar si es suelo pintable
        RaycastHit hit;

        for (int i = 0; i < groundLayers.Count; i++)
        {
            if (Physics.Raycast(transform.position, new Vector3(0, -1, 0), out hit, Mathf.Infinity, groundLayers[i]))
            {
                try
                {
                    Paintable hitPaintable = hit.collider.GetComponent<Paintable>();

                    //Debug.Log(texture.GetPixel((int)pos.x, (int)pos.y));

                    /*Color colorGround = */

                    maskT = hitPaintable.getRenderer().material.GetTexture("_MaskTexture"); //PaintManager.instance.GetPaintColor(hitPaintable, hit.point);

                    Graphics.CopyTexture(maskT, texture);

                    float xNormalized = hit.point.x / texture.width;
                    float yNormalized = hit.point.y / texture.height;

                    int x = Mathf.FloorToInt(xNormalized);
                    int y = Mathf.FloorToInt(yNormalized);

                    Debug.Log(texture.GetPixel(x, y) + " at: " + x + " " + y);

                    //if (hitPaintable != null)
                    //{
                    //    if (colorGround == SceneManagerScript.Instance.GetTeamColor("Alpha"))
                    //    {
                    //        Debug.Log("Alpha team Ink:" + colorGround);
                    //    }
                    //    else if (colorGround == SceneManagerScript.Instance.GetTeamColor("Beta"))
                    //    {
                    //        Debug.Log("Beta team Ink:" + colorGround);
                    //    }
                    //    else
                    //    {
                    //        Debug.Log("No Ink?");
                    //    }
                    //}
                }
                catch
                {
                    // No Paintable Component
                }
            }
        }
    }

    #endregion

    #region Player Input Actions

    void UIInputs()
    {
        if (GetComponent<PlayerNetworking>().isOwnByThisInstance)
        {
            if (input.actions["ShowConsole"].WasReleasedThisFrame()) SceneManagerScript.Instance.showConsole = !SceneManagerScript.Instance.showConsole;

            if (input.actions["OpenUI"].WasReleasedThisFrame()) UI_Manager.Instance.ToggleNetSettings();

            // Check if using Gamepad or not
            if (input.currentControlScheme == "Gamepad") isUsingGamepad = true; else isUsingGamepad = false;
        }
    }

    // Movement
    void OnMove(InputValue value)
    {
        if (GetComponent<PlayerNetworking>().isOwnByThisInstance)
            moveInput = value.Get<Vector2>();
    }

    void OnJump(InputValue value)
    {
        if (GetComponent<PlayerNetworking>().isOwnByThisInstance)
            isJumping = value.isPressed;
    }

    void OnRun(InputValue value)
    {
        if (GetComponent<PlayerNetworking>().isOwnByThisInstance)
            isRunning = value.isPressed;
    }

    // For Network

    //public Vector2 GetMoveInput()
    //{
    //    return moveInput;
    //}

    public bool GetRunInput()
    {
        return isRunning;
    }

    public bool GetJumpInput()
    {
        return isJumping;
    }

    //public void SetMoveInput(Vector2 _input)
    //{
    //    moveInput = _input;
    //}

    public void SetRunInput(bool _run)
    {
        isRunning = _run;
    }

    public void SetJumpInput(bool _jump)
    {
        isJumping = _jump;
    }

    public void SetPosition(Vector3 _position)
    {
        if (controller != null)
        {
            controller.Move(Vector3.LerpUnclamped(controller.transform.position, _position, interpolationSpeed * Time.deltaTime) - controller.transform.position);
        }
    }

    public void SetRotation(Quaternion _rot)
    {
        playerBody.transform.rotation = Quaternion.LerpUnclamped(playerBody.transform.rotation, _rot, interpolationSpeed * Time.deltaTime);
    }

    public void SetFacing(float angle)
    {
        Vector3 rot = transform.eulerAngles;
        rot.y = angle;
        transform.eulerAngles = rot;

        GetComponent<PlayerOrbitCamera>().CameraSetView(angle);
    }

    public void TeleportToSpawnPos()
    {
        controller.enabled = false;
        transform.position = GetComponent<PlayerStats>().spawnPos;
        controller.enabled = true;
    }

    #endregion
}
