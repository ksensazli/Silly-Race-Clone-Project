using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndZone : MonoBehaviour
{
    [SerializeField] private ParticleSystem _confetties;
    void OnEnable()
    {
        GameManager.OnEndReached += playConfetti;
    }

    void OnDisable() 
    {
        GameManager.OnEndReached -= playConfetti;
    }

    private void playConfetti()
    {
        _confetties.Play();
    }
}
