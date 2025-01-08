using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Timer : MonoBehaviour
{
    private float timer;
    public float timerSecret;
    private bool isTimerRunning;
    public TMP_Text timerText;

    void Start()
    {
        timer = 0f;
        timerSecret = 0f;
        isTimerRunning = false;
    }

    void Update()
    {
        if (isTimerRunning)
        {
            timer += Time.deltaTime;
            timerText.text = timer.ToString("F2");
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            timer = 0f;
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            timer = 0f;
            timerText.text = "";
            isTimerRunning = false;
        }
        timerSecret += Time.deltaTime;
    }

    public void StartTimer()
    {
        if (!isTimerRunning)
        {
            timer = 0f;
            isTimerRunning = true;
        }
        else
        {
            isTimerRunning = false;
        }
    }

    public float GetTimer()
    {
        return timer;
    }

    public float returnTimer()
    {
        return timerSecret;
    }
    
}
