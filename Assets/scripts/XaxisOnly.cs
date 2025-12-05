using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class XaxisOnly : MonoBehaviour
{
    [Header("Mouse Settings")]
    [Tooltip("How fast the camera rotates horizontally")]
    public float mouseSensitivityX = 2f;

    private float rotationY = 0f; // Tracks horizontal rotation

    void Start()
    {
        // Lock and hide the cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize rotation based on current camera orientation
        Vector3 rot = transform.localRotation.eulerAngles;
        rotationY = rot.y;
    }

    void Update()
    {
        // Get mouse input (only X-axis)
        Vector2 mouseDelta = GetMouseDelta();
        float mouseX = mouseDelta.x;

        // Calculate horizontal rotation (left/right) - no limits
        rotationY += mouseX * mouseSensitivityX;

        // Apply rotation to camera (only Y-axis rotation, no X or Z)
        transform.localRotation = Quaternion.Euler(0f, rotationY, 0f);

        // Press Escape to unlock cursor (optional)
        if (GetEscapePressed())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Click to lock cursor again (optional)
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