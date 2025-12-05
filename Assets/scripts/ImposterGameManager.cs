using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class ImposterGameManager : NetworkBehaviour
{
    public static ImposterGameManager Instance { get; private set; }

    [Header("Word Settings")]
    [Tooltip("Text file containing words (one word per line)")]
    public TextAsset wordFile;

    [Tooltip("The word shown to the imposter")]
    public string imposterWord = "IMPOSTER!!";

    private List<string> wordList;
    private string chosenWord;
    private ulong imposterClientId;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Only server assigns roles
        if (IsServer)
        {
            Debug.Log("🎮 GameManager: Server started, loading words...");
            LoadWords();

            // Wait for all clients to connect before assigning roles
            Invoke(nameof(AssignRoles), 1f);
        }
        else
        {
            Debug.Log($"🎮 GameManager: Client {NetworkManager.Singleton.LocalClientId} connected");
        }
    }

    void Update()
    {
        // Check for R key press - only host (server) or client ID 0 can restart
        if (Input.GetKeyDown(KeyCode.R))
        {
            ulong myClientId = NetworkManager.Singleton.LocalClientId;

            // Allow if this is the server OR if this is client ID 0
            if (IsServer || myClientId == 0)
            {
                Debug.Log($"🔄 R key pressed by authorized user (ClientId: {myClientId}) - requesting restart...");
                RestartGameServerRpc();
            }
            else
            {
                Debug.LogWarning($"⛔ Client {myClientId} tried to restart but only the host can do this!");
            }
        }
    }

    void LoadWords()
    {
        wordList = new List<string>();

        if (wordFile != null)
        {
            string[] lines = wordFile.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                string word = line.Trim();
                if (!string.IsNullOrEmpty(word))
                {
                    wordList.Add(word);
                }
            }

            Debug.Log($"✅ Loaded {wordList.Count} words from file");
        }
        else
        {
            Debug.LogError("❌ No word file assigned! Please assign a TextAsset in the Inspector");
        }
    }

    void AssignRoles()
    {
        if (!IsServer) return;

        if (wordList == null || wordList.Count == 0)
        {
            Debug.LogError("❌ No words available! Cannot start game.");
            return;
        }

        // Get all connected clients
        var connectedClients = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);

        if (connectedClients.Count < 2)
        {
            Debug.LogWarning("⚠️ Need at least 2 players to play! Currently: " + connectedClients.Count);
            return;
        }

        // Pick random word and imposter
        chosenWord = wordList[Random.Range(0, wordList.Count)];
        imposterClientId = connectedClients[Random.Range(0, connectedClients.Count)];

        Debug.Log($"🎲 Game Starting!");
        Debug.Log($"   Word: {chosenWord}");
        Debug.Log($"   Imposter: Client {imposterClientId}");
        Debug.Log($"   Total Players: {connectedClients.Count}");

        // Send words to each client
        foreach (var clientId in connectedClients)
        {
            string wordToSend = (clientId == imposterClientId) ? imposterWord : chosenWord;

            Debug.Log($"📤 Sending '{wordToSend}' to Client {clientId} {(clientId == imposterClientId ? "(IMPOSTER)" : "(Innocent)")}");

            SendWordToClientRpc(wordToSend, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            });
        }
    }

    [ClientRpc]
    void SendWordToClientRpc(string word, ClientRpcParams clientRpcParams = default)
    {
        ulong myClientId = NetworkManager.Singleton.LocalClientId;

        Debug.Log($"📥 CLIENT {myClientId} RECEIVED: '{word}'");

        // Find all card text components in scene
        RandomCardText[] allCards = FindObjectsByType<RandomCardText>(FindObjectsSortMode.None);

        Debug.Log($"   Found {allCards.Length} cards in scene");

        // Update all cards with this client's ID
        foreach (var card in allCards)
        {
            card.SetWord(word, myClientId);
        }

        Debug.Log($"   ✅ Cards updated for Client {myClientId}");
    }

    /// <summary>
    /// Call this to restart the game with new roles
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RestartGameServerRpc()
    {
        if (IsServer)
        {
            Debug.Log("🔄 Restarting game...");
            AssignRoles();
        }
    }

    // Add these methods to your ImposterGameManager.cs class
    // Place them at the end of the class, before the closing brace }

    /// <summary>
    /// Check if a specific client is the imposter
    /// </summary>
    public bool IsImposter(ulong clientId)
    {
        return clientId == imposterClientId;
    }

    /// <summary>
    /// Get the imposter's client ID (for debugging or admin purposes)
    /// </summary>
    public ulong GetImposterClientId()
    {
        return imposterClientId;
    }

    // IMPORTANT: You also need to make imposterClientId accessible
    // Find this line in your ImposterGameManager:
    //     private ulong imposterClientId;
    // 
    // And make sure it stays as 'private' (it's fine as is)
    // The methods above will access it properly
}