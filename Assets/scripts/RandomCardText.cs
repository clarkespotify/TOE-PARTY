using UnityEngine;
using TMPro;
using Unity.Netcode;

public class RandomCardText : MonoBehaviour
{
    [Header("Player Assignment")]
    [Tooltip("Which player does this card belong to? (0 = Host/Player 0, 1 = Player 1, 2 = Player 2, etc.)")]
    public int playerNumber = 0;

    private TMP_Text textComponent;

    void Start()
    {
        Debug.Log($"🎴 Card '{gameObject.name}' initialized for Player {playerNumber}");

        textComponent = GetComponent<TMP_Text>();

        if (textComponent == null)
        {
            Debug.LogError($"❌ No TMP_Text component found on {gameObject.name}!");
            return;
        }

        // Set initial waiting text
        textComponent.text = "Waiting...";
    }

    /// <summary>
    /// Called by GameManager to set this card's word
    /// </summary>
    public void SetWord(string word, ulong myClientId)
    {
        // Check if this card belongs to this client
        if (playerNumber != (int)myClientId)
        {
            Debug.Log($"⏭️ Skipping card '{gameObject.name}' (Player {playerNumber}) - I am Client {myClientId}");
            return;
        }

        // This card belongs to me! Update it
        if (textComponent == null)
        {
            textComponent = GetComponent<TMP_Text>();
        }

        if (textComponent != null)
        {
            Debug.Log($"✅ Updating MY card '{gameObject.name}' (Player {playerNumber}) to: '{word}'");
            Debug.Log($"   Before: '{textComponent.text}'");

            textComponent.text = word;

            Debug.Log($"   After: '{textComponent.text}'");
            Debug.Log($"   Component enabled: {textComponent.enabled}");
            Debug.Log($"   GameObject active: {gameObject.activeInHierarchy}");

            // Force update
            textComponent.ForceMeshUpdate();
        }
        else
        {
            Debug.LogError($"❌ TMP_Text component is null on {gameObject.name}!");
        }
    }
}