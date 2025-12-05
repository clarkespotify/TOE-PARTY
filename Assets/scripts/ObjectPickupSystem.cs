using UnityEngine;
using TMPro;
using Unity.Netcode;

public class ObjectPickupSystem : NetworkBehaviour
{
    [Header("Raycast Settings")]
    [Tooltip("The camera to raycast from")]
    public Camera playerCamera;

    [Tooltip("Maximum distance to detect objects")]
    public float maxRaycastDistance = 3f;

    [Tooltip("Layer mask for pickupable objects (optional)")]
    public LayerMask pickupLayer;

    [Header("UI Settings (Scene Objects - Found at Runtime)")]
    [Tooltip("Name of the UI GameObject to find in the scene")]
    public string promptUIName = "PickupPrompt";

    [Tooltip("The text to display on the prompt")]
    public string promptKeyText = "E";

    [Header("Pickup Settings")]
    [Tooltip("Key to press to pickup/drop object")]
    public KeyCode pickupKey = KeyCode.E;

    [Tooltip("Distance in front of camera to hold object")]
    public float holdDistance = 2f;

    [Tooltip("How smoothly the object moves (lower = smoother)")]
    public float moveSpeed = 10f;

    [Tooltip("How smoothly the object rotates (lower = smoother)")]
    public float rotateSpeed = 8f;

    [Header("Audio")]
    public AudioSource CanSound;

    [Header("Audio")]
    public AudioSource CardSound;

    [Header("Audio")]
    public AudioSource CardSound2;

    [Header("Local Visibility Settings")]
    [Tooltip("Name of the child object to show only for the local player")]
    public string localOnlyChildName = "LocalOnlyVisuals";

    [Header("Debug")]
    public bool showDebugRay = true;

    // Private UI references (found at runtime - NOT networked)
    private GameObject promptUI;
    private TextMeshProUGUI promptText;

    // Private pickup variables
    private PickupableObject currentLookTarget;
    private PickupableObject heldObject;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;
    private bool isReturningObject = false;

    void Start()
    {
        // Only initialize UI for the local player (owner of this player object)
        if (!IsOwner) return;

        // Find the player camera
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        // Find the UI elements in the scene at runtime
        promptUI = GameObject.Find(promptUIName);
        if (promptUI == null)
        {
            Debug.LogError($"PickupPrompt UI named '{promptUIName}' not found in scene! Make sure it exists and is named correctly.");
        }
        else
        {
            // Find the TextMeshPro component inside the UI
            promptText = promptUI.GetComponentInChildren<TextMeshProUGUI>();
            if (promptText == null)
            {
                Debug.LogWarning("TextMeshProUGUI not found in PickupPrompt!");
            }
            else
            {
                promptText.text = promptKeyText;
            }

            // Start with UI hidden
            promptUI.SetActive(false);
        }
    }

    void Update()
    {
        // Only the owner (local player) can control their own pickup
        if (!IsOwner) return;

        if (heldObject != null)
        {
            // Currently holding an object
            MoveHeldObject();

            if (Input.GetKeyDown(pickupKey) && !isReturningObject)
            {
                DropObject();
            }
        }
        else
        {
            // Not holding anything - detect what we're looking at
            DetectObjects();

            if (Input.GetKeyDown(pickupKey) && currentLookTarget != null && !isReturningObject)
            {
                PickupObject(currentLookTarget);
            }
        }
    }

