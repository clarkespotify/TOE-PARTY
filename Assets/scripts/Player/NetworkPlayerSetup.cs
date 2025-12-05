using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerSetup : NetworkBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public AudioListener audioListener;

    [Header("Scripts to Disable for Remote Players")]
    public MonoBehaviour[] scriptsToDisable; // Add your movement/look scripts here

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // Disable camera for other players
            if (playerCamera != null)
                playerCamera.enabled = false;

            // Disable audio listener for other players
            if (audioListener == null)
                audioListener = GetComponentInChildren<AudioListener>();
            if (audioListener != null)
                audioListener.enabled = false;

            // Disable movement and look scripts for other players
            Debug.Log($"Disabling {scriptsToDisable.Length} scripts for remote player");
            foreach (var script in scriptsToDisable)
            {
                if (script != null)
                {
                    Debug.Log($"Disabling script: {script.GetType().Name}");
                    script.enabled = false;
                }
            }

            Debug.Log("Remote player spawned - controls disabled");
        }
        else
        {
            Debug.Log($"Local player spawned - {scriptsToDisable.Length} scripts remain enabled");
            Debug.Log("Local player controls enabled");
        }
    }
}