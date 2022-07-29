using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CanvasController : MonoBehaviour
{
    [SerializeField] private GameObject _tutorialSc;
    [SerializeField] private GameObject _endSc;

    void OnEnable()
    {
        GameManager.OnLevelStarted += startScreen;
        GameManager.OnLevelCompleted += endScreen;
    }

    void OnDisable() 
    {
        GameManager.OnLevelStarted -= startScreen;
        GameManager.OnLevelCompleted -= endScreen;
    }

    private void startScreen()
    {
        DOVirtual.DelayedCall(1f, ()=>_tutorialSc.SetActive(false));
    }

    private void endScreen()
    {
        _endSc.SetActive(true);
    }
}