    void DetectObjects()
    {
        // Cast a ray from the center of the screen
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * maxRaycastDistance, Color.yellow);
        }

        if (Physics.Raycast(ray, out hit, maxRaycastDistance, pickupLayer))
        {
            PickupableObject pickupable = hit.collider.GetComponent<PickupableObject>();

            if (pickupable != null)
            {
                if (currentLookTarget != pickupable)
                {
                    currentLookTarget = pickupable;
                    ShowPrompt(true);
                    Debug.Log($"Looking at: {pickupable.gameObject.name}");
                }
            }
            else
            {
                ClearLookTarget();
            }
        }
        else
        {
            ClearLookTarget();
        }
    }

    void ClearLookTarget()
    {
        if (currentLookTarget != null)
        {
            currentLookTarget = null;
            ShowPrompt(false);
        }
    }

    void ShowPrompt(bool show)
    {
        // This only affects the local player's UI
        if (promptUI != null)
        {
            promptUI.SetActive(show);
        }
    }

    void PickupObject(PickupableObject obj)
    {
        // Get the NetworkObject component
        NetworkObject netObj = obj.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError($"Cannot pick up {obj.name} - missing NetworkObject component!");
            return;
        }

        // Store original transform data
        originalPosition = obj.transform.position;
        originalRotation = obj.transform.rotation;
        originalParent = obj.transform.parent;

        heldObject = obj;

        // Request ownership and notify the server
        RequestPickupServerRpc(netObj.NetworkObjectId);

        ShowPrompt(false);
        currentLookTarget = null;

        Debug.Log($"Picked up: {obj.gameObject.name}");

        // Play sound if it's a can
        if (heldObject.CompareTag("Can") && CanSound != null)
        {
            CanSound.Play();
        }

        if(heldObject.CompareTag("Card") && CardSound != null)
        {
            CardSound.Play();
        }
    }

    [ServerRpc]
    void RequestPickupServerRpc(ulong objectNetworkId)
    {
        // Find the object by NetworkObjectId
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectNetworkId, out NetworkObject netObj))
        {
            // Change ownership to the requesting player
            netObj.ChangeOwnership(OwnerClientId);

            // Notify all clients about the pickup
            NotifyPickupClientRpc(objectNetworkId, OwnerClientId);
        }
    }

    [ClientRpc]
    void NotifyPickupClientRpc(ulong objectNetworkId, ulong playerClientId)
    {
        // Find the object
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectNetworkId, out NetworkObject netObj))
            return;

        PickupableObject obj = netObj.GetComponent<PickupableObject>();
        if (obj == null) return;

        // Disable physics for everyone
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // Handle local-only child visibility
        // Only show it to the player who picked it up
        bool isLocalPlayer = playerClientId == NetworkManager.Singleton.LocalClientId;
        SetLocalOnlyChildVisibility(obj.gameObject, isLocalPlayer);
    }

    void MoveHeldObject()
    {
        if (heldObject == null) return;

        // Calculate target position in front of camera
        Vector3 targetPosition = playerCamera.transform.position +
                                playerCamera.transform.forward * holdDistance;

        heldObject.transform.position = Vector3.Lerp(
            heldObject.transform.position,
            targetPosition,
            Time.deltaTime * moveSpeed
        );

        // Apply rotation based on object tag
        if (heldObject.CompareTag("Card"))
        {
            // Make card face camera and rotate 90 degrees for upright text
            Vector3 cameraForward = playerCamera.transform.forward;
            Quaternion targetRotation = Quaternion.LookRotation(cameraForward, Vector3.up);
            targetRotation *= Quaternion.Euler(0, 0, 90);

            heldObject.transform.rotation = Quaternion.Slerp(
                heldObject.transform.rotation,
                targetRotation,
                Time.deltaTime * rotateSpeed
            );
        }
        else if (heldObject.CompareTag("Can"))
        {
            // Rotate can 45 degrees towards camera
            Vector3 cameraForward = playerCamera.transform.forward;
            Quaternion targetRotation = Quaternion.LookRotation(cameraForward, Vector3.up);
            targetRotation *= Quaternion.Euler(45, 0, 180);

            heldObject.transform.rotation = Quaternion.Slerp(
                heldObject.transform.rotation,
                targetRotation,
                Time.deltaTime * rotateSpeed
            );
        }
        else
        {
            // Maintain original rotation for other objects
            heldObject.transform.rotation = Quaternion.Slerp(
                heldObject.transform.rotation,
                originalRotation,
                Time.deltaTime * rotateSpeed
            );
        }
    }

    void DropObject()
    {
        if (heldObject == null) return;

        NetworkObject netObj = heldObject.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            // Notify server about the drop
            RequestDropServerRpc(netObj.NetworkObjectId, originalPosition, originalRotation);
        }

        StartCoroutine(ReturnToOriginalPosition(heldObject));

        Debug.Log($"Dropped: {heldObject.gameObject.name}");
        heldObject = null;

        // Stop audio
        if (CanSound != null)
        {
            CanSound.Stop();
        }

        if (CardSound != null)
        {
            CardSound2.Play();
        }
    }

    [ServerRpc]
    void RequestDropServerRpc(ulong objectNetworkId, Vector3 dropPosition, Quaternion dropRotation)
    {
        // Notify all clients to drop the object
        NotifyDropClientRpc(objectNetworkId, dropPosition, dropRotation);
    }

    [ClientRpc]
    void NotifyDropClientRpc(ulong objectNetworkId, Vector3 dropPosition, Quaternion dropRotation)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectNetworkId, out NetworkObject netObj))
            return;

        PickupableObject obj = netObj.GetComponent<PickupableObject>();
        if (obj == null) return;

        // Re-enable physics
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        // Hide local-only child when dropped (for everyone)
        SetLocalOnlyChildVisibility(obj.gameObject, false);

        // Set final position (the owner already animated it, so others just snap)
        if (!IsOwner)
        {
            obj.transform.position = dropPosition;
            obj.transform.rotation = dropRotation;
        }
    }

    System.Collections.IEnumerator ReturnToOriginalPosition(PickupableObject obj)
    {
        isReturningObject = true;

        float elapsed = 0f;
        float duration = 0.5f;

        Vector3 startPos = obj.transform.position;
        Quaternion startRot = obj.transform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Ease out cubic
            t = 1f - Mathf.Pow(1f - t, 3f);

            obj.transform.position = Vector3.Lerp(startPos, originalPosition, t);
            obj.transform.rotation = Quaternion.Slerp(startRot, originalRotation, t);

            yield return null;
        }

        // Ensure final position/rotation is exact
        obj.transform.position = originalPosition;
        obj.transform.rotation = originalRotation;
        obj.transform.parent = originalParent;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        isReturningObject = false;
    }

    /// <summary>
    /// Shows or hides a child object that should only be visible to the local player who picked it up
    /// </summary>
    void SetLocalOnlyChildVisibility(GameObject pickupableObject, bool visible)
    {
        // Find the child by name
        Transform localChild = pickupableObject.transform.Find(localOnlyChildName);

        if (localChild != null)
        {
            localChild.gameObject.SetActive(visible);
            Debug.Log($"Local-only child '{localOnlyChildName}' set to: {visible}");
        }
    }
}