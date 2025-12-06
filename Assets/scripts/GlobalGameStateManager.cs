using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameMode
{
    Bat, Impostor
}



public class GlobalGameStateManager : MonoBehaviour
{
    public string batSceneName;
    public string ImpostorSceneName;
    public GameObject bat;
    public GameMode gameMode;
    private static GlobalGameStateManager _instance;
    private static bool _isShuttingDown = false;
    private static readonly object _lock = new object();
    [SerializeField] private GameEvent eventChannel;
    public static GlobalGameStateManager Instance
    {
        get
        {
            if (_isShuttingDown) return null;

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GlobalGameStateManager>();
                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject(nameof(GlobalGameStateManager));
                        _instance = singletonObject.AddComponent<GlobalGameStateManager>();
                    }

                    DontDestroyOnLoad(_instance.gameObject);
                }

                return _instance;
            }
        }
        private set
        {
            lock (_lock)
            {
                _instance = value;
            }
        }
    }

    private void Awake()
    {
        // Enforce singleton
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnApplicationQuit()
    {
        _isShuttingDown = true;
    }

    private void OnDestroy()
    {
        _isShuttingDown = true;
    }
    private void OnEnable() => eventChannel.OnPlayerSpawn += HandlePlayerSpawn;
    private void OnDisable() => eventChannel.OnPlayerSpawn -= HandlePlayerSpawn;
    void HandlePlayerSpawn(GameObject player, GameMode mode)
    {
        Debug.Log("Player spawned!!!");
        gameMode = mode;
        if (gameMode == GameMode.Bat)
        {
            GameObject playerBat = Instantiate(bat, player.transform.Find("FirstPersonCamera"));
        }
    }
}
