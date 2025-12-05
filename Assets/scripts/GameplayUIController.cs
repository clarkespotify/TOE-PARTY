using UnityEngine;

public class GameplayUIController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The HOST/JOIN menu panel to hide if already hosting/joining")]
    public GameObject hostJoinMenuPanel;

    void Awake()
    {
        // Hide by default
        if (hostJoinMenuPanel != null)
        {
            hostJoinMenuPanel.SetActive(false);
        }

        // Check IMMEDIATELY before scene is visible
        if (GameStateManager.Instance != null)
        {
            if (GameStateManager.Instance.isHosting)
            {
                Debug.Log("Already hosting - HOST/JOIN menu stays hidden");
                // Already hidden above
            }
            else if (GameStateManager.Instance.isJoining)
            {
                Debug.Log("Already joining - HOST/JOIN menu stays hidden");
                // Already hidden above
            }
            else
            {
                Debug.Log("Not hosting or joining - showing HOST/JOIN menu");
                ShowHostJoinMenu();
            }
        }
        else
        {
            Debug.LogWarning("GameStateManager not found! Showing menu by default.");
            ShowHostJoinMenu();
        }
    }

    void HideHostJoinMenu()
    {
        if (hostJoinMenuPanel != null)
        {
            hostJoinMenuPanel.SetActive(false);
        }
    }

    void ShowHostJoinMenu()
    {
        if (hostJoinMenuPanel != null)
        {
            hostJoinMenuPanel.SetActive(true);
        }
    }
}