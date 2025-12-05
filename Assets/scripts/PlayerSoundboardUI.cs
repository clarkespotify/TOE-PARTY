using UnityEngine;
using System.Collections;

public class PlayerSoundboardUI : MonoBehaviour
{
    public GameObject uiIndicator; // Assign your UI element in the Inspector
    private Coroutine hideCoroutine;

    void Start()
    {
        // Make sure UI starts hidden
        if (uiIndicator != null)
        {
            uiIndicator.SetActive(false);
        }
    }

    public void ShowIndicator(float duration)
    {
        if (uiIndicator == null) return;

        // Stop any existing hide coroutine
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        // Show the UI
        uiIndicator.SetActive(true);

        // Hide after the sound duration
        hideCoroutine = StartCoroutine(HideAfterDelay(duration));
    }

    IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        uiIndicator.SetActive(false);
    }
}