using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class RankManager : MonoBehaviour
{
    [SerializeField] private GameObject[] _character;
    [SerializeField] private GameObject _leaderboard;
    [SerializeField] private string[] _text;
    [SerializeField] private float[] _distance;
    [SerializeField] private GameObject _target;
    [SerializeField] private TMPro.TMP_Text[] _TMPtext;
    private bool isLocked;

    void OnEnable()
    {
        GameManager.OnLevelStarted += showLeaderboard;
        GameManager.OnEndReached += closeLeaderboard;
    }

    void OnDisable()
    {
        GameManager.OnLevelStarted -= showLeaderboard;
        GameManager.OnEndReached -= closeLeaderboard;
    }
    void Update() 
    {
        if(!isLocked){
            calculateDistance();
            isLocked=true;
            DOVirtual.DelayedCall(.1f,()=>isLocked=false);
        }
    }

    private void calculateDistance()
    {
        for(int i=0; i<_character.Length; i++)
        {
            _distance[i] = _target.transform.position.z - _character[i].transform.position.z;
        }
        bubbleSort();
    }

    private void bubbleSort()
    {
        for(int i=0; i<_character.Length; i++)
        {
            for(int j=0; j<_character.Length -1; j++)
            {
                if(_distance[j] > _distance[j+1])
                {
                    float _temp = _distance[j];
                    _distance[j] = _distance[j+1];
                    _distance[j+1] = _temp;

                    GameObject _tempCharacter = _character[j];
                    _character[j] = _character[j+1];
                    _character[j+1] = _tempCharacter;
                }
            }
        }

        for(int i=0; i<_character.Length; i++)
        {
            _text[i] = _character[i].transform.name;
            _TMPtext[i].text = i+1 + ". " + _character[i].transform.name;
        }
    }

    private void showLeaderboard()
    {
        for(int i=0; i<_character.Length; i++)
        {
            DOVirtual.DelayedCall(1f, ()=>_leaderboard.SetActive(true));
        }
    }

    private void closeLeaderboard()
    {
        _leaderboard.SetActive(false);
    }
}
