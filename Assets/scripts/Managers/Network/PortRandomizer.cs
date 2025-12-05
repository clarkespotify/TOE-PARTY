using UnityEngine;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;

public class PortRandomizer : MonoBehaviour
{
    void Awake()
    {
        // Set a random port IMMEDIATELY when game starts
        var transport = NetworkManager.Singleton?.GetComponent<UnityTransport>();
        if (transport != null)
        {
            ushort randomPort = (ushort)Random.Range(7777, 9999);
            transport.ConnectionData.Port = randomPort;
            Debug.Log($"🔧 Set random port on startup: {randomPort}");
        }
    }
}