using UnityEngine;

/// <summary>
/// Manages the visibility of the start menu UI.
/// Attach this to your start menu Canvas or panel in StartScene.
/// </summary>
public class StartMenuManager : MonoBehaviour
{
    public static StartMenuManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("The main menu panel/canvas to show/hide")]
    public GameObject menuPanel;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Show menu by default
        ShowStartMenu();
    }

    public void ShowStartMenu()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
            Debug.Log("✅ Start menu shown");
        }
        else
        {
            Debug.LogWarning("⚠️ Menu panel not assigned to StartMenuManager!");
        }
    }

    public void HideStartMenu()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
            Debug.Log("✅ Start menu hidden");
        }
        else
        {
            Debug.LogWarning("⚠️ Menu panel not assigned to StartMenuManager!");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}