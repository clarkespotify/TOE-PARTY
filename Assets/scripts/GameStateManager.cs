using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("Game State")]
    public bool isHosting = false;
    public bool isJoining = false;

    void Awake()
    {
        // Singleton pattern - only one instance exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist between scenes
            Debug.Log("GameStateManager created and persisting between scenes");
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate
        }
    }

    public void SetHosting()
    {
        isHosting = true;
        isJoining = false;
        Debug.Log("Game state set to: HOSTING");
    }

    public void SetJoining()
    {
        isHosting = false;
        isJoining = true;
        Debug.Log("Game state set to: JOINING");
    }

    public void ResetState()
    {
        isHosting = false;
        isJoining = false;
        Debug.Log("Game state reset");
    }
}