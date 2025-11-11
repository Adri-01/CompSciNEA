using System;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public float throttle;
    public float brake;
    public float steering;
    public bool shiftUp;
    public bool shiftDown;
    public string transmissionType;
    public MainMenuManager menuManager;

    public Dictionary<string, KeyCode> keybinds = new Dictionary<string, KeyCode>();

    void Awake()
    {
        // Default bindings
        keybinds["AccelerateBind"] = KeyCode.W;
        keybinds["BrakeBind"] = KeyCode.S;
        keybinds["SteerLeftBind"] = KeyCode.A;
        keybinds["SteerRightBind"] = KeyCode.D;
        keybinds["ShiftDownBind"] = KeyCode.C;
        keybinds["ShiftUpBind"] = KeyCode.F;
    }
    void Start()
    {
        transmissionType = "Manual";
    }
    void Update()
    {
        Debug.Log("Update running. Transmission type: " + transmissionType);
    }

    void FixedUpdate()
    {
        throttle = Input.GetKey(keybinds["AccelerateBind"]) ? 1.0f : 0.0f;
        brake = Input.GetKey(keybinds["BrakeBind"]) ? 1.0f : 0.0f;

        bool left = Input.GetKey(keybinds["SteerLeftBind"]);
        bool right = Input.GetKey(keybinds["SteerRightBind"]);

        steering = 0;
        if (left && !right) steering = -1;
        else if (right && !left) steering = 1;

        shiftDown = Input.GetKeyDown(keybinds["ShiftDownBind"]);
        shiftUp = Input.GetKeyDown(keybinds["ShiftUpBind"]);
    }

    public void ToggleTransmissionType()
    {
        if (transmissionType == "Manual")
        {
            transmissionType = "Automatic";
        }
        else
        {
            transmissionType = "Manual";
        }
        Debug.Log("Toggle function called! Current type: " + transmissionType);
    }

    public void SetKeybind(string tag, char keyChar)
    {
        KeyCode keyCode = (KeyCode)System.Enum.Parse(typeof(KeyCode), keyChar.ToString().ToUpper());

        keybinds[tag] = keyCode;
    }
    public void SetControllerKeybind(string tag, KeyCode keyCode)
    {
        if (keybinds.ContainsKey(tag)) keybinds[tag] = keyCode;
    }
}