using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// Automatically converts scene objects to spawned network prefabs.
/// Just tag your pickupable objects and this handles the rest!
/// </summary>
public class AutoSpawnSceneObjects : NetworkBehaviour
{
    [Header("Settings")]
    [Tooltip("Tag of objects to auto-spawn (e.g., 'Can', 'Card')")]
    public string objectTag = "Can";

    [Tooltip("The prefab to spawn (must match your scene objects)")]
    public GameObject pickupablePrefab;

    [Tooltip("Should the spawned objects keep their original tag?")]
    public bool preserveTag = true;

    private List<SpawnInfo> objectsToSpawn = new List<SpawnInfo>();

    private struct SpawnInfo
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public string name;
        public string tag;
        public int layer;
    }

    void Start()
    {
        Debug.Log("AutoSpawnSceneObjects Start() called!");
        Debug.Log($"IsServer: {IsServer}");

        if (!IsServer) return;

        Debug.Log("We are the server, continuing...");

        if (!IsServer) return; // Only server spawns

        // Find all objects with the specified tag
        GameObject[] sceneObjects = GameObject.FindGameObjectsWithTag(objectTag);

        Debug.Log($"Found {sceneObjects.Length} objects with tag '{objectTag}' to convert");

        // Store their transform data and destroy them
        foreach (GameObject obj in sceneObjects)
        {
            SpawnInfo info = new SpawnInfo
            {
                position = obj.transform.position,
                rotation = obj.transform.rotation,
                scale = obj.transform.localScale,
                name = obj.name,
                tag = obj.tag,
                layer = obj.layer
            };

            objectsToSpawn.Add(info);

            // Destroy the scene object
            Destroy(obj);
        }

        // Spawn network prefabs in their place
        foreach (SpawnInfo info in objectsToSpawn)
        {
            GameObject newObj = Instantiate(pickupablePrefab, info.position, info.rotation);
            newObj.transform.localScale = info.scale;
            newObj.name = info.name;

            // Preserve the original tag and layer
            if (preserveTag)
            {
                newObj.tag = info.tag;
            }
            newObj.layer = info.layer;

            NetworkObject netObj = newObj.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
                Debug.Log($"✅ Spawned networked version of '{info.name}'");
            }
            else
            {
                Debug.LogError($"❌ Prefab is missing NetworkObject component!");
            }
        }

        Debug.Log($"✅ Converted {objectsToSpawn.Count} scene objects to network spawned objects");
    }
}