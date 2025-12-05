using UnityEngine;
using Unity.Netcode;

public class PlayerColorApplier : NetworkBehaviour
{
    [Header("References")]
    [Tooltip("The renderer of the player model to color")]
    public Renderer playerRenderer;

    [Tooltip("Material index to change (usually 0)")]
    public int materialIndex = 0;

    // ✅ NETWORK VARIABLE to sync color across all clients
    private NetworkVariable<float> playerHue = new NetworkVariable<float>(
        0f, // Default red
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    void Start()
    {
        if (playerRenderer == null)
        {
            playerRenderer = GetComponentInChildren<Renderer>();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // If this is the local player (owner), load their saved color
        if (IsOwner)
        {
            float savedHue = PlayerPrefs.GetFloat("PlayerHue", 0f);
            Debug.Log($"✅ Loading saved hue for local player: {savedHue}");

            // Set the network variable (will sync to all clients)
            playerHue.Value = savedHue;
        }

        // Subscribe to changes (for when other players' colors sync)
        playerHue.OnValueChanged += OnHueChanged;

        // Apply the initial color
        ApplyColor(playerHue.Value);
    }

    private void OnHueChanged(float previousValue, float newValue)
    {
        Debug.Log($"Player hue changed from {previousValue} to {newValue}");
        ApplyColor(newValue);
    }

    private void ApplyColor(float hue)
    {
        if (playerRenderer == null)
        {
            Debug.LogWarning("Player renderer not assigned!");
            return;
        }

        // Get the material (creates instance automatically)
        Material[] materials = playerRenderer.materials;

        if (materialIndex < materials.Length)
        {
            // Convert hue to color
            Color newColor = Color.HSVToRGB(hue, 1f, 1f); // Full saturation and value

            // Apply color
            materials[materialIndex].color = newColor;
            playerRenderer.materials = materials;

            Debug.Log($"✅ Applied color to player: Hue={hue}, Color={newColor}");
        }
        else
        {
            Debug.LogError($"Material index {materialIndex} out of range!");
        }
    }

    public override void OnNetworkDespawn()
    {
        playerHue.OnValueChanged -= OnHueChanged;
        base.OnNetworkDespawn();
    }
}