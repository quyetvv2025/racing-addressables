using UnityEngine;

public class PlayerCarMotor : MonoBehaviour
{
    [Header("References")]
    public MonoBehaviour inputSource; // must implement IPlayerInput
    private IPlayerInput input;
    private Rigidbody rb;

    [Header("Speed")]
    public float maxSpeed = 45f;          // m/s  (~162 km/h)
    public float accel = 18f;             // m/s^2
    public float brakeDecel = 28f;        // m/s^2
    public float naturalDecel = 6f;       // when no throttle

    [Header("Steering")]
    public float lateralSpeed = 10f;      // m/s sideways at low speed
    public float steerStrength = 1f;      // multiplier
    public AnimationCurve steerBySpeed = AnimationCurve.EaseInOut(0, 1f, 1f, 0.35f);
    // x = normalized speed (0..1), y = steer factor

    [Header("Clamp to Road")]
    public float clampHalfWidth = 5.5f;   // road half width minus padding
    public float clampPadding = 0.2f;

    [Header("Stability")]
    public float extraDownforce = 30f;    // helps keep on road

    float targetForwardSpeed;
    Vector3 startPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        startPos = transform.position;

        input = inputSource as IPlayerInput;
        if (input == null)
        {
            Debug.LogError("PlayerCarMotor: inputSource must implement IPlayerInput.");
        }

        targetForwardSpeed = 15f; // base forward speed so car always moves
    }

    void FixedUpdate()
    {
        if (input == null) return;

        // 1) Forward speed control
        float throttle = input.Throttle;
        float brake = input.Brake;

        float curForward = Vector3.Dot(rb.linearVelocity, Vector3.forward);

        if (throttle > 0f)
            targetForwardSpeed += accel * throttle * Time.fixedDeltaTime;
        else
            targetForwardSpeed -= naturalDecel * Time.fixedDeltaTime;

        if (brake > 0f)
            targetForwardSpeed -= brakeDecel * brake * Time.fixedDeltaTime;

        targetForwardSpeed = Mathf.Clamp(targetForwardSpeed, 0f, maxSpeed);

        // Approach target speed smoothly
        float newForward = Mathf.MoveTowards(curForward, targetForwardSpeed, accel * Time.fixedDeltaTime);

        // 2) Lateral steering (analog)
        float steer = input.Steer; // -1..1
        float speed01 = (maxSpeed <= 0.01f) ? 0f : Mathf.Clamp01(Mathf.Abs(newForward) / maxSpeed);
        float steerFactor = steerBySpeed.Evaluate(speed01) * steerStrength;

        float desiredLateral = steer * lateralSpeed * steerFactor;

        // Compose new velocity (keep current Y from physics)
        Vector3 v = rb.linearVelocity;
        v.z = newForward;
        v.x = desiredLateral;
        rb.linearVelocity = v;

        // 3) Extra downforce for stability
        rb.AddForce(Vector3.down * extraDownforce, ForceMode.Acceleration);

        // 4) Clamp X to road bounds (avoid flying off road)
        Vector3 p = rb.position;
        float limit = Mathf.Max(0f, clampHalfWidth - clampPadding);
        p.x = Mathf.Clamp(p.x, startPos.x - limit, startPos.x + limit);
        rb.position = p;
    }
}
