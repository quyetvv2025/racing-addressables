using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TrafficCar : MonoBehaviour
{
    public int laneIndex;
    public bool scored;

    [HideInInspector] public float speed; // m/s
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    public void ResetState(int lane, float newSpeed)
    {
        laneIndex = lane;
        speed = newSpeed;
        scored = false;
    }

    void FixedUpdate()
    {
        // Move forward in world Z (straight highway)
        Vector3 v = rb.linearVelocity;
        v.z = speed;
        rb.linearVelocity = v;
    }
}
