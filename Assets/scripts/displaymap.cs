using UnityEngine;

public class displaymap : MonoBehaviour
{
    public GameObject Conferencetable;
    public GameObject startgame;
    public Animator PlaySpaz;
    public Animator PlayAnvil;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    public void OnMapOne()
    {
        Conferencetable.SetActive(true);
        startgame.SetActive(true);
        PlaySpaz.Play("jumping");
        PlayAnvil.Play("crushtobo");
    }

    // Update is called once per frame
    public void OnMapTwo()
    {
        Conferencetable.SetActive(false);
        startgame.SetActive(false);
    }

    // Update is called once per frame
    public void OnMapThree()
    {
        Conferencetable.SetActive(false);
        startgame.SetActive(false);
    }
}
