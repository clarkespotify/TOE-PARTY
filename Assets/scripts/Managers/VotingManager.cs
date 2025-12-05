using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class VotingManager : NetworkBehaviour
{
    public static VotingManager Instance { get; private set; }

    [Header("Spawn Settings")]
    [Tooltip("Where guilty imposters spawn after being voted out")]
    public Transform guiltySpawnPoint;

    // Track votes using a dictionary-like structure
    private NetworkList<ulong> voterIds;
    private NetworkList<ulong> targetIds;

    // Voting phase active
    private NetworkVariable<bool> votingActive = new NetworkVariable<bool>(false);

    // Voting timer
    private NetworkVariable<float> votingTimeRemaining = new NetworkVariable<float>(30f);
    public float votingDuration = 30f;

    // Events
    public System.Action<ulong, bool> OnPlayerVotedOut; // clientId, wasImposter
    public System.Action OnVotingStarted;
    public System.Action OnVotingEnded;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        voterIds = new NetworkList<ulong>();
        targetIds = new NetworkList<ulong>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            voterIds.OnListChanged += OnVotesChanged;
            targetIds.OnListChanged += OnVotesChanged;
        }
    }

    private void Update()
    {
        if (IsServer && votingActive.Value)
        {
            votingTimeRemaining.Value -= Time.deltaTime;

            if (votingTimeRemaining.Value <= 0)
            {
                EndVoting();
            }
        }
    }

    // Call this to start voting (called from countdown script)
    public void StartVoting()
    {
        // Anyone can request voting to start
        StartVotingServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartVotingServerRpc()
    {
        if (!IsServer) return;

        voterIds.Clear();
        targetIds.Clear();
        votingActive.Value = true;
        votingTimeRemaining.Value = votingDuration;

        Debug.Log("🗳️ Voting phase started!");

        OnVotingStarted?.Invoke();
        NotifyVotingStartedClientRpc();
    }

    [ClientRpc]
    private void NotifyVotingStartedClientRpc()
    {
        if (!IsServer)
        {
            OnVotingStarted?.Invoke();
        }
    }

    // Called by clients to cast their vote
    public void CastVote(ulong targetClientId)
    {
        if (!votingActive.Value)
        {
            Debug.LogWarning("Cannot vote - voting is not active!");
            return;
        }

        CastVoteServerRpc(targetClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CastVoteServerRpc(ulong targetClientId, ServerRpcParams rpcParams = default)
    {
        ulong voterClientId = rpcParams.Receive.SenderClientId;

        // Prevent voting for yourself
        if (voterClientId == targetClientId)
        {
            Debug.LogWarning($"Player {voterClientId} tried to vote for themselves!");
            return;
        }

        // Check if player already voted, remove old vote
        for (int i = voterIds.Count - 1; i >= 0; i--)
        {
            if (voterIds[i] == voterClientId)
            {
                voterIds.RemoveAt(i);
                targetIds.RemoveAt(i);
                Debug.Log($"Player {voterClientId} changed their vote");
                break;
            }
        }

        // Add new vote
        voterIds.Add(voterClientId);
        targetIds.Add(targetClientId);

        Debug.Log($"📊 Player {voterClientId} voted for Player {targetClientId}. Total votes: {voterIds.Count}");
    }

    private void OnVotesChanged(NetworkListEvent<ulong> changeEvent)
    {
        // You can add real-time UI updates here if needed
    }

    private void EndVoting()
    {
        if (!votingActive.Value) return;

        votingActive.Value = false;

        Debug.Log("🗳️ Voting phase ended! Tallying votes...");

        // Tally votes
        Dictionary<ulong, int> voteCounts = new Dictionary<ulong, int>();

        for (int i = 0; i < voterIds.Count; i++)
        {
            ulong targetId = targetIds[i];
            if (!voteCounts.ContainsKey(targetId))
            {
                voteCounts[targetId] = 0;
            }
            voteCounts[targetId]++;
        }

        // Find player with most votes
        if (voteCounts.Count > 0)
        {
            var mostVoted = voteCounts.OrderByDescending(x => x.Value).First();
            ulong votedOutPlayer = mostVoted.Key;
            int voteCount = mostVoted.Value;

            Debug.Log($"📊 Player {votedOutPlayer} received the most votes ({voteCount})");

            // Check if they were the imposter
            bool wasImposter = ImposterGameManager.Instance.IsImposter(votedOutPlayer);

            Debug.Log($"🎭 Player {votedOutPlayer} was {(wasImposter ? "GUILTY (IMPOSTER)! 🎉" : "INNOCENT! 😢")}");

            // Notify everyone first
            OnPlayerVotedOut?.Invoke(votedOutPlayer, wasImposter);
            NotifyPlayerVotedOutClientRpc(votedOutPlayer, wasImposter);

            // Teleport guilty player after a short delay
            if (wasImposter && guiltySpawnPoint != null)
            {
                StartCoroutine(TeleportAfterDelay(votedOutPlayer, 1f));
            }
        }
        else
        {
            Debug.Log("⚠️ No votes were cast!");
        }

        OnVotingEnded?.Invoke();
        NotifyVotingEndedClientRpc();
    }

    [ClientRpc]
    private void NotifyPlayerVotedOutClientRpc(ulong votedOutClientId, bool wasImposter)
    {
        if (!IsServer)
        {
            OnPlayerVotedOut?.Invoke(votedOutClientId, wasImposter);
        }
    }

    [ClientRpc]
    private void NotifyVotingEndedClientRpc()
    {
        if (!IsServer)
        {
            OnVotingEnded?.Invoke();
        }
    }

    private IEnumerator TeleportAfterDelay(ulong clientId, float delay)
    {
        yield return new WaitForSeconds(delay);

        Debug.Log($"📍 Starting teleport for player {clientId}");

        if (guiltySpawnPoint != null)
        {
            // Tell the specific client to teleport themselves
            TeleportGuiltyPlayerClientRpc(clientId, guiltySpawnPoint.position, guiltySpawnPoint.rotation);
        }
    }

    [ClientRpc]
    private void TeleportGuiltyPlayerClientRpc(ulong targetClientId, Vector3 position, Quaternion rotation)
    {
        ulong myClientId = NetworkManager.Singleton.LocalClientId;

        Debug.Log($"📍 Client {myClientId} received teleport command for Player {targetClientId} to {position}");

        // Only the guilty player teleports themselves
        if (myClientId == targetClientId)
        {
            Debug.Log($"🎯 I am the guilty player! Teleporting to {position}");

            // Find my player object
            var myPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (myPlayer != null)
            {
                // Disable CharacterController
                CharacterController cc = myPlayer.GetComponent<CharacterController>();
                if (cc != null)
                {
                    cc.enabled = false;
                    Debug.Log("📍 Disabled CharacterController");
                }

                // Set position
                myPlayer.transform.position = position;
                myPlayer.transform.rotation = rotation;

                Debug.Log($"✅ Teleported to {myPlayer.transform.position}");

                // Re-enable CharacterController
                if (cc != null)
                {
                    cc.enabled = true;
                    Debug.Log("📍 Re-enabled CharacterController");
                }
            }
            else
            {
                Debug.LogError("❌ Could not find my player object!");
            }
        }
    }

    // Helper method to get current vote counts (for UI)
    public Dictionary<ulong, int> GetVoteCounts()
    {
        Dictionary<ulong, int> voteCounts = new Dictionary<ulong, int>();

        for (int i = 0; i < voterIds.Count; i++)
        {
            ulong targetId = targetIds[i];
            if (!voteCounts.ContainsKey(targetId))
            {
                voteCounts[targetId] = 0;
            }
            voteCounts[targetId]++;
        }

        return voteCounts;
    }

    public bool IsVotingActive() => votingActive.Value;
    public float GetTimeRemaining() => votingTimeRemaining.Value;

    // Get all players except the local player
    public List<ulong> GetVotablePlayers()
    {
        var players = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
        players.Remove(NetworkManager.Singleton.LocalClientId);
        return players;
    }
}