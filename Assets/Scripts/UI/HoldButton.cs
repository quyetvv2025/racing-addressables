using UnityEngine;
using UnityEngine.EventSystems;

public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum Type { Gas, Brake }
    public Type type;
    public MobilePlayerInput input;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!input) return;
        if (type == Type.Gas) input.gasHeld = true;
        else input.brakeHeld = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!input) return;
        if (type == Type.Gas) input.gasHeld = false;
        else input.brakeHeld = false;
    }
}
