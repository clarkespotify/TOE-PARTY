using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class SceneChanger : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("The name of the scene to load when this object is clicked")]
    public string sceneName;

    [Header("Debug")]
    public bool showDebugLogs = true;

    void Start()
    {
        if (GetComponent<Collider>() == null)
        {
            Debug.LogError("SceneChanger: No collider found on " + gameObject.name + "! Add a collider component.");
        }

        if (showDebugLogs)
        {
            Debug.Log("SceneChanger ready on " + gameObject.name + ". Target scene: " + sceneName);
        }
    }

    void Update()
    {
        // Check for mouse click (works with both old and new input systems)
        bool mouseClicked = false;

#if ENABLE_INPUT_SYSTEM
            mouseClicked = UnityEngine.InputSystem.Mouse.current != null && 
                          UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
#else
        mouseClicked = Input.GetMouseButtonDown(0);
#endif

        if (mouseClicked)
        {
            // Don't process click if over UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                if (showDebugLogs)
                {
                    Debug.Log("Click blocked by UI");
                }
                return;
            }

            // Cast ray from camera to mouse position
            Ray ray = Camera.main.ScreenPointToRay(GetMousePosition());
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // Check if we hit this object
                if (hit.collider.gameObject == gameObject)
                {
                    if (showDebugLogs)
                    {
                        Debug.Log("Clicked on " + gameObject.name);
                    }
                    LoadScene();
                }
            }
        }
    }

    Vector3 GetMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                return UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            }
            return Vector3.zero;
#else
        return Input.mousePosition;
#endif
    }

    public void LoadScene()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Scene name is not set on " + gameObject.name);
            return;
        }

        if (showDebugLogs)
        {
            Debug.Log("Attempting to load scene: " + sceneName);
        }

        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("Scene '" + sceneName + "' not found in Build Settings! Add it to File > Build Settings.");
        }
    }
}