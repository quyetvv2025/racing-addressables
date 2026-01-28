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
    
    // Backing fields
    private float _throttle;
    private float _brake;
    private float _steer;

    public float Throttle => _throttle;
    public float Brake    => _brake;
    public float Steer    => _steer;

    void Update()
    {
        // Read raw inputs
        bool t = Input.GetKey(throttleKey);
        bool b = Input.GetKey(brakeKey);
        bool l = Input.GetKey(leftKey);
        bool r = Input.GetKey(rightKey);

        _throttle = t ? 1f : 0f;
        _brake    = b ? 1f : 0f;

        // Steering smoothing
        float targetSteer = (r ? 1f : 0f) - (l ? 1f : 0f);
        steerSmoothed = Mathf.MoveTowards(
            steerSmoothed,
            targetSteer,
            steerResponse * Time.deltaTime
        );
        _steer = steerSmoothed;
        
        // Debugging (Optional)
        // Debug.Log($"[INPUT] T:{_throttle} B:{_brake} S:{_steer}");
    }
}
