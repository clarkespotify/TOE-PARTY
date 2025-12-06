using UnityEngine;
using UnityEngine.SceneManagement;

public class batspawnmanager : MonoBehaviour
{

    public GameObject bat;
    public Transform spawnposition;

    void Start()
    {
        bat.transform.parent = spawnposition;
        bat.SetActive(false);
    }

    void OnSceneLoaded(Scene scne, LoadSceneMode mode)
    {
        CheckScene();
    }

    void CheckScene()
    {
        if (SceneManager.GetActiveScene().name == "Runandbatscene" && bat.activeSelf == null)
        {
            bat.SetActive(true);
        }
        else
        {
            bat.SetActive(false);
        }
    }
}
