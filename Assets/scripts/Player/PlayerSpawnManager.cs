using UnityEngine;
using Unity.Netcode;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance { get; private set; }

    [SerializeField] private GameEvent eventChannel;

    [Header("Spawn Points")]
    [Tooltip("Assign your spawn point empty GameObjects in order (0 = Player 1, 1 = Player 2, etc.)")]
    public Transform[] spawnPoints;

    [Header("Spawn Settings")]
    [Tooltip("Height offset to spawn player at their feet instead of center. Set to half your player's height.")]
    public float playerHeightOffset = 1f; // Adjust this to match your player's half-height

    private int nextSpawnIndex = 0;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Validate spawn points
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points assigned! Please assign spawn point transforms in the Inspector.");
        }
        else
        {
            Debug.Log($"PlayerSpawnManager initialized with {spawnPoints.Length} spawn points");
        }
    }

    void Start()
    {
        // Set up connection approval to assign spawn positions
        if (NetworkManager.Singleton != null)
        {
            // Register callback BEFORE starting host/server
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            Debug.Log("Connection approval callback registered");

            // Subscribe to connection events to see what's happening
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnServerStarted()
    {
        Debug.Log("=== SERVER STARTED ===");
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"=== CLIENT CONNECTED === ClientId: {clientId}");
    }

    void OnDestroy()
    {
        // Clean up
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = null;
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // Approve the connection
        response.Approved = true;
        response.CreatePlayerObject = true;

        // Get the next spawn point
        Vector3 spawnPosition = Vector3.zero;
        Quaternion spawnRotation = Quaternion.identity;

        Debug.Log($"=== APPROVAL CHECK START ===");
        Debug.Log($"Next spawn index: {nextSpawnIndex}");
        Debug.Log($"Spawn points array length: {(spawnPoints != null ? spawnPoints.Length : 0)}");

        if (spawnPoints != null && spawnPoints.Length > 0 && nextSpawnIndex < spawnPoints.Length)
        {
            Transform spawnPoint = spawnPoints[nextSpawnIndex];

            Debug.Log($"Selected spawn point: {spawnPoint.name}");
            Debug.Log($"Spawn point LOCAL position: {spawnPoint.localPosition}");
            Debug.Log($"Spawn point WORLD position: {spawnPoint.position}");
            Debug.Log($"Spawn point parent: {(spawnPoint.parent != null ? spawnPoint.parent.name : "NULL")}");

            // Offset spawn position upward so player's FEET are at the spawn point
            spawnPosition = spawnPoint.position + Vector3.up * playerHeightOffset;
            spawnRotation = spawnPoint.rotation;

            Debug.Log($"playerHeightOffset: {playerHeightOffset}");
            Debug.Log($"FINAL calculated spawn position: {spawnPosition}");

            // Move to next spawn point (loop back if needed)
            nextSpawnIndex = (nextSpawnIndex + 1) % spawnPoints.Length;
        }
        else
        {
            Debug.LogError($"SPAWN POINT ERROR! nextSpawnIndex={nextSpawnIndex}, array length={spawnPoints.Length}");
        }

        // Set the spawn position for this player
        response.Position = spawnPosition;
        response.Rotation = spawnRotation;

        Debug.Log($"=== RESPONSE SET === Position: {response.Position}, Rotation: {response.Rotation}");
    }

    // Optional: Reset spawn index (useful if you want to restart)
    public void ResetSpawnIndex()
    {
        nextSpawnIndex = 0;
        Debug.Log("Spawn index reset to 0");
    }

    // ✅ NEW METHOD: Get next spawn point for manual spawning
    public void GetNextSpawnPoint(out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform spawnPoint = spawnPoints[nextSpawnIndex];

            // Offset spawn position upward so player's FEET are at the spawn point
            position = spawnPoint.position + Vector3.up * playerHeightOffset;
            rotation = spawnPoint.rotation;

            Debug.Log($"GetNextSpawnPoint: index {nextSpawnIndex}, position {position}");

            // Move to next spawn point (loop back if needed)
            nextSpawnIndex = (nextSpawnIndex + 1) % spawnPoints.Length;
        }
        else
        {
            Debug.LogWarning("No spawn points available in GetNextSpawnPoint!");
        }
    }
}