using System.Collections.Generic;
using UnityEngine;


public class RacingLineManager : MonoBehaviour
{
    public CarController _car;
    public TextAsset waypointFile;
    public int lookAheadIndex;
    private List<Vector2> waypoints = new List<Vector2>();
    private Vector2 closestWaypoint;
    public float shortestDistance;
    public Material lineMaterial;
    public float LAP_Distance;
    int closestIndex;
    public float LAP_Angle;
    public bool isOnPath;
    public float pointDisplacementBase = 5f; // Base displacement
    public float maxInitialAngle = 30f; // Maximum initial angle for slowing down
    public float lookAheadDistanceFactor = 1.5f; // Adjusts look ahead distance based on speed
    public float tangentSmoothingFactor = 0.5f; // Controls how much the line smooths out at the end
    private LineRenderer closestWaypointLine;
    public Vector3 lookAheadVector;
    public Vector2 lookAheadPoint;
    private LineRenderer lookAheadLine;
    public float turnSeverity; // 0 = straight, 1 = sharp turn
    private List<RacingLinePoint> racingLinePoints = new List<RacingLinePoint>();
    private List<RacingLinePoint> correctedLinePoints = new List<RacingLinePoint>();
    private List<Vector2> correctedPointWaypoints = new List<Vector2>();
    float rotationDamping = 0.2f; // Adjust this value to control smoothness
    public Gradient gradient = new Gradient();
    public GameObject _racingLinePrefab;
    public Vector2 carPosition;
    public bool advancedData = true;

    void Start()
    {
        if (waypointFile != null)
        {
            LoadWaypoints();
            MapRacingLine();

            GameObject closestLineObj = new GameObject("ClosestWaypointLine");
            closestLineObj.transform.parent = transform;
            closestWaypointLine = closestLineObj.AddComponent<LineRenderer>();

            GameObject lookAheadLineObj = new GameObject("LookAheadLine");
            lookAheadLineObj.transform.parent = transform;
            lookAheadLine = lookAheadLineObj.AddComponent<LineRenderer>();

        }
    }

    void Update()
    {
        carPosition = _car.position;
        float speed = _car.speed;
        Quaternion carRotation = _car.transform.rotation; // Get the car's rotation

        UpdatePointColors(racingLinePoints, speed);
        UpdatePointColors(correctedLinePoints, _car.speed);

        FindLookAheadPoint(carPosition, speed);
        bool onMainLine = IsOnPath();
        Debug.Log(onMainLine);
        bool onCorrected = IsOnCorrectedPath();

        if (!onMainLine && !onCorrected)
        {
            CalculateData(lookAheadIndex, speed); // Generate new corrected line
        }

        if (advancedData) AdvancedData(closestWaypoint, lookAheadPoint);
    }


    void LoadWaypoints()
    {
        string[] lines = waypointFile.text.Split('\n');
        foreach (string line in lines)
        {
            string[] values = line.Split(',');
            if (values.Length >= 2)
            {
                if (float.TryParse(values[0], out float x) && float.TryParse(values[1], out float y))
                {
                    waypoints.Add(new Vector2(x, y));
                }
            }
        }
    }

    private RacingLinePoint InstantiatePoint(Vector2 pos, Quaternion rot)
    {
        var obj = Instantiate(_racingLinePrefab, new Vector3(pos.x, 0.18f, pos.y), rot);
        return obj.GetComponent<RacingLinePoint>();
    }

    void MapRacingLine()
    {
        racingLinePoints.Clear();

        for (int i = 0; i < waypoints.Count; i++)
        {
            Vector2 current = waypoints[i];
            Vector2 dir = (waypoints[(i + 1) % waypoints.Count] - current).normalized;

            float severity = ComputeTurnSeverity(waypoints, i);

            Quaternion rotation = Quaternion.LookRotation(-1 * new Vector3(dir.x, 0, dir.y));
            var point = InstantiatePoint(current, rotation);

            point.turnSeverity = severity;
            point.coordinates = waypoints[i];
            point.SetActiveState(false);

            racingLinePoints.Add(point);
        }
    }

    private float ComputeTurnSeverity(List<Vector2> list, int index, int lookaheadSteps = 15)
    {
        if (list == null || list.Count < 2) return 0f;

        int count = list.Count;

        Vector2 prev = list[(index - 1 + count) % count];
        Vector2 current = list[index];
        Vector2 next15;

        if (index + lookaheadSteps >= list.Count) {
            int difference = lookaheadSteps - list.Count;
            next15 = waypoints[lookAheadIndex + difference];
        }
        else next15 = list[index + lookaheadSteps - 1];

        Vector2 dir1 = (current - prev).normalized;
        Vector2 dir2 = (next15 - current).normalized;

        float angle = Vector2.Angle(dir1, dir2);
        float normalisedAngle = angle / 90f; // Angle is usually between 0 - 90 degrees.
        return Mathf.Clamp01(normalisedAngle);
    }

