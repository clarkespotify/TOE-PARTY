using UnityEngine;
using TMPro;

public class SHOWRULES : MonoBehaviour
{
    public GameObject rules;

    void Start()
    {
        // Make sure rules is hidden at start
        if (rules != null)
        {
            rules.SetActive(false);
        }
    }

    public void OnClickers()
    {
        if (rules != null)
        {
            // Toggle between visible and hidden
            rules.SetActive(!rules.activeSelf);
            Debug.Log("Rules toggled: " + rules.activeSelf);
        }
    }
}