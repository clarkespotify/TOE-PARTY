using UnityEngine;
using Unity.Netcode;

public class HideLocalPlayerModel : NetworkBehaviour
{
    [Header("Character Model to Hide")]
    [Tooltip("Drag your character model GameObject here (the mesh/sprite you want to hide for local player)")]
    public GameObject characterModel;
    public GameObject hatModel;
    public GameObject cigModel;

    public override void OnNetworkSpawn()
    {
        // Only hide the model for the LOCAL player (you)
        if (IsOwner)
        {
            if (characterModel != null)
            {
                // Hide the character model for yourself
                characterModel.SetActive(false);
                hatModel.SetActive(false);
                cigModel.SetActive(false);
                Debug.Log("Local player model hidden - first person view!");
            }
            else
            {
                Debug.LogWarning("Character model not assigned! Drag your character GameObject into the inspector.");
            }
        }
        else
        {
            // Other players see your character model
            Debug.Log($"Remote player model visible for client {OwnerClientId}");
        }
    }
}