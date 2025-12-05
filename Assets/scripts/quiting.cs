using UnityEngine;

public class quiting : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    public void OnQuitClicked()
    {
        Application.Quit();
        Debug.Log("YOU QUIT THE GAME? fair enough.");
    }
}
