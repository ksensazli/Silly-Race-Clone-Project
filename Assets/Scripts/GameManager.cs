using System;
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

    private void FixedUpdate() 
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