    bool IsOnPath()
    {
        bool isNearMainLine = shortestDistance <= 1f && Mathf.Abs(LAP_Angle) <= 3f;

        // Check if car is aligned with main racing line's rotation
        bool isAligned = false;
        if (closestIndex >= 0 && closestIndex < racingLinePoints.Count)
        {
            Vector3 carForward = _car.rb.velocity.normalized;
            Vector3 lineForward = racingLinePoints[closestIndex].transform.forward;

            float angleDifference = Vector3.Angle(carForward, lineForward);
            isAligned = angleDifference <= 180f; // Allow up to 10 degrees mismatch
        }

        return isNearMainLine && isAligned;
    }
    bool IsOnCorrectedPath()
    {
        if (correctedLinePoints.Count == 0) return false;

        float closestCorrectedDistance = Mathf.Infinity;
        RacingLinePoint closestCorrected = null;

        foreach (var point in correctedLinePoints)
        {
            if (point == null) continue;

            float dist = Vector2.Distance(carPosition, new Vector2(point.transform.position.x, point.transform.position.z));
            if (dist < closestCorrectedDistance)
            {
                closestCorrectedDistance = dist;
                closestCorrected = point;
            }
        }

        if (closestCorrected == null) return false;

        // Check proximity and direction
        bool isClose = closestCorrectedDistance <= 1.2f;

        Vector3 carForward = _car.transform.forward;
        Vector3 correctedForward = closestCorrected.transform.forward;
        float angleDiff = Vector3.Angle(carForward, correctedForward);

        bool isAligned = angleDiff <= 10f;

        return isClose && isAligned;
    }

    void FindLookAheadPoint(Vector2 carPosition, float currentSpeed)
    {
        shortestDistance = Mathf.Infinity;
        closestIndex = -1;

        for (int i = 0; i < racingLinePoints.Count; i++)
        {
            var point = racingLinePoints[i];

            // Skip inactive points
            if (!point.gameObject.activeInHierarchy)
                continue;

            float distance = Vector2.Distance(carPosition, new Vector2(point.transform.position.x, point.transform.position.z));
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                closestWaypoint = new Vector2(point.transform.position.x, point.transform.position.z);
                closestIndex = i;
            }
        }

        if (closestIndex == -1) return;

