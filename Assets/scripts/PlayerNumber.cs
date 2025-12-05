using UnityEngine;
using Unity.Netcode;

public class PlayerNumber : NetworkBehaviour
{
    [Header("Player Info")]
    [Tooltip("This player's number (1, 2, 3, 4...)")]
    public NetworkVariable<int> playerNumber = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [Header("Optional: Display Name")]
    public TMPro.TMP_Text playerNameText; // UI text to show player nickname

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Only the server assigns player numbers
        if (IsServer)
        {
            AssignPlayerNumber();
        }

        // Everyone (including this player) updates their display when the number changes
        playerNumber.OnValueChanged += OnPlayerNumberChanged;

        // Update display immediately if number is already set
        UpdatePlayerDisplay();

        Debug.Log($"Player spawned! Player Number: {playerNumber.Value}, IsOwner: {IsOwner}");
    }

    void AssignPlayerNumber()
    {
        if (!IsServer) return;

        // Count how many players are connected and assign the next number
        int connectedPlayers = NetworkManager.Singleton.ConnectedClientsList.Count;
        playerNumber.Value = connectedPlayers;

        Debug.Log($"Assigned player number {playerNumber.Value} to client {OwnerClientId}");
    }

    void OnPlayerNumberChanged(int oldValue, int newValue)
    {
        Debug.Log($"Player number changed from {oldValue} to {newValue}");
        UpdatePlayerDisplay();
    }

    void UpdatePlayerDisplay()
    {
        // Update the UI text if assigned
        if (playerNameText != null)
        {
            // Try to get nickname from PlayerDataManager
            if (PlayerDataManager.Instance != null)
            {
                string nickname = PlayerDataManager.Instance.GetPlayerName(OwnerClientId);
                Debug.Log($"=== NAME DEBUG ===");
                Debug.Log($"OwnerClientId: {OwnerClientId}");
                Debug.Log($"Retrieved nickname: '{nickname}'");
                Debug.Log($"Has custom name: {PlayerDataManager.Instance.HasCustomName(OwnerClientId)}");
                playerNameText.text = nickname;
                Debug.Log($"Displaying nickname: {nickname} for client {OwnerClientId}");
            }
            else
            {
                // Fallback if PlayerDataManager isn't available
                playerNameText.text = $"PLAYER {playerNumber.Value}";
                Debug.LogWarning("PlayerDataManager not found! Using default name.");
            }
        }

        // If this is the local player, you might want to do something special
        if (IsOwner)
        {
            Debug.Log($"You are Player {playerNumber.Value}!");
            // You could update a UI element here to show "You are Player X"
        }
    }

    // Public method to get this player's number
    public int GetPlayerNumber()
    {
        return playerNumber.Value;
    }

    // Check if this is a specific player number
    public bool IsPlayerNumber(int number)
    {
        return playerNumber.Value == number;
    }

    // Public method to get this player's nickname
    public string GetPlayerNickname()
    {
        if (PlayerDataManager.Instance != null)
        {
            return PlayerDataManager.Instance.GetPlayerName(OwnerClientId);
        }
        return $"Player {playerNumber.Value}";
    }

    public override void OnNetworkDespawn()
    {
        playerNumber.OnValueChanged -= OnPlayerNumberChanged;
        base.OnNetworkDespawn();
    }
}