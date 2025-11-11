using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour, IPointerEnterHandler
{
    public InputManager im;
    bool capturingInput;
    Button currentButton;
    public Button[] controlButtons;
    public GameObject CaptureKeybindScreen;
    public GameObject settingsScreen;
    public TextMeshProUGUI KeybindCaptureText;
    public TextMeshProUGUI infoText;

    void Start()
    {
        loadKeybinds();
    }

    void Update()
    {
        if (capturingInput)
        {
            CaptureKeyboardInput();
        }
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        Debug.Log("Game Quit");
        Application.Quit();
    }
    public void OnPointerEnter(PointerEventData eventData)
        {
            Button hoveredButton = eventData.pointerEnter.GetComponentInParent<Button>();
            
            if (hoveredButton == null){
                infoText.text = "";
            }
            else if (hoveredButton != null){
                string tag = hoveredButton.tag;
                switch (tag)
                {
                    case "AccelerateBind":
                        infoText.text = "The key you press to accelerate the car."; 
                        break;
                    case "BrakeBind":
                        infoText.text = "The key you press to stop the car."; 
                        break;
                    case "SteerLeftBind":
                        infoText.text = "The key you press to turn the car left."; 
                        break;
                    case "SteerRightBind":
                        infoText.text = "The key you press to turn the car right."; 
                        break;
                    case "ShiftDownBind":
                        infoText.text = "The key you press to decrease a gear. (Manual transmission type only)"; 
                        break;
                    case "ShiftUpBind":
                        infoText.text = "The key you press to increase a gear. (Manual transmission type only)"; 
                        break;
                    default:
                        infoText.text = "";
                        break;
                }
            }
        }


    public void loadKeybinds()
    {
        foreach (Button button in controlButtons)
        {
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            string tag = button.tag;
            
            buttonText.text = $"<color=lightblue>{im.keybinds[tag]}</color>";
        }
    }

    public void ChangeKeybind(Button clickedButton)
    {
        currentButton = clickedButton;
        KeybindCaptureText.text = $"Rebinding <color=yellow>{clickedButton.tag}</color>. Press <color=blue>[any key]</color> on keyboard to continue. \n\n Press <color=blue>[escape]</color> to cancel or click the <color=orange>(back)</color> button.";
        capturingInput = true;
    }
    public void CancelInputCapture(){
        capturingInput = false;
        settingsScreen.SetActive(true);
        CaptureKeybindScreen.SetActive(false);
    }

    void CaptureKeyboardInput()
    {    
        if (Input.anyKeyDown)
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) {
                foreach (char c in Input.inputString)
                {
                    if (c != '\\') // Skip escape
                    {
                        capturingInput = false;

                        // Display key pressed on button
                        TextMeshProUGUI buttonText = currentButton.GetComponentInChildren<TextMeshProUGUI>();
                        buttonText.text = c.ToString().ToUpper();
                        im.SetKeybind(currentButton.tag, c);

                        // Change screen
                        settingsScreen.SetActive(true);
                        CaptureKeybindScreen.SetActive(false);

                        break;
                    }
                }
            }
            else CancelInputCapture();
        }
    }
    void CaptureControllerInput()
    {
        if (!capturingInput) return;

        // Loop through potential joystick buttons
        for (int i = 0; i <= 19; i++) // Extend the range if needed
        {
            KeyCode button = (KeyCode)System.Enum.Parse(typeof(KeyCode), "JoystickButton" + i);
            if (Input.GetKeyDown(button))
            {
                capturingInput = false;

                // Update button text
                TextMeshProUGUI buttonText = currentButton.GetComponentInChildren<TextMeshProUGUI>();
                buttonText.text = button.ToString();

                // Save keybind
                im.SetControllerKeybind(currentButton.tag, button);

                // Switch screen
                settingsScreen.SetActive(true);
                CaptureKeybindScreen.SetActive(false);

                return;
            }
        }

        // Cancel with Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelInputCapture();
        }
    }
}