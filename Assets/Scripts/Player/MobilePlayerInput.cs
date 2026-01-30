using UnityEngine;
using UnityEngine.EventSystems;

public class MobilePlayerInput : MonoBehaviour, IPlayerInput, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Steer")]
    public float steerSensitivity = 0.008f; // swipe pixels -> steer
    public float steerReturnSpeed = 6f;

    float steer;
    bool pointerDown;
    Vector2 lastPos;

    // These will be set by UI buttons
    public bool gasHeld;
    public bool brakeHeld;

    public float Throttle => gasHeld ? 1f : 0f;
    public float Brake => brakeHeld ? 1f : 0f;
    public float Steer => steer;

    void Update()
    {
        // return to center when no swipe
        if (!pointerDown)
            steer = Mathf.MoveTowards(steer, 0f, steerReturnSpeed * Time.deltaTime);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDown = true;
        lastPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.position - lastPos;
        lastPos = eventData.position;

        steer += delta.x * steerSensitivity;
        steer = Mathf.Clamp(steer, -1f, 1f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pointerDown = false;
    }
}
