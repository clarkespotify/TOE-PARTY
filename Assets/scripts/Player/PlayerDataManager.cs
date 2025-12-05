using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;

public class PlayerDataManager : NetworkBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    // Dictionary to store player names by their client ID
    // CRITICAL: NetworkList must be initialized here, not in Awake()
    private NetworkList<PlayerData> playerDataList = new NetworkList<PlayerData>();

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Don't initialize playerDataList here - already done above!
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            // Initialize with default data for host
            SetPlayerName(NetworkManager.Singleton.LocalClientId, "PLAYER 1 (HOST)");
            DontDestroyOnLoad(gameObject);
        }
    }

    // Call this when a player sets their name
    public void SetPlayerName(ulong clientId, string playerName)
    {
        if (IsServer)
        {
            // Update or add player data
            for (int i = 0; i < playerDataList.Count; i++)
            {
                if (playerDataList[i].clientId == clientId)
                {
                    playerDataList[i] = new PlayerData { clientId = clientId, playerName = playerName };
                    return;
                }
            }

            // If not found, add new entry
            playerDataList.Add(new PlayerData { clientId = clientId, playerName = playerName });
        }
        else
        {
            // Client requests server to update name
            SetPlayerNameServerRpc(clientId, playerName);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerNameServerRpc(ulong clientId, string playerName, ServerRpcParams rpcParams = default)
    {
        SetPlayerName(clientId, playerName);
    }

    // Get player name by client ID
    public string GetPlayerName(ulong clientId)
    {
        foreach (var playerData in playerDataList)
        {
            if (playerData.clientId == clientId)
            {
                return playerData.playerName.ToString();
            }
        }

        // Return default if not found
        return clientId == 0 ? "PLAYER 1 (HOST)" : $"Player {clientId}";
    }

    // Check if player has set a custom name
    public bool HasCustomName(ulong clientId)
    {
        foreach (var playerData in playerDataList)
        {
            if (playerData.clientId == clientId)
            {
                return true;
            }
        }
        return false;
    }
}

// Struct to hold player data (must be serializable for NetworkList)
public struct PlayerData : INetworkSerializable, System.IEquatable<PlayerData>
{
    public ulong clientId;
    public FixedString64Bytes playerName;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref playerName);
    }

    public bool Equals(PlayerData other)
    {
        return clientId == other.clientId && playerName.Equals(other.playerName);
    }
}