using UnityEngine;
using Unity.Netcode;

public class PlayerMovementManager : NetworkBehaviour
{
    private CharacterController characterController;
    private FirstPersonMovement movementScript;
    private bool isGuiltyPlayer = false; // Track if this player is guilty

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // LOG IMMEDIATELY ON SPAWN
        Debug.Log($"🚨 [SPAWN] Player spawned at: {transform.position} (Client {NetworkManager.Singleton.LocalClientId})");

        // Only run for local player
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        // Get components immediately
        characterController = GetComponent<CharacterController>();
        movementScript = GetComponent<FirstPersonMovement>();

        // Subscribe to voting events
        if (VotingManager.Instance != null)
        {
            VotingManager.Instance.OnPlayerVotedOut += OnPlayerVotedOut;
        }

        // LOCK MOVEMENT IMMEDIATELY - NO DELAY!
        LockMovement();

        Debug.Log($"✅ PlayerMovementManager: Locked immediately at position {transform.position}");
    }

    void LockMovement()
    {
        if (movementScript != null)
        {
            // Disable the movement script to prevent player input
            movementScript.enabled = false;
            Debug.Log("✅ Movement LOCKED - Player is now stationary!");
        }
    }

    void OnPlayerVotedOut(ulong votedOutClientId, bool wasImposter)
    {
        // Check if this is the local player who was voted out and was guilty
        ulong myClientId = NetworkManager.Singleton.LocalClientId;

        if (votedOutClientId == myClientId && wasImposter)
        {
            Debug.Log("🎭 I was voted out and I was GUILTY! Unlocking movement...");
            isGuiltyPlayer = true;
            UnlockMovement();
        }
    }

    void UnlockMovement()
    {
        if (movementScript != null)
        {
            movementScript.enabled = true;
            Debug.Log("✅ Movement UNLOCKED - Guilty player can now move!");
        }
    }

    // Alternative method using trigger zones
    void OnTriggerEnter(Collider other)
    {
        // If you want to use a trigger zone instead
        if (other.CompareTag("GuiltyZone") && isGuiltyPlayer)
        {
            Debug.Log("Guilty player entered the guilty zone!");
            UnlockMovement();
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (VotingManager.Instance != null)
        {
            VotingManager.Instance.OnPlayerVotedOut -= OnPlayerVotedOut;
        }
    }
}