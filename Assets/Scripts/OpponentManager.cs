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
        // Although there is no problem when starting the first move; when opponent characters collide 
        // with an obstacle and start again, the following codes are used to reset the force and motion left over from the previous collision.
        _rigidBody.velocity = Vector3.zero;
        _rigidBody.angularVelocity = Vector3.zero;
        _rigidBody.constraints = RigidbodyConstraints.FreezeRotationY;
        _agent.SetDestination(_target.position);
        _anim.SetTrigger("Run");
    }

    //It is the function where various trigger enter events are written.
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

            //The for loop is used to ensure that the Opponent Characters have a random transition rate.
            for(int i=0; i<10; i++)
            {
                _opponentRotation = Random.Range(-1f, 7f);
                _rigidBody.velocity= Vector3.forward * 15 + Vector3.left * _opponentRotation;
            }

            _rigidBody.useGravity = false;
            _agent.enabled = false;
        }

        //The following function is used so that the characters do not push each other when the finish line is reached (since we are using a rigidbody).
        if(other.tag == "EndTrigger")
        {
            _rigidBody.detectCollisions = false;
        }
    }

    //It is the function where various trigger exit events are written.
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

    //It is the function where various collision enter events are written.
    private void OnCollisionEnter(Collision other) 
    {
        if (other.collider.tag == "Rotator")
        {
            //This function is used to obtain the normal (perpendicular to it) of the object hit by the character.
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

    //It stops the movement of all characters when the player completes the coloring and finishes the level.
    private void endGame()
    {
        _agent.enabled = false;
        _rigidBody.velocity = Vector3.zero;
        _rigidBody.constraints = RigidbodyConstraints.FreezeAll;
        _anim.SetTrigger("Idle");
    }
}
