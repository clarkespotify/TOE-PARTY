using UnityEngine;

/// <summary>
/// Add this component to any object you want to be pickupable
/// </summary>
public class PickupableObject : MonoBehaviour
{
    [Header("Optional Settings")]
    [Tooltip("Custom name to display (optional)")]
    public string objectName;

    void Start()
    {
        // Set default name if not specified
        if (string.IsNullOrEmpty(objectName))
        {
            objectName = gameObject.name;
        }
    }
}