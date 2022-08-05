using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;

public class OpponentManager : MonoBehaviour
{
    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private Transform _target;
    [SerializeField] private Transform _target2;
    [SerializeField] private Rigidbody _rigidBody;
    private Animator _anim;
    private float _opponentRotation;
    private bool _isCheck = false;

    private void OnEnable() 
    {
        GameManager.OnLevelStarted += startMove;
        GameManager.OnLevelCompleted += endGame;
        _anim = GetComponentInChildren<Animator>();
        
        if(!_isCheck)
        {
            transform.RotateAround(transform.parent.position,Vector3.back * 25, 180 * Time.fixedDeltaTime);
            _isCheck = true;
        }
    }

    private void OnDisable() 
    {
        GameManager.OnLevelStarted -= startMove;
        GameManager.OnLevelCompleted -= endGame;
    }

    private void startMove()
    {
        _rigidBody.velocity = Vector3.zero;
        _rigidBody.angularVelocity = Vector3.zero;
        _rigidBody.constraints = RigidbodyConstraints.FreezeRotationY;
        _agent.SetDestination(_target.position);
        _anim.SetTrigger("Run");
    }

    private void OnTriggerEnter(Collider other) 
    {
        if(other.tag == "RotatingPlatform")
        {
            _isCheck = false;
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
            _rigidBody.angularVelocity=Vector3.zero;
            _rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
            transform.parent = other.transform;
            for(int i=0; i<10; i++)
            {
                _opponentRotation = Random.Range(-1f, 7f);
                _rigidBody.velocity= Vector3.forward * 15 + Vector3.left * _opponentRotation;
            }
            _rigidBody.useGravity = false;
            _agent.enabled = false;
        }
        if(other.tag == "EndTrigger")
        {
            _rigidBody.detectCollisions = false;
        }
    }
    private void OnTriggerExit(Collider other) 
    {
       if(other.transform.tag =="RotatingPlatform")
        {
            if(_rigidBody.position.y <= -1f || _rigidBody.position.x <= -7.15f || _rigidBody.position.x >= 7.15f)
            {
                _agent.enabled = false;
                _agent.velocity = Vector3.zero;
                _rigidBody.angularVelocity=Vector3.zero;
                transform.position = Vector3.zero;
                _agent.enabled = true;
                _agent.SetDestination(_target.position);
            }
            else
            {
                _isCheck = true;
                _agent.enabled=true;
                _agent.isStopped = false;
                _rigidBody.velocity = Vector3.zero;
                _rigidBody.constraints = RigidbodyConstraints.None;
                _rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
                transform.parent = null;
                transform.position = new Vector3(0,0,transform.position.z);
                _agent.SetDestination(_target2.position);
            }
            
        }
        if(other.tag == "EndTrigger")
        {
            _agent.isStopped = true;
            _agent.enabled = false;
            _agent.velocity = Vector3.zero;
            _rigidBody.constraints = RigidbodyConstraints.FreezeAll;
            _anim.SetTrigger("Idle");
        }
    }

    private void OnCollisionEnter(Collision other) 
    {
        if (other.collider.tag == "Rotator")
        {
            Vector3 info = other.contacts[0].normal;
            info.y=0;
            _rigidBody.AddForce(info*500);
            DOVirtual.DelayedCall(.5f, ()=>_rigidBody.velocity = Vector3.zero);
        }

        if (other.collider.tag == "Donut")
        {
            Vector3 info = other.contacts[0].normal;
            info.y=0;
            _rigidBody.AddForce(info*500);
            DOVirtual.DelayedCall(.5f, ()=>_rigidBody.velocity = Vector3.zero);
        }

        if (other.collider.tag == "Obstacle")
        {
            _agent.enabled = false;
            _agent.velocity = Vector3.zero;
            _rigidBody.angularVelocity=Vector3.zero;
            transform.position = Vector3.zero;
            _agent.enabled = true;
            _agent.SetDestination(_target.position);
        }
    }

    private void endGame()
    {
        _agent.enabled = false;
        _rigidBody.velocity = Vector3.zero;
        _rigidBody.constraints = RigidbodyConstraints.FreezeAll;
        _anim.SetTrigger("Idle");
    }
}
