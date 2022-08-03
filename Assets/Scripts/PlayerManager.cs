using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private Rigidbody rb; 
    [SerializeField] private float _forwardSpeed;
    [SerializeField] private float _slideSpeed;
    [SerializeField] private float _LerpSpeed;
    [SerializeField] private float _rotationSpeed;
    private touchInput _touchInput;
    private Vector3 _forwardMoveAmount;
    private Animator _anim;
    private bool _isEndGame = false;
    private bool _isStart = false;
    private bool _isOnPlatform;
    private bool _isPainting;
    private float _xPos = 0;

    private void OnEnable()
    {
        GameManager.OnLevelStarted += startGame;
        _anim = GetComponentInChildren<Animator>();
        _touchInput = GetComponent<touchInput>();
    }

    private void OnDisable() 
    {
        GameManager.OnLevelStarted -= startGame;
    }

    void FixedUpdate()
    {
        if(!_isStart)
        {
            return;
        }
        if (_isEndGame)
        {
            return;
        }

        if(_isPainting)
        {
            return;
        }

        if(_isOnPlatform)
        {
            moveCharacterPlatform();
        }
        else
        {
            moveCharacterNormal();
        }
    }

    private void startGame()
    {
        _anim.SetTrigger("Run");
        _isStart = true;
    }

    //It is the function that allows the character to move while on the platform.
    private void moveCharacterPlatform()
    {
        rb.velocity = Vector3.forward * 15;
        
        if(_touchInput.DragAmountDeltaX > 0 || _touchInput.DragAmountDeltaX < 0)
        {
            transform.RotateAround(transform.parent.position,Vector3.back * _touchInput.DragAmountDeltaX, _rotationSpeed * Time.fixedDeltaTime);
        }
        
        if(rb.position.y <= -1.01f && rb.position.y >= -1.99f)
        {
            checkEndGame();
        }

        if(rb.position.y <= -2f)
        {
            //It is the function that allows us to reload (restart) the scene.
            DOVirtual.DelayedCall(1f, ()=>SceneManager.LoadScene(SceneManager.GetActiveScene().name));
        }
    }

    //It is the function that allows the character to move forward continuously, to the right/left when controlled.
    private void moveCharacterNormal()              
    {
        if (rb.position.y >= -1f)
        {
            _forwardMoveAmount = Vector3.forward* _forwardSpeed;
            Vector3 targetPosition = rb.transform.position + _forwardMoveAmount;

            if(Input.GetMouseButton(0) && !_isOnPlatform)
            {
                targetPosition.x = 0;
                targetPosition.x = _touchInput.DragAmountX * _slideSpeed;
            }

            //Thanks to the Mathf.Clamp function, we determine the max and min points that our character can go on the X axis.
            _xPos = Mathf.Clamp(targetPosition.x, -4.25f, 4.25f);

            //Lerp is a function that allows us to go from one point to another on a linear scale at a given time.
            Vector3 targetPositionLerp = new Vector3(Mathf.Lerp(rb.position.x,_xPos,Time.fixedDeltaTime * _LerpSpeed),
            Mathf.Lerp(rb.position.y,targetPosition.y,Time.fixedDeltaTime * _LerpSpeed)
            ,Mathf.Lerp(rb.position.z,targetPosition.z,Time.fixedDeltaTime * _LerpSpeed));

            rb.MovePosition(targetPositionLerp);
        }
        else
        {
            checkEndGame();
        }
    }

    //It is the function that checks whether the game is over or not.
    private void checkEndGame()                         
    {
        _isStart = false;
        rb.useGravity = true;
        transform.parent = null;
        rb.velocity = Vector3.zero;
        _isEndGame = true;
        _anim.SetTrigger("Idle");
        DOVirtual.DelayedCall(1f, ()=>SceneManager.LoadScene(SceneManager.GetActiveScene().name));
    }

    //It is the function where various trigger enter events are written.
    private void OnTriggerEnter(Collider other)         
    {
        if(other.tag == "RotatingPlatform")
        {
            rb.useGravity = false;
            transform.parent = other.transform;
            _isOnPlatform = true;
        }
    }

    //It is the function where various trigger exit events are written.
    private void OnTriggerExit(Collider other)          
    {
        if(other.tag == "RotatingPlatform")
        {
            _isOnPlatform=false;
            transform.parent = null;
            transform.rotation = Quaternion.Euler(Vector3.zero);
            transform.position = new Vector3(transform.position.x, 0, transform.position.z);
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if(other.tag == "EndTrigger")
        {
            _isPainting = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            GameManager.OnEndReached?.Invoke();
            _anim.SetTrigger("Idle");
        }

        if(other.tag == "Ground")
        {
            rb.useGravity = true;
        }
    }

    //It is the function where various collision enter events are written.
    void OnCollisionEnter(Collision collisionInfo)      
    {
        if (collisionInfo.collider.tag == "Rotator")
        {
            Vector3 info = collisionInfo.contacts[0].normal;
            info.y=0;
            rb.AddForce(info*600);
            DOVirtual.DelayedCall(.5f, ()=>rb.velocity = Vector3.zero);
        }

        if (collisionInfo.collider.tag == "Donut")
        {
            Vector3 info = collisionInfo.contacts[0].normal;
            info.y=0;
            rb.AddForce(info*500);
            DOVirtual.DelayedCall(.5f, ()=>rb.velocity = Vector3.zero);
        }

        if (collisionInfo.collider.tag == "Obstacle")
        {
            checkEndGame();
        }
    }
}
