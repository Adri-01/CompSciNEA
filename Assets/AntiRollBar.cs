using UnityEngine;

public class AntiRollBar : MonoBehaviour 
{
    public WheelCollider WheelL;
    public WheelCollider WheelR;
    public float AntiRollStrength = 5000f;  // Adjusted for smoother response
    public float DampingFactor = 0.1f;  // Helps reduce jittering

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float travelL = 1.0f;
        float travelR = 1.0f;
        WheelHit hit;

        bool groundedL = WheelL.GetGroundHit(out hit);
        if (groundedL)
        {
            travelL = (-WheelL.transform.InverseTransformPoint(hit.point).y - WheelL.radius) / WheelL.suspensionDistance;
        }

        bool groundedR = WheelR.GetGroundHit(out hit);
        if (groundedR)
        {
            travelR = (-WheelR.transform.InverseTransformPoint(hit.point).y - WheelR.radius) / WheelR.suspensionDistance;
        }

        if (groundedL && groundedR)
        {
            float antiRollForce = (travelL - travelR) * AntiRollStrength;

            // Apply damping to smooth out force application
            antiRollForce = Mathf.Lerp(0, antiRollForce, 1 - DampingFactor);

            rb.AddForceAtPosition(WheelL.transform.up * -antiRollForce / 2f, WheelL.transform.position);
            rb.AddForceAtPosition(WheelR.transform.up * antiRollForce / 2f, WheelR.transform.position);
        }
    }
}