        // LAP distance based on speed
        //lookAheadIndex = Mathf.Clamp(closestIndex + Mathf.RoundToInt(currentSpeed * lookAheadDistanceFactor), 0, waypoints.Count - 1);
        lookAheadIndex = closestIndex + 15;
        lookAheadPoint = waypoints[lookAheadIndex];
    }

    void CalculateData(int lookAheadIndex, float currentSpeed)
    {
        lookAheadPoint = waypoints[lookAheadIndex];
        LAP_Distance = Vector2.Distance(carPosition, lookAheadPoint);
        Vector2 carDirection = new Vector2(_car.rb.velocity.x, _car.rb.velocity.z).normalized;
        lookAheadVector = new Vector3(lookAheadPoint.x - carPosition.x, 0, lookAheadPoint.y - carPosition.y);
        LAP_Angle = Vector3.SignedAngle(_car.rb.velocity, lookAheadVector, Vector3.up);

        // Rule 2: Limit initial angle
        LAP_Angle = Mathf.Clamp(LAP_Angle, -maxInitialAngle, maxInitialAngle);

        // Rule 4 & 5: Determine initial angle and color based on racing line point
        float initialRotationAngle = 0f;
        Color correctedLineStartColor = Color.white;
        if (closestIndex >= 0 && closestIndex < racingLinePoints.Count)
        {
            initialRotationAngle = racingLinePoints[closestIndex].turnSeverity * maxInitialAngle; // Example: severity influences initial angle
            correctedLineStartColor = gradient.Evaluate(Mathf.Clamp01(racingLinePoints[closestIndex].turnSeverity));
        }

        // Rule 3: Adjust steps for smoother ending
        int steps = Mathf.Max(5, (int)Mathf.Round(LAP_Distance / pointDisplacementBase)); // Ensure at least a few steps

        DrawCurvedPath(carPosition, carDirection, LAP_Distance, steps, correctedLineStartColor, initialRotationAngle);
    }

    public void DrawCurvedPath(Vector2 startPos, Vector2 initialDir, float lapDistance, int steps, Color startColor, float initialRotation)
    {
        Vector2 currentPos = startPos;
        Vector2 dir = RotateVector(initialDir.normalized, initialRotation);
        Vector2 targetPos = lookAheadPoint;
        Vector2 previousDir = dir; // Store the previous direction for smoothing

        ClearCorrectedLine();
        correctedLinePoints.Clear();

        for (int i = 0; i < steps; i++)
        {
            Vector2 toTarget = (targetPos - currentPos).normalized;
            float angleToTarget = Vector2.SignedAngle(dir, toTarget);
            float rotationFactor = Mathf.Lerp(0.5f, tangentSmoothingFactor, (float)i / (steps - 1));
            float rawRotationAngle = angleToTarget * rotationFactor;

            // Apply damping to the rotation angle
            float smoothedRotationAngle = Mathf.Lerp(0f, rawRotationAngle, rotationDamping);
            dir = RotateVector(previousDir, smoothedRotationAngle); // Rotate from the previous direction
            previousDir = dir; // Update the previous direction

            float stepDistance = lapDistance / steps;
            currentPos += dir * stepDistance;

            Quaternion rotation = Quaternion.LookRotation(-1 * new Vector3(dir.x, 0, dir.y));
            var obj = Instantiate(_racingLinePrefab, new Vector3(currentPos.x, 0.18f, currentPos.y), rotation);
            correctedPointWaypoints.Add(currentPos);

            var rlp = obj.GetComponent<RacingLinePoint>();
            if (rlp != null)
            {
                rlp.SetActiveState(true);
                rlp.tag = "CorrectedLine";
                rlp.coordinates = currentPos;
                correctedLinePoints.Add(rlp);
            }
        }
        foreach (var obj in correctedLinePoints){
            int i = correctedPointWaypoints.FindIndex(wp => wp == obj.coordinates);
            var rlp = obj.GetComponent<RacingLinePoint>();
            float severity = ComputeTurnSeverity(correctedPointWaypoints, i); // Ensure you're passing correctedLinePoints
            rlp.turnSeverity = severity;
        }
    }
    public void ClearCorrectedLine()
    {
        foreach (var point in correctedLinePoints)
        {
            if (point != null)
                Destroy(point.gameObject);
        }
        correctedLinePoints.Clear();
    }


    void UpdatePointColors(List<RacingLinePoint> pointList, float speed)
    {
        foreach (var obj in pointList)
        {
            if (!obj.gameObject.activeInHierarchy) continue;
            RacingLinePoint point = obj;

            float severity = Mathf.Clamp01(point.turnSeverity * speed / 13f);
            Color color = gradient.Evaluate(severity);
            point.SetColor(color);
        }
    }

    private Vector2 RotateVector(Vector2 rotationVector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        return new Vector2(
            cos * rotationVector.x - sin * rotationVector.y,
            sin * rotationVector.x + cos * rotationVector.y
        ).normalized;
    }

    void AdvancedData(Vector2 closestWaypoint, Vector2 lookAheadPoint)
    {
        if (closestWaypointLine != null)
        {
            closestWaypointLine.enabled = true;
            closestWaypointLine.material = lineMaterial;
            closestWaypointLine.positionCount = 2;
            closestWaypointLine.startColor = Color.blue;
            closestWaypointLine.endColor = Color.blue;
            closestWaypointLine.startWidth = 0.05f;
            closestWaypointLine.endWidth = 0.05f;
            closestWaypointLine.SetPosition(0, new Vector3(carPosition.x, 0.2f, carPosition.y));
            closestWaypointLine.SetPosition(1, new Vector3(closestWaypoint.x, 0.2f, closestWaypoint.y));
        }

        if (lookAheadLine != null)
        {
            lookAheadLine.enabled = true;
            lookAheadLine.material = lineMaterial;
            lookAheadLine.positionCount = 2;
            lookAheadLine.startColor = Color.red;
            lookAheadLine.endColor = Color.red;
            lookAheadLine.startWidth = 0.05f;
            lookAheadLine.endWidth = 0.05f;
            lookAheadLine.SetPosition(0, new Vector3(carPosition.x, 0.2f, carPosition.y));
            lookAheadLine.SetPosition(1, new Vector3(lookAheadPoint.x, 0.2f, lookAheadPoint.y));
        }
    }
}