using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartButton : MonoBehaviour
{
    public void restartGame()
    {
        SceneManager.LoadScene("SampleScene");
    }
    
}
