using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections.Generic;

public class LobbyManager : NetworkBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI joinCodeText;
    public TextMeshProUGUI playerListText;
    public GameObject startButton;

    private Dictionary<ulong, string> connectedPlayers = new Dictionary<ulong, string>();
    private bool isInitialized = false;

    private void Awake()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is null!");
            return;
        }

        NetworkManager.Singleton.NetworkConfig.PlayerPrefab = null;
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;

        Debug.Log("✅ Automatic player spawning DISABLED - using shared camera");
    }

    private void Start()
    {
        // If we're the host and this NetworkObject isn't spawned yet, spawn it
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            NetworkObject netObj = GetComponent<NetworkObject>();
            if (netObj != null && !netObj.IsSpawned)
            {
                Debug.Log("[Start] Host is spawning LobbyManager NetworkObject");
                netObj.Spawn();
            }
        }

        // Try to initialize if we're already connected but OnNetworkSpawn hasn't been called
        if (NetworkManager.Singleton != null &&
            (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient) &&
            !isInitialized)
        {
            Debug.Log("[Start] Manually initializing since OnNetworkSpawn didn't fire");
            Invoke(nameof(LateInitialize), 0.5f);
        }
    }

    private void LateInitialize()
    {
        if (!isInitialized)
        {
            Initialize();
        }
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        response.CreatePlayerObject = false;
        response.Pending = false;

        Debug.Log($"✅ Connection approved for client (no player spawn)");
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"[OnNetworkSpawn] Called! IsServer: {IsServer}, IsClient: {IsClient}");
        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized)
        {
            Debug.Log("[Initialize] Already initialized, skipping");
            return;
        }

        isInitialized = true;

        // Use NetworkManager to determine role, not IsServer/IsClient
        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        bool isClient = NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost;

        Debug.Log($"[Initialize] isHost: {isHost}, isClient: {isClient}, LocalClientId: {NetworkManager.Singleton?.LocalClientId}");

        // Subscribe to connection events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        if (isHost)
        {
            SetupAsHost();
        }
        else if (isClient)
        {
            SetupAsClient();
        }
        else
        {
            Debug.LogWarning("[Initialize] Not host or client yet - will try again");
            // Try again after a delay
            Invoke(nameof(RetryInitialize), 1f);
        }
    }

    private void RetryInitialize()
    {
        if (!isInitialized)
        {
            Debug.Log("[RetryInitialize] Trying initialization again...");
            Initialize();
        }
        else
        {
            // Already initialized, but check if we need to setup
            bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
            bool isClient = NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost;

            if (isClient && connectedPlayers.Count == 0)
            {
                Debug.Log("[RetryInitialize] Setting up as client now");
                SetupAsClient();
            }
        }
    }

    private void SetupAsHost()
    {
        Debug.Log("[SetupAsHost] Setting up host UI");

        if (RelayManager.Instance != null)
        {
            string joinCode = RelayManager.Instance.GetCurrentJoinCode();
            if (!string.IsNullOrEmpty(joinCode))
            {
                joinCodeText.text = $"JOIN CODE: {joinCode}";
                statusText.text = "Lobby Active!";
                Debug.Log($"✅ Join Code: {joinCode}");
            }
        }

        if (startButton != null)
            startButton.SetActive(true);

        // Add host to player list
        AddPlayerLocally(NetworkManager.Singleton.LocalClientId, "Player 0 (Host)");
    }

    private void SetupAsClient()
    {
        Debug.Log("[SetupAsClient] ===== SETTING UP CLIENT UI =====");

        if (statusText != null)
        {
            statusText.text = "Connected to lobby!";
            Debug.Log("[SetupAsClient] Set status text to 'Connected to lobby!'");
        }
        else
        {
            Debug.LogError("[SetupAsClient] statusText is NULL!");
        }

        if (joinCodeText != null)
        {
            joinCodeText.text = "";
            Debug.Log("[SetupAsClient] Cleared join code text");
        }
        else
        {
            Debug.LogError("[SetupAsClient] joinCodeText is NULL!");
        }

        if (startButton != null)
        {
            startButton.SetActive(false);
            Debug.Log("[SetupAsClient] Hid start button");
        }

        // Wait a moment then request player list
        Debug.Log("[SetupAsClient] Will request player list in 1 second...");
        Invoke(nameof(RequestPlayerListDelayed), 3f);
    }

    private void RequestPlayerListDelayed()
    {
        if (IsSpawned)
        {
            RequestPlayerListServerRpc();
        }
        else
        {
            Debug.LogError("Cannot request player list - NetworkObject not spawned!");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[OnClientConnected] Client {clientId} connected. IsServer: {IsServer}");

        if (!IsServer) return;

        string playerName = clientId == 0 ? "Player 0 (Host)" : $"Player {clientId}";
        AddPlayerLocally(clientId, playerName);
        NotifyPlayerJoinedClientRpc(clientId, playerName);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"[OnClientDisconnected] Client {clientId} disconnected");

        if (!IsServer) return;

        RemovePlayerLocally(clientId);
        NotifyPlayerLeftClientRpc(clientId);
    }

    private void AddPlayerLocally(ulong clientId, string playerName)
    {
        connectedPlayers[clientId] = playerName;
        UpdatePlayerListDisplay();
        Debug.Log($"[AddPlayerLocally] Added {clientId}: {playerName}. Total: {connectedPlayers.Count}");
    }

    private void RemovePlayerLocally(ulong clientId)
    {
        if (connectedPlayers.Remove(clientId))
        {
            UpdatePlayerListDisplay();
            Debug.Log($"[RemovePlayerLocally] Removed {clientId}. Total: {connectedPlayers.Count}");
        }
    }

    private void UpdatePlayerListDisplay()
    {
        string playerListString = "PLAYERS:\n";
        foreach (var player in connectedPlayers)
        {
            playerListString += $"{player.Value}\n";
        }

        if (playerListText != null)
        {
            playerListText.text = playerListString;
        }
    }

    // ===== NAME CHANGE METHODS =====

    public void UpdatePlayerName(ulong clientId, string newName)
    {
        Debug.Log($"[UpdatePlayerName] Client {clientId} wants to change name to: {newName}");

        // Check if NetworkObject is spawned
        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("Cannot update name - NetworkObject component missing!");
            return;
        }

        if (!netObj.IsSpawned)
        {
            Debug.LogError("Cannot update name - NetworkObject not spawned yet! Will try when spawned.");
            return;  // Just return, don't retry
        }

        if (IsServer)
        {
            if (connectedPlayers.ContainsKey(clientId))
            {
                connectedPlayers[clientId] = newName;
                UpdatePlayerListDisplay();
                UpdatePlayerNameClientRpc(clientId, newName);
            }
        }
        else
        {
            // Make sure we can call ServerRpc
            try
            {
                RequestNameChangeServerRpc(clientId, newName);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to send name change request: {e.Message}");
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestNameChangeServerRpc(ulong clientId, string newName, ServerRpcParams rpcParams = default)
    {
        Debug.Log($"[ServerRpc] Client {clientId} requested name change to: {newName}");

        if (connectedPlayers.ContainsKey(clientId))
        {
            connectedPlayers[clientId] = newName;
            UpdatePlayerListDisplay();
            UpdatePlayerNameClientRpc(clientId, newName);
        }
    }

    [ClientRpc]
    private void UpdatePlayerNameClientRpc(ulong clientId, string newName)
    {
        Debug.Log($"[ClientRpc] Updating player {clientId} name to: {newName}");

        if (connectedPlayers.ContainsKey(clientId))
        {
            connectedPlayers[clientId] = newName;
            UpdatePlayerListDisplay();
        }
    }

    // ===== NETWORK RPCs =====

    [ServerRpc(RequireOwnership = false)]
    private void RequestPlayerListServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong requestingClient = rpcParams.Receive.SenderClientId;
        Debug.Log($"[ServerRpc] Client {requestingClient} requested player list. Sending {connectedPlayers.Count} players");

        foreach (var player in connectedPlayers)
        {
            SendPlayerDataClientRpc(player.Key, player.Value, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { requestingClient }
                }
            });
        }
    }

    [ClientRpc]
    private void NotifyPlayerJoinedClientRpc(ulong clientId, string playerName)
    {
        Debug.Log($"[ClientRpc] NotifyPlayerJoined: {clientId} - {playerName}");
        AddPlayerLocally(clientId, playerName);
    }

    [ClientRpc]
    private void NotifyPlayerLeftClientRpc(ulong clientId)
    {
        Debug.Log($"[ClientRpc] NotifyPlayerLeft: {clientId}");
        RemovePlayerLocally(clientId);
    }

    [ClientRpc]
    private void SendPlayerDataClientRpc(ulong clientId, string playerName, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"[ClientRpc] SendPlayerData: {clientId} - {playerName}");
        AddPlayerLocally(clientId, playerName);
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        base.OnNetworkDespawn();
    }
}