using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class HalfDonutObject : MonoBehaviour
{
    [SerializeField] private Transform _movingStick;
    [SerializeField] private float _movementDuration;
    [SerializeField] private float _targetX;

    private void OnEnable()
    {
        _movingStick.DOLocalMoveX(_targetX,_movementDuration)
            .SetLoops(-1,LoopType.Yoyo);
    } 
}
