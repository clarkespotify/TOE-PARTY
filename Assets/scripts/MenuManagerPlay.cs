using UnityEngine;
using Unity.Netcode;

public class MenuManagerPlay : MonoBehaviour
{
    public GameObject pausemenu;
    private FirstPersonCamera playerCamera;
    public GameObject otherUI;

    void Start()
    {
        pausemenu.SetActive(false);
    }

    void Update()
    {
        // DON'T DO ANYTHING if voting is active
        if (VotingManager.Instance != null && VotingManager.Instance.IsVotingActive())
        {
            return; // Exit early - don't process any input during voting
        }

        // Find the camera if we don't have a reference yet
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<FirstPersonCamera>();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Toggle the pause menu
            bool isPaused = !pausemenu.activeSelf;
            pausemenu.SetActive(isPaused);
            otherUI.SetActive(false);

            // Enable/disable camera (opposite of pause state)
            if (playerCamera != null)
            {
                playerCamera.enabled = !isPaused;
            }

            // Lock/unlock cursor based on pause state
            if (isPaused)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                otherUI.SetActive(true);
            }
        }
    }
}