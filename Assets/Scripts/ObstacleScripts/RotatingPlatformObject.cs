using UnityEngine;

public class RotatingPlatformObject : MonoBehaviour
{
    public float rotatingSpeed;
    void FixedUpdate()
    {
        transform.Rotate(Vector3.back, rotatingSpeed * Time.fixedDeltaTime);
    }
}
