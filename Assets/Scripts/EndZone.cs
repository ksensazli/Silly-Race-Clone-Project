using UnityEngine;

public class EndZone : MonoBehaviour
{
    [SerializeField] private ParticleSystem _confetties;
    void OnEnable()
    {
        GameManager.OnLevelCompleted += playConfetti;
    }

    void OnDisable() 
    {
        GameManager.OnLevelCompleted -= playConfetti;
    }

    private void playConfetti()
    {
        _confetties.Play();
    }
}
