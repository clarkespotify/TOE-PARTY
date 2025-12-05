using UnityEngine;

public class HostAnimationController : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("The animator for the button's goup animation")]
    public Animator buttonAnimator;

    [Tooltip("Name of the button animation state")]
    public string buttonAnimationName = "goup";

    [Tooltip("The animator for the camera animation (movetotable)")]
    public Animator cameraAnimator;

    [Tooltip("Name of the camera animation state")]
    public string cameraAnimationName = "movetotable";

    [Tooltip("How long the animation takes (in seconds)")]
    public float animationDuration = 2f;

    [Header("Button References")]
    [Tooltip("The host button GameObject")]
    public GameObject hostButton;

    [Tooltip("The back button GameObject")]
    public GameObject backButton;

    [Tooltip("The Joining UI")]
    public GameObject joining;

    [Tooltip("The Quitting UI")]
    public GameObject Quitting;

    [Tooltip("The card blocker")]
    public GameObject cardblock;

    // Store original transform for camera
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private Vector3 originalCameraScale;

    // Store original transform for button
    private Vector3 originalButtonPosition;
    private Quaternion originalButtonRotation;
    private Vector3 originalButtonScale;

    private bool isAnimating = false;

    void Awake()
    {
        // Disable animators IMMEDIATELY in Awake (before Start)
        if (buttonAnimator != null)
        {
            buttonAnimator.enabled = false;
        }

        if (cameraAnimator != null)
        {
            cameraAnimator.enabled = false;
        }

        Debug.Log("🔵 Animators disabled in Awake");
    }

    void Start()
    {
        // Setup button animator
        if (buttonAnimator != null)
        {
            Transform buttonTransform = buttonAnimator.transform;
            originalButtonPosition = buttonTransform.localPosition;
            originalButtonRotation = buttonTransform.localRotation;
            originalButtonScale = buttonTransform.localScale;
            buttonAnimator.enabled = false;
            Debug.Log($"Button animation disabled on start.");
        }
        else
        {
            Debug.LogWarning("Button Animator not assigned!");
        }

        // Setup camera animator
        if (cameraAnimator != null)
        {
            Transform cameraTransform = cameraAnimator.transform;
            originalCameraPosition = cameraTransform.localPosition;
            originalCameraRotation = cameraTransform.localRotation;
            originalCameraScale = cameraTransform.localScale;
            cameraAnimator.enabled = false;
            Debug.Log($"Camera animation disabled on start.");
        }
        else
        {
            Debug.LogError("Camera Animator not assigned!");
        }

        // Setup button visibility
        if (hostButton != null)
        {
            hostButton.SetActive(true);
            Debug.Log("✅ Host button visible");
        }
        else
        {
            Debug.LogError("❌ Host Button not assigned in Inspector!");
        }

        if (backButton != null)
        {
            backButton.SetActive(false);
            Debug.Log("✅ Back button hidden");
        }
        else
        {
            Debug.LogError("❌ Back Button not assigned in Inspector!");
        }
    }

    // Call this when HOST button is clicked
    public void PlayAnimationForward()
    {
        // Hide host button, show back button
        if (hostButton != null)
        {
            hostButton.SetActive(false);
            Debug.Log("🔵 Host button hidden");
            cardblock.SetActive(false);
        }

        if (backButton != null)
        {
            backButton.SetActive(true);
            Debug.Log("🔵 Back button shown");
        }

        if (Quitting != null)
        {
            Quitting.SetActive(false);
        }

        // Stop any coroutines that might be running
        StopAllCoroutines();

        // Play BUTTON animation (goup)
        if (buttonAnimator != null)
        {
            buttonAnimator.enabled = true;
            buttonAnimator.speed = 1;
            buttonAnimator.Rebind();
            buttonAnimator.Update(0);
            buttonAnimator.Play(buttonAnimationName, 0, 0f);
            Debug.Log($"✅ Playing button animation forward: {buttonAnimationName}");
        }

        // Play CAMERA animation (movetotable)
        if (cameraAnimator != null)
        {
            cameraAnimator.enabled = true;
            cameraAnimator.speed = 1;
            cameraAnimator.Rebind();
            cameraAnimator.Update(0);
            cameraAnimator.Play(cameraAnimationName, 0, 0f);
            Debug.Log($"✅ Playing camera animation forward: {cameraAnimationName}");
        }

        isAnimating = true;

        joining.SetActive(false);
    }

    // Call this when BACK button is clicked
    public void PlayAnimationBackward()
    {
        // Hide back button, show host button
        if (backButton != null)
        {
            backButton.SetActive(false);
            Debug.Log("🔵 Back button hidden");
            cardblock.SetActive(true);
        }

        if (hostButton != null)
        {
            hostButton.SetActive(true);
            Debug.Log("🔵 Host button shown");
        }

        if (Quitting != null)
        {
            Quitting.SetActive(true);
        }

        // Play BUTTON animation backwards (goup)
        if (buttonAnimator != null)
        {
            buttonAnimator.enabled = true;
            buttonAnimator.speed = -1;
            buttonAnimator.Play(buttonAnimationName, 0, 1f);
            Debug.Log($"✅ Playing button animation backward: {buttonAnimationName}");
        }

        // Play CAMERA animation backwards (movetotable)
        if (cameraAnimator != null)
        {
            cameraAnimator.enabled = true;
            cameraAnimator.speed = -1;
            cameraAnimator.Play(cameraAnimationName, 0, 1f);
            Debug.Log($"✅ Playing camera animation backward: {cameraAnimationName}");
        }

        // If backwards doesn't work, use the coroutine fallback
        StartCoroutine(ReverseAnimationFallback());

        joining.SetActive(true);
    }

    private System.Collections.IEnumerator ReverseAnimationFallback()
    {
        // Wait a frame to see if the negative speed works
        yield return new WaitForSeconds(0.1f);

        bool buttonNeedsManual = false;
        bool cameraNeedsManual = false;

        if (buttonAnimator != null)
        {
            AnimatorStateInfo buttonState = buttonAnimator.GetCurrentAnimatorStateInfo(0);
            if (buttonState.normalizedTime >= 0.95f)
            {
                buttonNeedsManual = true;
            }
        }

        if (cameraAnimator != null)
        {
            AnimatorStateInfo cameraState = cameraAnimator.GetCurrentAnimatorStateInfo(0);
            if (cameraState.normalizedTime >= 0.95f)
            {
                cameraNeedsManual = true;
            }
        }

        if (buttonNeedsManual || cameraNeedsManual)
        {
            Debug.Log("Using manual reverse animation");
            yield return StartCoroutine(ManualReverseAnimation(buttonNeedsManual, cameraNeedsManual));
        }

        isAnimating = false;
    }

    private System.Collections.IEnumerator ManualReverseAnimation(bool animateButton, bool animateCamera)
    {
        // Setup button animation
        Transform buttonTransform = null;
        Vector3 buttonStartPosition = Vector3.zero;
        Quaternion buttonStartRotation = Quaternion.identity;
        Vector3 buttonStartScale = Vector3.one;

        if (animateButton && buttonAnimator != null)
        {
            buttonAnimator.enabled = false;
            buttonTransform = buttonAnimator.transform;
            buttonStartPosition = buttonTransform.localPosition;
            buttonStartRotation = buttonTransform.localRotation;
            buttonStartScale = buttonTransform.localScale;
        }

        // Setup camera animation
        Transform cameraTransform = null;
        Vector3 cameraStartPosition = Vector3.zero;
        Quaternion cameraStartRotation = Quaternion.identity;
        Vector3 cameraStartScale = Vector3.one;

        if (animateCamera && cameraAnimator != null)
        {
            cameraAnimator.enabled = false;
            cameraTransform = cameraAnimator.transform;
            cameraStartPosition = cameraTransform.localPosition;
            cameraStartRotation = cameraTransform.localRotation;
            cameraStartScale = cameraTransform.localScale;
        }

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // Smooth easing
            t = t * t * (3f - 2f * t);

            // Animate button
            if (animateButton && buttonTransform != null)
            {
                buttonTransform.localPosition = Vector3.Lerp(buttonStartPosition, originalButtonPosition, t);
                buttonTransform.localRotation = Quaternion.Lerp(buttonStartRotation, originalButtonRotation, t);
                buttonTransform.localScale = Vector3.Lerp(buttonStartScale, originalButtonScale, t);
            }

            // Animate camera
            if (animateCamera && cameraTransform != null)
            {
                cameraTransform.localPosition = Vector3.Lerp(cameraStartPosition, originalCameraPosition, t);
                cameraTransform.localRotation = Quaternion.Lerp(cameraStartRotation, originalCameraRotation, t);
                cameraTransform.localScale = Vector3.Lerp(cameraStartScale, originalCameraScale, t);
            }

            yield return null;
        }

        // Ensure exact original values for button
        if (animateButton && buttonTransform != null)
        {
            buttonTransform.localPosition = originalButtonPosition;
            buttonTransform.localRotation = originalButtonRotation;
            buttonTransform.localScale = originalButtonScale;
        }

        // Ensure exact original values for camera
        if (animateCamera && cameraTransform != null)
        {
            cameraTransform.localPosition = originalCameraPosition;
            cameraTransform.localRotation = originalCameraRotation;
            cameraTransform.localScale = originalCameraScale;
        }

        Debug.Log("✅ Manual reverse animation complete");
    }
}