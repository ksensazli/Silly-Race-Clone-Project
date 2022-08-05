using UnityEngine;

public class RotatorObject : MonoBehaviour
{
    public float rotatingSpeed = 45f;
    void FixedUpdate()
    {
        transform.Rotate(Vector3.up, rotatingSpeed * Time.fixedDeltaTime);
    }
}
