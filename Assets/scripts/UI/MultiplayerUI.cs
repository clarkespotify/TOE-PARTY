using UnityEngine;
using Unity.Netcode;

public class MultiplayerUI : MonoBehaviour
{
    private bool shouldShowMenu = true;

    void Start()
    {
        // Check if we're already hosting/joining from the menu scene
        if (GameStateManager.Instance != null)
        {
            Debug.Log($"GameStateManager found! isHosting: {GameStateManager.Instance.isHosting}, isJoining: {GameStateManager.Instance.isJoining}");
            if (GameStateManager.Instance.isHosting || GameStateManager.Instance.isJoining)
            {
                shouldShowMenu = false;
                Debug.Log("Already hosting/joining - hiding multiplayer UI");
            }
            else
            {
                Debug.Log("Not hosting/joining - showing multiplayer UI");
            }
        }
        else
        {
            Debug.LogError("GameStateManager.Instance is NULL! Make sure GameStateManager exists in the first scene.");
        }
    }

    void OnGUI()
    {
        // Don't show menu if we're already connected from the previous scene
        if (!shouldShowMenu && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
        {
            // Already connected - show status only
            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            string status = NetworkManager.Singleton.IsHost ? "HOST" : "CLIENT";
            GUILayout.Label("Connected as: " + status, GUILayout.Height(30));
            GUILayout.Label("Players: " + NetworkManager.Singleton.ConnectedClients.Count, GUILayout.Height(30));
            if (GUILayout.Button("DISCONNECT", GUILayout.Height(50)))
            {
                NetworkManager.Singleton.Shutdown();
                shouldShowMenu = true; // Show menu again after disconnect
                if (GameStateManager.Instance != null)
                {
                    GameStateManager.Instance.ResetState();
                }
                Debug.Log("Disconnected");
            }
            GUILayout.EndArea();
            return;
        }

        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            // Not connected yet - show connection buttons only if shouldShowMenu is true
            if (shouldShowMenu)
            {
                GUILayout.Label("Multiplayer Menu", GUILayout.Height(30));
                if (GUILayout.Button("HOST (Start Game)", GUILayout.Height(50)))
                {
                    NetworkManager.Singleton.StartHost();
                    if (GameStateManager.Instance != null)
                    {
                        GameStateManager.Instance.SetHosting();
                    }
                    Debug.Log("Started as Host");
                }
                if (GUILayout.Button("JOIN (Connect to Friend)", GUILayout.Height(50)))
                {
                    NetworkManager.Singleton.StartClient();
                    if (GameStateManager.Instance != null)
                    {
                        GameStateManager.Instance.SetJoining();
                    }
                    Debug.Log("Started as Client");
                }
            }
        }
        else
        {
            // Connected - show status
            string status = NetworkManager.Singleton.IsHost ? "HOST" : "CLIENT";
            GUILayout.Label("Connected as: " + status, GUILayout.Height(30));
            GUILayout.Label("Players: " + NetworkManager.Singleton.ConnectedClients.Count, GUILayout.Height(30));
            if (GUILayout.Button("DISCONNECT", GUILayout.Height(50)))
            {
                NetworkManager.Singleton.Shutdown();
                shouldShowMenu = true; // Show menu again after disconnect
                if (GameStateManager.Instance != null)
                {
                    GameStateManager.Instance.ResetState();
                }
                Debug.Log("Disconnected");
            }
        }
        GUILayout.EndArea();
    }
}