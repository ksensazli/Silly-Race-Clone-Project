using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class HorizontalObstacleObject : MonoBehaviour
{
    [SerializeField] private Transform _rotatingObstacle;
    [SerializeField] private float _movementDuration;
    [SerializeField] private float _targetX;
    [SerializeField] private float _rotatingSpeed;

    private void OnEnable()
    {
        transform.DOLocalMoveX(_targetX,_movementDuration)
            .SetLoops(-1,LoopType.Yoyo);
    }
    
    private void FixedUpdate() 
    {
        _rotatingObstacle.Rotate(Vector3.up, _rotatingSpeed * Time.fixedDeltaTime);
    }
}
