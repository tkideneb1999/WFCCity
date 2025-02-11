using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FunctionTimer
{
    private float startTime;
    public FunctionTimer()
    {
        startTime = Time.realtimeSinceStartup;
    }

    public void StopTimer(string message)
    {
        Debug.Log(message + " " + (Time.realtimeSinceStartup - startTime) + "s");
    }
}
