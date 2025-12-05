using UnityEngine;
using TMPro;

public class PlaySceneJoinCodeDisplay : MonoBehaviour
{
    public TextMeshProUGUI joinCodeText;

    private void Start()
    {
        DisplayJoinCode();
    }

    private void DisplayJoinCode()
    {
        if (RelayManager.Instance != null)
        {
            string joinCode = RelayManager.Instance.JoinCode;

            if (!string.IsNullOrEmpty(joinCode))
            {
                joinCodeText.text = "JOIN CODE: " + joinCode;
                Debug.Log($"Displayed join code in play scene: {joinCode}");
            }
            else
            {
                joinCodeText.text = "JOIN CODE: N/A";
            }
        }
        else
        {
            Debug.LogError("RelayManager not found!");
        }
    }
}