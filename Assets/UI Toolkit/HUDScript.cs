using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CarHUD : MonoBehaviour
{
    public CarController carController;
    public InputManager im;
    public RacingLineManager lineManager;
    private Label mphLabel;
    private Label gearLabel;
    private Label FPSLabel;
    private Label LAPDistanceLabel;
    private Label LAPLabel;
    private Label LAPAngleLabel;
    private Label distanceFromLineLabel;
    private Label CurrentRPMLabel;
    public VisualElement RPMContainer;
    private ProgressBar BrakeProgressBar;
    private ProgressBar ThrottleProgressBar;
    private List<VisualElement> rpmCircles = new List<VisualElement>();


    void Start()
    {
        UIDocument uiDocument = GetComponent<UIDocument>();
        VisualElement root = uiDocument.rootVisualElement;

        mphLabel = root.Q<Label>("MPH");
        gearLabel = root.Q<Label>("CurrentGear");
        FPSLabel = root.Q<Label>("FPS");
        LAPDistanceLabel = root.Q<Label>("LAP_Distance");
        LAPLabel = root.Q<Label>("LAP_Cords");
        LAPAngleLabel = root.Q<Label>("LAP_Angle");
        distanceFromLineLabel = root.Q<Label>("DistanceFromLine");
        RPMContainer = root.Q<VisualElement>("RPMContainer");
        CurrentRPMLabel = root.Q<Label>("CurrentRPM");
        BrakeProgressBar = root.Q<ProgressBar>("BrakeBarFill");
        ThrottleProgressBar = root.Q<ProgressBar>("ThrottleBarFill");

        int numberOfCircles = 11;
        List<int> green = new List<int> { 0, 1, numberOfCircles - 1, numberOfCircles - 2 };
        List<int> orange = new List<int> { 2, 3, numberOfCircles - 3, numberOfCircles - 4 };

        for (int i = 0; i < numberOfCircles; i++)
        {
            var circle = new VisualElement();
            circle.style.width = 50;
            circle.style.height = 50;

            circle.style.borderTopLeftRadius = 25;
            circle.style.borderTopRightRadius = 25;
            circle.style.borderBottomLeftRadius = 25;
            circle.style.borderBottomRightRadius = 25;

            circle.style.borderTopWidth = 2; //Outline thickness.
            circle.style.borderTopColor = Color.black;
            circle.style.borderRightWidth = 2; //Outline thickness.
            circle.style.borderRightColor = Color.black;
            circle.style.borderBottomWidth = 2; //Outline thickness.
            circle.style.borderBottomColor = Color.black;
            circle.style.borderLeftWidth = 2; //Outline thickness.
            circle.style.borderLeftColor = Color.black;

            circle.style.position = Position.Relative;

            // Initial base color (very faint)
            if (green.Contains(i)) circle.style.backgroundColor = new Color(0, 1, 0, 0.1f);
            else if (orange.Contains(i)) circle.style.backgroundColor = new Color(1, 0.66f, 0, 0.1f);
            else circle.style.backgroundColor = new Color(1, 0, 0, 0.1f);

            RPMContainer.Add(circle);
            rpmCircles.Add(circle); // Store for later reference

        }
    }

    void Update()
    {
        float mph = carController.speed * 2.236936f;
        mphLabel.text = mph.ToString("F0");

        int currentGear = carController._currentGearIndex + 1;
        gearLabel.text = currentGear.ToString();

        float fps = 1f / Time.deltaTime;

        if (fps <= 30)
        {
            FPSLabel.style.color = Color.red;
        }
        else if (fps <= 60)
        {
            FPSLabel.style.color = Color.yellow;
        }
        else
        {
            FPSLabel.style.color = Color.green;
        }

        FPSLabel.text = fps.ToString("F0");

        float lapDistance = lineManager.LAP_Distance;
        LAPDistanceLabel.text = lapDistance.ToString("F1");

        Vector2 LAP = lineManager.lookAheadPoint;
        LAPLabel.text = LAP.ToString();

        string LAPAngle = lineManager.LAP_Angle.ToString("F0");
        LAPAngleLabel.text = $"{LAPAngle} °";

        float DistanceFromLine = lineManager.shortestDistance;
        distanceFromLineLabel.text = DistanceFromLine.ToString("F2");



        float CurrentRPM = carController.engineRPM;
        CurrentRPMLabel.text = CurrentRPM.ToString("F0");

        for (int i = 0; i < rpmCircles.Count; i++){
            VisualElement circle = rpmCircles[i];
            
            if (i <= 1 || i >= rpmCircles.Count - 2)
            {
                circle.style.backgroundColor = new Color(0, 1, 0, carController.engineRPM > 5000f ? 0.7f : 0.1f);
            }
            else if (i <= 3 || i >= rpmCircles.Count - 4)
            {
                circle.style.backgroundColor = new Color(1, 0.66f, 0, carController.engineRPM > 5750f ? 0.7f : 0.1f);
            }
            else
            {
                circle.style.backgroundColor = new Color(1, 0, 0, carController.engineRPM > 6500f ? 0.7f : 0.1f);
            }
        }
        float brake = im.brake;
        BrakeProgressBar.value = brake;
        float throttle = im.brake;
        ThrottleProgressBar.value = throttle;
    }
}