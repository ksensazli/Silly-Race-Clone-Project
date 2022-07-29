using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static Action OnLevelStarted;
    public static Action OnEndReached;
    public static Action OnPaintingDone;
    public static Action OnLevelCompleted;
    private bool _isStart;

    private void startLevel()
    {
        OnLevelStarted?.Invoke();
        _isStart = true;
    }

    private void Update() 
    {
        if (!_isStart)
        {
            if(Input.GetMouseButtonDown(0))
            {
                startLevel();
            }
            return;
        }
    }
}
