using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class touchInput : MonoBehaviour
{
    private float _lastPosX;
    private float _dragAmountX;
    public float DragAmountX => _dragAmountX;
    private float _dragAmountDelta;
    public float DragAmountDeltaX => _dragAmountDelta;
    private float _startPosX;
    private float _offset;
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _startPosX = Input.mousePosition.x;
            _lastPosX = _startPosX;
            _offset = _dragAmountX;
        }
        else if (Input.GetMouseButton(0))
        {
            _dragAmountX = Input.mousePosition.x - _startPosX + _offset;
            _dragAmountDelta = Input.mousePosition.x - _lastPosX;
            _lastPosX =  Input.mousePosition.x;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            _dragAmountDelta = 0;
        }
    }
}
