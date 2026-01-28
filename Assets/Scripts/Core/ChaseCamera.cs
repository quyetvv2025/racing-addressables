using UnityEngine;

public class ChaseCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Offset (local to target)")]
    public Vector3 offset = new Vector3(0f, 6f, -14f);

    [Header("Smoothing")]
    public float positionSmooth = 8f;
    public float rotationSmooth = 10f;

    [Header("Look Ahead")]
    public float lookAheadDistance = 12f;
    public float lookAheadBySpeed = 0.25f; // extra lookahead = speed * this

    [Header("Optional: Use Rigidbody velocity")]
    public Rigidbody targetRb;

    void LateUpdate()
    {
        if (target == null) return;

        // 1) Follow position (camera stays behind car)
        Vector3 desiredPos = target.TransformPoint(offset); // offset behind target
        transform.position = Vector3.Lerp(transform.position, desiredPos, 1f - Mathf.Exp(-positionSmooth * Time.deltaTime));

        // 2) Compute look point (ahead of car)
        float speed = 0f;
        if (targetRb != null) speed = targetRb.linearVelocity.magnitude;

        float ahead = lookAheadDistance + speed * lookAheadBySpeed;
        Vector3 lookPoint = target.position + target.forward * ahead;

        // 3) Smooth rotation to look at the lookPoint
        Quaternion desiredRot = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, 1f - Mathf.Exp(-rotationSmooth * Time.deltaTime));
    }
}