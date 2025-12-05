using UnityEngine;
using Unity.Netcode;

public class soundboards : NetworkBehaviour
{
    public AudioSource soundboard1;
    public AudioSource soundboard2;

    void Update()
    {
        // Any player can trigger sounds
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlaySoundServerRpc(1, NetworkManager.Singleton.LocalClientId);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PlaySoundServerRpc(2, NetworkManager.Singleton.LocalClientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void PlaySoundServerRpc(int soundIndex, ulong clientId)
    {
        // Server tells all clients to play the sound and show UI
        PlaySoundClientRpc(soundIndex, clientId);
    }

    [ClientRpc]
    void PlaySoundClientRpc(int soundIndex, ulong clientId)
    {
        // Play the sound
        AudioSource selectedSound = null;
        if (soundIndex == 1)
        {
            selectedSound = soundboard1;
            soundboard1.Play();
        }
        else if (soundIndex == 2)
        {
            selectedSound = soundboard2;
            soundboard2.Play();
        }

        // Find the player who pressed the key and show their UI
        if (selectedSound != null)
        {
            ShowPlayerUI(clientId, selectedSound.clip.length);
        }
    }

    void ShowPlayerUI(ulong clientId, float duration)
    {
        // Find the player's NetworkObject
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
            NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.NetworkObjectId,
            out NetworkObject playerObject))
        {
            // Get the UI component on the player
            PlayerSoundboardUI playerUI = playerObject.GetComponent<PlayerSoundboardUI>();
            if (playerUI != null)
            {
                playerUI.ShowIndicator(duration);
            }
        }
    }
}