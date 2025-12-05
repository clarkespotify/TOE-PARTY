using UnityEngine;
using Unity.Netcode; // ADD THIS
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class FirstPersonMovement : NetworkBehaviour // CHANGED FROM MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How fast the player walks")]
    public float walkSpeed = 5f;

    [Tooltip("How fast the player runs")]
    public float runSpeed = 8f;

    [Tooltip("How high the player jumps")]
    public float jumpHeight = 2f;

    [Header("Physics")]
    [Tooltip("Gravity force applied to player")]
    public float gravity = -9.81f;

    [Header("Camera FOV")]
    [Tooltip("The camera to modify FOV on")]
    public Camera playerCamera;

    [Tooltip("Normal field of view")]
    public float normalFOV = 60f;

    [Tooltip("FOV when sprinting")]
    public float sprintFOV = 70f;

    [Tooltip("How fast FOV transitions")]
    public float fovTransitionSpeed = 8f;

    [Header("Ground Check")]
    [Tooltip("Transform at the player's feet to check if grounded")]
    public Transform groundCheck;

    [Tooltip("Radius of ground check sphere")]
    public float groundDistance = 0.4f;

    [Tooltip("What layers count as ground")]
    public LayerMask groundMask;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Auto-find camera if not assigned
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                normalFOV = playerCamera.fieldOfView;
            }
        }

        // Auto-create ground check if it doesn't exist
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.parent = transform;
            groundCheckObj.transform.localPosition = new Vector3(0, -1f, 0);
            groundCheck = groundCheckObj.transform;
        }
    }

    void Update()
    {
        // ONLY allow input if this is OUR player
        if (!IsOwner) return;

        // Check if player is on the ground
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // Reset falling velocity when grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small negative value to keep grounded
        }

        // Get input
        Vector2 moveInput = GetMoveInput();
        bool isRunning = GetRunInput();
        bool jumpPressed = GetJumpInput();

        // Calculate movement direction (relative to where player is facing)
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        // Apply speed
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // Jump
        if (jumpPressed && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Smooth FOV transition
        if (playerCamera != null)
        {
            float targetFOV = (isRunning && moveInput.magnitude > 0.1f) ? sprintFOV : normalFOV;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, fovTransitionSpeed * Time.deltaTime);
        }
    }

    Vector2 GetMoveInput()
    {
#if ENABLE_INPUT_SYSTEM
            Vector2 input = Vector2.zero;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) input.y += 1;
                if (Keyboard.current.sKey.isPressed) input.y -= 1;
                if (Keyboard.current.aKey.isPressed) input.x -= 1;
                if (Keyboard.current.dKey.isPressed) input.x += 1;
            }
            return input.normalized;
#else
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");
        return new Vector2(x, y);
#endif
    }

    bool GetRunInput()
    {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
#else
        return Input.GetKey(KeyCode.LeftShift);
#endif
    }

    bool GetJumpInput()
    {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    // Visualize ground check in editor
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}