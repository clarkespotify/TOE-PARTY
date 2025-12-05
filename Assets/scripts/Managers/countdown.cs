using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class countdown : MonoBehaviour
{
    float currentTime;
    public float round1Time = 5f; // 3 minutes for round 1
    [SerializeField] TextMeshProUGUI countdownText;

    [Header("UI Elements")]
    public GameObject ROUND1;
    public GameObject findimposter;
    public GameObject votingTime;

    [Header("Audio")]
    public AudioSource bgmusic;
    public AudioSource stoptimer;
    public AudioSource discussion;

    [Header("Camera Settings")]
    public Transform targetObject; // The object to look at (assign in Inspector)
    public float rotationSpeed = 2f; // How fast the camera rotates
    private Transform cameraTransform; // Will be found at runtime

    [Header("Voting System")]
    public VotingManager votingManager; // Assign in Inspector
    public float votingStartDelay = 3f; // Delay before voting UI appears

    private int currentRound = 1;
    private bool isRotatingCamera = false;

    void Start()
    {
        currentTime = round1Time;

        // Find the main camera at runtime
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogWarning("No main camera found!");
        }

        // Subscribe to voting events
        if (votingManager != null)
        {
            votingManager.OnPlayerVotedOut += OnPlayerVotedOut;
            votingManager.OnVotingEnded += OnVotingEnded;
        }
        else
        {
            Debug.LogError("⚠️ VotingManager not assigned in countdown script!");
        }
    }

    void Update()
    {
        // Only countdown if we're in round 1
        if (currentRound == 1)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0)
            {
                currentTime = 0;
                TransitionRound();
            }
        }

        // Smoothly rotate camera towards target during voting round
        if (isRotatingCamera && targetObject != null && cameraTransform != null)
        {
            Vector3 direction = targetObject.position - cameraTransform.position;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        DisplayTime(currentTime);
    }

    void TransitionRound()
    {
        if (currentRound == 1)
        {
            Debug.Log("🎮 Round 1 Complete - Starting Voting Time");

            // Hide round 1 UI
            ROUND1.SetActive(false);
            countdownText.enabled = false;
            findimposter.SetActive(false);

            // Show voting time indicator
            votingTime.SetActive(true);

            // Audio changes
            bgmusic.Stop();
            stoptimer.Play();

            currentRound = 2; // Move to round 2

            StartCoroutine(PlayEerie());

            // DISABLE CAMERA ON THE LOCAL PLAYER - NOT Camera.main!
            FirstPersonCamera[] allCameras = FindObjectsOfType<FirstPersonCamera>();
            foreach (FirstPersonCamera cam in allCameras)
            {
                if (cam.GetComponent<Unity.Netcode.NetworkBehaviour>().IsOwner)
                {
                    cam.enabled = false;
                    Debug.Log("✅ Disabled local player camera");
                    break;
                }
            }

            // Start rotating camera towards target
            isRotatingCamera = true;

            // Start voting after a delay
            StartCoroutine(StartVotingAfterDelay());
        }
    }

    IEnumerator PlayEerie()
    {
        yield return new WaitForSeconds(3);
        discussion.Play();
    }

    IEnumerator StartVotingAfterDelay()
    {
        // Wait for the delay (gives time for audio/camera transition)
        yield return new WaitForSeconds(votingStartDelay);

        // Start the voting phase
        if (votingManager != null)
        {
            Debug.Log("🗳️ Starting voting phase...");
            votingManager.StartVoting();
        }
        else
        {
            Debug.LogError("❌ Cannot start voting - VotingManager is null!");
        }
    }

    void OnPlayerVotedOut(ulong clientId, bool wasImposter)
    {
        Debug.Log($"🎭 Voting complete! Player {clientId + 1} was voted out. Imposter: {wasImposter}");

        // You can add additional logic here, like:
        // - Play a sound effect
        // - Show an animation
        // - Update score
    }

    void OnVotingEnded()
    {
        Debug.Log("🏁 Voting phase ended");

        // Re-enable camera after voting (if desired)
        StartCoroutine(ReEnableCameraAfterResults());
    }

    IEnumerator ReEnableCameraAfterResults()
    {
        // Wait for results to be shown (5 seconds as per VotingUI)
        yield return new WaitForSeconds(6f);

        // Stop rotating camera
        isRotatingCamera = false;

        // RE-ENABLE CAMERA ON THE LOCAL PLAYER
        FirstPersonCamera[] allCameras = FindObjectsOfType<FirstPersonCamera>();
        foreach (FirstPersonCamera cam in allCameras)
        {
            if (cam.GetComponent<Unity.Netcode.NetworkBehaviour>().IsOwner)
            {
                cam.enabled = true;
                Debug.Log("✅ Re-enabled local player camera");
                break;
            }
        }

        // Optional: Hide voting time UI
        if (votingTime != null)
        {
            votingTime.SetActive(false);
        }
    }


    void DisplayTime(float timeToDisplay)
    {
        timeToDisplay = Mathf.Max(0, timeToDisplay);
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        countdownText.text = string.Format("{0}:{1:00}", minutes, seconds);
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (votingManager != null)
        {
            votingManager.OnPlayerVotedOut -= OnPlayerVotedOut;
            votingManager.OnVotingEnded -= OnVotingEnded;
        }
    }

    // Optional: Method to restart the game
    public void StartNewRound()
    {
        currentRound = 1;
        currentTime = round1Time;
        ROUND1.SetActive(true);
        countdownText.enabled = true;
        findimposter.SetActive(true);
        votingTime.SetActive(false);

        // Restart game manager
        if (ImposterGameManager.Instance != null)
        {
            ImposterGameManager.Instance.RestartGameServerRpc();
        }
    }
}