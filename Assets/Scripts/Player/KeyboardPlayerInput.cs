using UnityEngine;

public class KeyboardPlayerInput : MonoBehaviour, IPlayerInput
{
    [Header("Keyboard")]
    public KeyCode throttleKey = KeyCode.W;
    public KeyCode brakeKey    = KeyCode.S;
    public KeyCode leftKey     = KeyCode.A;
    public KeyCode rightKey    = KeyCode.D;

    // Simple "digital -> analog" smoothing
    [Header("Smoothing")]
    public float steerResponse = 8f; // higher = snappier
    private float steerSmoothed;

    public float Throttle => Input.GetKey(throttleKey) ? 1f : 0f;
    public float Brake    => Input.GetKey(brakeKey) ? 1f : 0f;

    public float Steer
    {
        get
        {
            float target =
                (Input.GetKey(rightKey) ? 1f : 0f) -
                (Input.GetKey(leftKey)  ? 1f : 0f);

            // Smooth so holding keys feels analog-ish
            steerSmoothed = Mathf.MoveTowards(
                steerSmoothed,
                target,
                steerResponse * Time.deltaTime
            );

            return steerSmoothed;
        }
    }
}
