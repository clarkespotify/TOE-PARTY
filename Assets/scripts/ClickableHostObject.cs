using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using TMPro;

public class ClickableHostObject : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("The lobby UI scene where host configures settings")]
    public string hostLobbySceneName = "hostscene";

    [Header("Visual Feedback")]
    [Tooltip("Optional: Material to use when hovering")]
    public Material hoverMaterial;

    private Material originalMaterial;
    private Renderer objectRenderer;
    private bool isHovering = false;
    public TextMeshProUGUI loading;

    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
        }
    }

    public async void OnClicked()
    {
        Debug.Log("🔵 HOST object clicked - Starting host sequence...");
        Debug.Log($"🔵 Current scene: {SceneManager.GetActiveScene().name}");
        loading.text = "LOADING...";

        // Step 1: Mark as hosting
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetHosting();
            Debug.Log("✅ GameStateManager.SetHosting() called");
        }

        // Step 2: Create relay and start as host
        if (RelayManager.Instance != null)
        {
            string joinCode = await RelayManager.Instance.HostLobbyWithoutLoading();

            if (!string.IsNullOrEmpty(joinCode))
            {
                Debug.Log($"✅ Relay created with code: {joinCode}");
                Debug.Log($"✅ NetworkManager.IsHost: {NetworkManager.Singleton?.IsHost}");

                // Step 3: Hide the start menu UI
                if (StartMenuManager.Instance != null)
                {
                    StartMenuManager.Instance.HideStartMenu();
                    Debug.Log("✅ Start menu hidden");
                }

                // Step 4: Load HostScene using REGULAR SceneManager
                // (No clients connected yet, so don't use Netcode scene manager)
                Debug.Log($"🔵 Loading {hostLobbySceneName} (lobby UI) using regular SceneManager...");
                NetworkManager.Singleton.SceneManager.LoadScene(hostLobbySceneName, LoadSceneMode.Single);
            }
            else
            {
                Debug.LogError("❌ Failed to create relay - no join code received");
            }
        }
        else
        {
            Debug.LogError("❌ RelayManager.Instance is null!");
        }
    }

    void OnMouseEnter()
    {
        isHovering = true;
        if (objectRenderer != null && hoverMaterial != null)
        {
            objectRenderer.material = hoverMaterial;
        }
    }

    void OnMouseExit()
    {
        isHovering = false;
        if (objectRenderer != null && originalMaterial != null)
        {
            objectRenderer.material = originalMaterial;
        }
    }

    void OnMouseDown()
    {
        OnClicked();
    }
}