using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject menuPanel;
    public TMPro.TMP_InputField joinCodeInput;
    public TMPro.TMP_Text statusText;
    public TMPro.TMP_Text joinCodeDisplayText;

    [Header("Scene Settings")]
    public string gameSceneName = "playscene";

    private string currentJoinCode = "";

    public event Action<string> OnJoinCodeGenerated;
    public string JoinCode => currentJoinCode;
    public GameObject playerDataManagerPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log("Duplicate RelayManager found, destroying this one");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("✅ RelayManager created and set to DontDestroyOnLoad");
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene '{scene.name}' loaded. RelayManager still alive!");

        if (!string.IsNullOrEmpty(currentJoinCode))
        {
            Debug.Log($"Current join code: {currentJoinCode}");
            OnJoinCodeGenerated?.Invoke(currentJoinCode);
        }
    }

    async void Start()
    {
        if (statusText != null) statusText.text = "";
        await InitializeUnityServices();
    }

    async Task InitializeUnityServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Unity Services initialized and signed in!");
        }
        catch (Exception e)
        {
            if (statusText != null) statusText.text = "FAILED TO INITIALIZE: " + e.Message;
            Debug.LogError($"Unity Services initialization failed: {e}");
        }
    }

    /// <summary>
    /// Creates a relay and starts as host WITHOUT loading any scenes.
    /// Returns the join code if successful, null if failed.
    /// </summary>
    public async Task<string> HostLobbyWithoutLoading()
    {
        try
        {
            Debug.Log("🔑 Creating relay for host...");

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("Signing in anonymously...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            // Create relay allocation for up to 4 players (3 + host)
            int maxConnections = 3;
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            Debug.Log($"✅ Relay allocation created for {maxConnections + 1} total players");

            // Get the join code
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            currentJoinCode = joinCode;

            Debug.Log($"✅ Join code generated: {joinCode}");
            OnJoinCodeGenerated?.Invoke(joinCode);

            // Configure transport with relay data
            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            // Start as host (network active, but no scene changes yet)
            NetworkManager.Singleton.StartHost();
            Debug.Log("✅ Started as host (Netcode active)");

            // ===== NEW: Spawn PlayerDataManager =====
            if (playerDataManagerPrefab != null)
            {
                GameObject pdm = Instantiate(playerDataManagerPrefab);
                NetworkObject netObj = pdm.GetComponent<NetworkObject>();

                if (netObj != null)
                {
                    netObj.Spawn();
                    Debug.Log("✅ PlayerDataManager spawned and will persist across scenes");
                }
                else
                {
                    Debug.LogError("❌ PlayerDataManager prefab doesn't have NetworkObject component!");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ PlayerDataManager prefab not assigned! Names won't persist across scenes.");
            }
            // ========================================

            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"❌ Relay Service Error: {e.Message}");
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Unexpected error creating relay: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Joins a relay using a join code WITHOUT loading scenes.
    /// Netcode will automatically sync the client to the host's current scene.
    /// Returns true if successful, false if failed.
    /// </summary>
    public async Task<bool> JoinLobby(string joinCode)
    {
        try
        {
            Debug.Log($"🔑 Joining relay with code: {joinCode}");

            // Check if already connected
            if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
            {
                Debug.LogWarning("Already connected! Shutting down first...");
                NetworkManager.Singleton.Shutdown();
                await Task.Delay(500); // Wait for clean shutdown
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("Signing in anonymously...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            // Join the relay allocation
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            Debug.Log("✅ Successfully joined relay allocation");

            // Configure transport
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("UnityTransport not found!");
                return false;
            }

            transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));
            Debug.Log("✅ Transport configured with relay data");

            // Subscribe to connection callbacks BEFORE starting client
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            // Start as client
            bool started = NetworkManager.Singleton.StartClient();
            Debug.Log($"StartClient result: {started}");

            if (!started)
            {
                Debug.LogError("❌ Failed to start client");
                return false;
            }

            // Wait for connection (timeout after 10 seconds)
            float timeout = 10f;
            float elapsed = 0f;

            while (!NetworkManager.Singleton.IsConnectedClient && elapsed < timeout)
            {
                await Task.Delay(100);
                elapsed += 0.1f;
            }

            if (NetworkManager.Singleton.IsConnectedClient)
            {
                Debug.Log("✅ Successfully connected to host!");
                return true;
            }
            else
            {
                Debug.LogError("❌ Connection timeout");
                NetworkManager.Singleton.Shutdown();
                return false;
            }
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"❌ Relay join failed: {e.Message}");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Joining failed: {e.Message}");
            return false;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"✅ Client connected! ClientId: {clientId}");

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("✅ Connected to host! Netcode will sync scene automatically.");
            // Don't manually load any scenes - Netcode handles this!
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.LogWarning("⚠️ Disconnected from host!");

            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    /// <summary>
    /// Call this from HostScene to load everyone into the play scene.
    /// Only the host should call this.
    /// </summary>
    public void LoadGameScene()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            Debug.Log($"🎮 Host loading game scene: {gameSceneName}");
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("Cannot load scene - not a host!");
        }
    }

    public string GetCurrentJoinCode()
    {
        return currentJoinCode;
    }

    public string GetJoinCode()
    {
        return currentJoinCode;
    }

    public void OnJoinLobbyButton()
    {
        string code = joinCodeInput.text.Trim();
        statusText.text = "JOINING...";
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogError("Join code is empty!");
            return;
        }

        Debug.Log("🔵 Join Lobby Button Pressed. Attempting to join with code: " + code);

        // Call the async join function
        StartCoroutine(JoinLobbyRoutine(code));
    }

    private IEnumerator JoinLobbyRoutine(string code)
    {
        var joinTask = JoinLobby(code);

        // Wait until the task completes
        while (!joinTask.IsCompleted)
            yield return null;

        if (joinTask.Result)
        {
            Debug.Log("✅ Successfully joined lobby!");
        }
        else
        {
            Debug.LogError("❌ Failed to join lobby");
            statusText.text = "DID YOU MISSTYPE?";
        }
    }
}