using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public KeyCode pauseKey = KeyCode.P;
    public bool isPaused;

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 60;
        isPaused = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(pauseKey)) {
            isPaused = !isPaused;

            Time.timeScale = isPaused ? 0f : 1f;
        }
    }
}
