using UnityEngine;
using Unity.Netcode;

public class NetworkManagerPersist : MonoBehaviour
{
    void Awake()
    {
        // Make NetworkManager persist between scenes
        DontDestroyOnLoad(gameObject);
        Debug.Log("NetworkManager set to DontDestroyOnLoad");
    }
}