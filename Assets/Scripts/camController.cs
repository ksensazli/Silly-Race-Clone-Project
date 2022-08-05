using Cinemachine;
using UnityEngine;

public class camController : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera firstVirtualCam;
    [SerializeField] private CinemachineVirtualCamera secondVirtualCam;

    private void OnEnable()
    {
       GameManager.OnLevelStarted += startTheGame;
    }
    private void OnDisable() 
    {
        GameManager.OnLevelStarted -= startTheGame;
    }
    
    private void startTheGame()
    {
        firstVirtualCam.enabled = false;
        secondVirtualCam.enabled = true; 
    }
}
