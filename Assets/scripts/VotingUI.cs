using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Netcode;

public class VotingUI : MonoBehaviour
{
    [Header("References")]
    public VotingManager votingManager;
    public GameObject votingPanel;
    public Transform playerButtonContainer;
    public GameObject playerButtonPrefab;
    public TextMeshProUGUI timerText;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;

    private Dictionary<ulong, GameObject> playerButtons = new Dictionary<ulong, GameObject>();
    private ulong? myVote = null;

    private void Start()
    {
        votingPanel.SetActive(false);
        resultPanel.SetActive(false);

        // Subscribe to events
        if (votingManager != null)
        {
            votingManager.OnVotingStarted += OnVotingStarted;
            votingManager.OnVotingEnded += OnVotingEnded;
            votingManager.OnPlayerVotedOut += OnPlayerVotedOut;
        }
        else
        {
            Debug.LogError("⚠️ VotingManager not assigned in VotingUI!");
        }
    }

    private void Update()
    {
        if (votingManager != null && votingManager.IsVotingActive())
        {
            float timeLeft = votingManager.GetTimeRemaining();
            timerText.text = $"TIME: {Mathf.CeilToInt(timeLeft)}";
        }
    }

    private void OnVotingStarted()
    {
        Debug.Log("🎨 UI: Voting started, showing panel");

        votingPanel.SetActive(true);
        resultPanel.SetActive(false);
        myVote = null;

        // DISABLE CAMERA MOVEMENT
        FirstPersonCamera playerCam = FindObjectOfType<FirstPersonCamera>();
        if (playerCam != null)
        {
            playerCam.enabled = false;
            Debug.Log("🎨 Disabled FirstPersonCamera for voting");
        }

        // UNLOCK CURSOR
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Clear old buttons
        foreach (var btnObj in playerButtons.Values)
        {
            Destroy(btnObj);
        }
        playerButtons.Clear();

        // Get all players except yourself
        var votablePlayers = votingManager.GetVotablePlayers();

        Debug.Log($"🎨 UI: Creating buttons for {votablePlayers.Count} players");

        foreach (var clientId in votablePlayers)
        {
            CreatePlayerButton(clientId);
        }
    }

    private void CreatePlayerButton(ulong clientId)
    {
        GameObject btnObj = Instantiate(playerButtonPrefab, playerButtonContainer);
        Button btn = btnObj.GetComponent<Button>();
        TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

        // Display as "Player X" where X is the client ID + 1 for user-friendly numbering
        btnText.text = $"Player {clientId + 1}";

        // Store reference
        playerButtons[clientId] = btnObj;

        // Add click listener
        ulong targetId = clientId;
        btn.onClick.AddListener(() => VoteForPlayer(targetId, btnObj));

        Debug.Log($"🎨 UI: Created button for Player {clientId + 1}");
    }

    private void VoteForPlayer(ulong targetClientId, GameObject buttonObj)
    {
        myVote = targetClientId;
        votingManager.CastVote(targetClientId);

        Debug.Log($"✅ Voted for Player {targetClientId + 1}");

        // Visual feedback - highlight selected button, unhighlight others
        foreach (var btnObj in playerButtons.Values)
        {
            Image btnImage = btnObj.GetComponent<Image>();
            if (btnImage != null)
            {
                btnImage.color = Color.white;
            }
        }

        Image selectedImage = buttonObj.GetComponent<Image>();
        if (selectedImage != null)
        {
            selectedImage.color = Color.green;
        }
    }

    private void OnPlayerVotedOut(ulong votedOutClientId, bool wasImposter)
    {
        Debug.Log($"🎨 UI: Showing results - PLAYER {votedOutClientId + 1} WAS {(wasImposter ? "GUILTY" : "INNOCENT")}");

        resultPanel.SetActive(true);

        if (wasImposter)
        {
            resultText.text = $"PLAYER {votedOutClientId + 1} WAS GUILTY";
            resultText.color = Color.white;
        }
        else
        {
            resultText.text = $"PLAYER {votedOutClientId + 1} WAS INNOCENT";
            resultText.color = Color.white;
        }
    }

    private void OnVotingEnded()
    {
        Debug.Log("🎨 UI: Voting ended");

        // RE-ENABLE CAMERA MOVEMENT
        FirstPersonCamera playerCam = FindObjectOfType<FirstPersonCamera>();
        if (playerCam != null)
        {
            playerCam.enabled = true;
            Debug.Log("🎨 Re-enabled FirstPersonCamera after voting");
        }

        // LOCK CURSOR
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Disable all buttons
        foreach (var btnObj in playerButtons.Values)
        {
            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = false;
            }
        }

        // Hide panels after a few seconds
        Invoke(nameof(HidePanels), 5f);
    }

    private void HidePanels()
    {
        votingPanel.SetActive(false);
        resultPanel.SetActive(false);

        Debug.Log("🎨 UI: Panels hidden");
    }

    private void OnDestroy()
    {
        if (votingManager != null)
        {
            votingManager.OnVotingStarted -= OnVotingStarted;
            votingManager.OnVotingEnded -= OnVotingEnded;
            votingManager.OnPlayerVotedOut -= OnPlayerVotedOut;
        }
    }
}