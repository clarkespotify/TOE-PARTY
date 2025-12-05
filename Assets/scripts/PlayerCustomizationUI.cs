using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections;

public class PlayerCustomizationUI : MonoBehaviour
{
    public TMP_InputField nameInputField;
    private LobbyManager lobbyManager;

    private void Start()
    {
        Debug.Log("PlayerCustomizationUI Started");

        // Find LobbyManager
        lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager == null)
        {
            Debug.LogError("LobbyManager not found!");
        }

        // Wait a moment for network to be ready
        StartCoroutine(InitializeAfterDelay());
    }

    private IEnumerator InitializeAfterDelay()
    {
        // Wait for network and managers to be ready
        yield return new WaitForSeconds(0.5f);

        if (NetworkManager.Singleton != null && nameInputField != null)
        {
            ulong myClientId = NetworkManager.Singleton.LocalClientId;

            // Check if player already has a saved name
            if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.HasCustomName(myClientId))
            {
                nameInputField.text = PlayerDataManager.Instance.GetPlayerName(myClientId);
                Debug.Log($"Loaded existing name: {nameInputField.text}");
            }
            else
            {
                // Set default name
                string defaultName = myClientId == 0 ? "PLAYER 1 (HOST)" : $"Player {myClientId}";
                nameInputField.text = defaultName;
                Debug.Log($"Set default name: {defaultName}");
            }
        }
    }

    public void OnNameChanged()
    {
        Debug.Log("OnNameChanged called");

        // Validation checks
        if (nameInputField == null)
        {
            Debug.LogError("nameInputField is null!");
            return;
        }

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager is null!");
            return;
        }

        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("PlayerDataManager.Instance is null!");
            return;
        }

        // Check if PlayerDataManager is spawned
        if (!PlayerDataManager.Instance.IsSpawned)
        {
            Debug.LogError("PlayerDataManager is not spawned yet! Try again in a moment.");
            return;
        }

        // Get my client ID
        ulong myClientId = NetworkManager.Singleton.LocalClientId;
        string newName = nameInputField.text.Trim();

        if (string.IsNullOrEmpty(newName))
        {
            Debug.LogWarning("Name cannot be empty!");
            return;
        }

        Debug.Log($"Attempting to change name to: {newName} for client {myClientId}");

        // Only use PlayerDataManager - it handles both persistence AND lobby updates
        PlayerDataManager.Instance.SetPlayerName(myClientId, newName);

        // Also update LobbyManager if it exists (for immediate UI update)
        if (lobbyManager != null && lobbyManager.IsSpawned)
        {
            lobbyManager.UpdatePlayerName(myClientId, newName);
        }

        Debug.Log($"✅ Name change request sent: {newName}");
    }
}