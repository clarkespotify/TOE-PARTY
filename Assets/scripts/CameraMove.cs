using UnityEngine;
using Unity.Netcode; // ADD THIS
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class FirstPersonCamera : NetworkBehaviour // CHANGED FROM MonoBehaviour
{
    [Header("Mouse Settings")]
    [Tooltip("How fast the camera rotates horizontally")]
    public float mouseSensitivityX = 2f;
    [Tooltip("How fast the camera rotates vertically")]
    public float mouseSensitivityY = 2f;

    [Header("Rotation Limits")]
    [Tooltip("Maximum angle you can look up (positive value)")]
    public float maxLookUpAngle = 90f;
    [Tooltip("Maximum angle you can look down (positive value)")]
    public float maxLookDownAngle = 90f;

    [Header("References")]
    [Tooltip("Drag your camera here")]
    public Transform cameraTransform;

    private float rotationX = 0f; // Camera up/down
    private float rotationY = 0f; // Player left/right

    void Start()
    {
        // ONLY lock cursor if this is OUR player
        if (IsOwner)
        {
            // Lock and hide the cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Auto-find camera if not assigned
        if (cameraTransform == null)
        {
            cameraTransform = GetComponentInChildren<Camera>().transform;
        }

        // Initialize rotation
        rotationY = transform.eulerAngles.y;
        rotationX = cameraTransform.localEulerAngles.x;
    }

    void Update()
    {
        // ONLY allow input if this is OUR player
        if (!IsOwner) return;

        // Get mouse input
        Vector2 mouseDelta = GetMouseDelta();
        float mouseX = mouseDelta.x;
        float mouseY = mouseDelta.y;

        // Horizontal rotation (left/right) - rotate the PLAYER body
        rotationY += mouseX * mouseSensitivityX;
        transform.rotation = Quaternion.Euler(0f, rotationY, 0f);

        // Vertical rotation (up/down) - rotate the CAMERA
        rotationX -= mouseY * mouseSensitivityY;
        rotationX = Mathf.Clamp(rotationX, -maxLookUpAngle, maxLookDownAngle);
        cameraTransform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);

        // Press Escape to unlock cursor
        if (GetEscapePressed())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Click to lock cursor again
        if (GetMouseClick() && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    Vector2 GetMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                return Mouse.current.delta.ReadValue();
            }
            return Vector2.zero;
#else
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
    }

    bool GetEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    bool GetMouseClick()
    {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }
}