using UnityEngine;
using System.Collections;

public class HostButtonAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("The Animator component (usually on your camera or object)")]
    public Animator targetAnimator;

    [Tooltip("Name of your animation state in the Animator Controller")]
    public string animationStateName = "movetotable";

    [Tooltip("How long does the animation take? (in seconds)")]
    public float animationDuration = 2f;

    [Header("Button References")]
    [Tooltip("The host/forward button GameObject")]
    public GameObject hostButton;

    [Tooltip("The back button GameObject")]
    public GameObject backButton;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private bool hasPlayedOnce = false;

    void Awake()
    {
        // CRITICAL: Stop the animator immediately on awake
        if (targetAnimator != null)
        {
            targetAnimator.enabled = false;
            if (showDebugLogs) Debug.Log("✅ Animator DISABLED in Awake to prevent auto-play");
        }
    }

    void Start()
    {
        // Verify animator
        if (targetAnimator == null)
        {
            Debug.LogError("❌ No Animator assigned! Drag your camera's Animator into the Inspector.");
            return;
        }

        // Setup button visibility
        if (hostButton != null)
        {
            hostButton.SetActive(true);
            if (showDebugLogs) Debug.Log($"✅ Host button '{hostButton.name}' is VISIBLE");
        }
        else
        {
            Debug.LogError("❌ Host Button not assigned in Inspector!");
        }

        if (backButton != null)
        {
            backButton.SetActive(false);
            if (showDebugLogs) Debug.Log($"✅ Back button '{backButton.name}' is HIDDEN");
        }
        else
        {
            Debug.LogError("❌ Back Button not assigned in Inspector!");
        }
    }

    // Call this from your HOST button's OnClick event
    public void PlayForward()
    {
        if (showDebugLogs) Debug.Log("🔵 PlayForward() called - Host button clicked!");

        if (targetAnimator == null)
        {
            Debug.LogError("❌ No Animator assigned!");
            return;
        }

        // Switch buttons FIRST
        if (hostButton != null)
        {
            hostButton.SetActive(false);
            if (showDebugLogs) Debug.Log($"✅ Host button hidden. Active: {hostButton.activeSelf}");
        }

        if (backButton != null)
        {
            backButton.SetActive(true);
            if (showDebugLogs) Debug.Log($"✅ Back button shown. Active: {backButton.activeSelf}");

            // Double-check parent hierarchy
            Transform parent = backButton.transform.parent;
            if (parent != null && !parent.gameObject.activeInHierarchy)
            {
                Debug.LogError($"❌ Back button's parent '{parent.name}' is DISABLED! Enable it!");
            }
        }

        // Enable animator and play
        targetAnimator.enabled = true;
        targetAnimator.speed = 1f;
        targetAnimator.Play(animationStateName, 0, 0f);

        hasPlayedOnce = true;

        if (showDebugLogs) Debug.Log($"✅ Animation '{animationStateName}' playing FORWARD");
    }

    // Call this from your BACK button's OnClick event
    public void PlayReverse()
    {
        if (showDebugLogs) Debug.Log("🔵 PlayReverse() called - Back button clicked!");

        if (targetAnimator == null)
        {
            Debug.LogError("❌ No Animator assigned!");
            return;
        }

        // Switch buttons FIRST
        if (backButton != null)
        {
            backButton.SetActive(false);
            if (showDebugLogs) Debug.Log("✅ Back button hidden");
        }

        if (hostButton != null)
        {
            hostButton.SetActive(true);
            if (showDebugLogs) Debug.Log("✅ Host button shown");
        }

        // Play animation in reverse
        StartCoroutine(ReverseAnimation());
    }

    private IEnumerator ReverseAnimation()
    {
        // Enable animator
        targetAnimator.enabled = true;

        // Jump to end of animation
        targetAnimator.speed = 1f;
        targetAnimator.Play(animationStateName, 0, 1f);

        // Wait for state to activate
        yield return null;
        yield return null;

        // Now reverse
        targetAnimator.speed = -1f;

        if (showDebugLogs) Debug.Log($"✅ Animation '{animationStateName}' playing REVERSE");

        // Wait for animation to complete
        yield return new WaitForSeconds(animationDuration);

        // Stop animator to prevent looping
        targetAnimator.enabled = false;

        if (showDebugLogs) Debug.Log("✅ Reverse animation complete, animator disabled");
    }
}