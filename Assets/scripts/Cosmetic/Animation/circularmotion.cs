using UnityEngine;

public class circularmotion : MonoBehaviour
{
    float timeCounter = 0;

    float speed;
    float width;
    float height;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        speed = 1;
        width = 4;
        height = 7;
    }

    // Update is called once per frame
    void Update()
    {
        timeCounter += Time.deltaTime * speed;

        float x = Mathf.Cos (timeCounter)*width;
        float y = 0;
        float z = Mathf.Sin(timeCounter) * height;

        transform.position = new Vector3(x, y, z);
    }
}
