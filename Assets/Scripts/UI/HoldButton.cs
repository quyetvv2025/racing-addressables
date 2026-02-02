using UnityEngine;
using UnityEngine.EventSystems;

public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum Type { Gas, Brake }
    public Type type;
    public MobilePlayerInput input;

    void Start()
    {
        if (input == null)
            Debug.LogError($"[HoldButton] '{gameObject.name}' - input reference is NULL! Assign MobilePlayerInput in Inspector.");
        else
            Debug.Log($"[HoldButton] '{gameObject.name}' - input reference OK: {input.gameObject.name}");
    }

    void OnEnable()
    {
        Debug.Log($"[HoldButton] '{gameObject.name}' OnEnable - input is {(input == null ? "NULL" : "SET")}");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"[HoldButton] '{gameObject.name}' ({type}) - OnPointerDown triggered");

        if (!input)
        {
            Debug.LogError($"[HoldButton] '{gameObject.name}' - Cannot set {type}, input is NULL!");
            return;
        }

        if (type == Type.Gas) input.gasHeld = true;
        else input.brakeHeld = true;

        Debug.Log($"[HoldButton] Set {type} = true");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log($"[HoldButton] '{gameObject.name}' ({type}) - OnPointerUp triggered");

        if (!input)
        {
            Debug.LogError($"[HoldButton] '{gameObject.name}' - Cannot release {type}, input is NULL!");
            return;
        }

        if (type == Type.Gas) input.gasHeld = false;
        else input.brakeHeld = false;

        Debug.Log($"[HoldButton] Set {type} = false");
    }
}
