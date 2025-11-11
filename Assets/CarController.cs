using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.iOS;

[RequireComponent(typeof(InputManager))]
[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour{
    public InputManager im;

    public AnimationCurve torqueCurve;

    public List<WheelCollider> frontWheelColliders;
    public List<WheelCollider> throttleWheels;

    public List<GameObject> steeringWheels;
    public List<GameObject> frontWheelMeshes;
    public List<GameObject> rearWheelMeshes;
    private float minRPM = 1000f;
    public float maxRPM = 7000f;
    private float finalDriveRatio = 3.91f;
    private float[] gearRatios = {3.2f, 2.2f, 1.6f, 1.3f, 1.155f, 0.84f};
    private float[] maxSpeed = {18f, 34.72f, 47.78f, 52.8f, 57.5f, 80.56f};
    public int _currentGearIndex = 0;
    public float engineRPM = 1000f;
    private bool reverse;
    private float lastShiftTime = 0f;
    public float shiftCooldown = 0.75f;
    public float _velocity;
    private float forwardSpeed;
    public Vector2 position;
    public float speed;
    private float wheelBase = 2.917f;
    private float trackWidth = 1.8f;
    private float engineTorque;
    public float engineBrakingStrength = 100f;
    private float _RearWheelRadius = 0.3625f;
    private float maxSteerAngle = 30f;
    public Transform CM;
    public Rigidbody rb;
    public float brakeForce = 2500f; 

    void Start()
    {
        im = GetComponent<InputManager>();
        rb = GetComponent<Rigidbody>();
        if (CM){
            rb.centerOfMass = CM.position;
        }
    }

    void Update()
    {
        string transmissionType = im.transmissionType;
        if (transmissionType == "Manual") manualShifting();
        else autoShifting();
    }

    void FixedUpdate()
    {
        forwardSpeed = Vector3.Dot(rb.velocity, rb.transform.forward);
        _velocity = rb.velocity.magnitude;
        speed = MathF.Abs(_velocity);
        position = new Vector2 (rb.position.x, rb.position.z);

        if (!reverse){
            if (im.throttle >= im.brake)
            {
                CalculateEngineRPM(im.throttle - im.brake, false);
                engineTorque = torqueCurve.Evaluate(engineRPM);

                if (im.throttle > 0f)
                {
                    accelerate(im.throttle - im.brake);
                }
                else
                {
                    engineBreak();
                }
            }
            else
            {
                CalculateEngineRPM(0f, false);
                brake(im.brake - im.throttle);
            }
        }
        else{
            CalculateEngineRPM(im.brake, true);
            engineTorque = torqueCurve.Evaluate(engineRPM);
        }

        if (speed > 0.01f)  // Prevent division by zero
        {
            float Cd = 0.36f;  // Drag coefficient
            float A = 2f;   // Frontal area (m²)
            float rho = 1.225f; // Air density (kg/m³)

            float _airResistance = 0.5f * rho * Cd * A * (_velocity * _velocity);

            float Crr = 0.015f;  // Rolling resistance coefficient
            float rollingResistance = Crr * rb.mass * 9.81f;  // F_rolling = Crr * mg

            // Apply drag force in the opposite direction of velocity
            rb.AddForce(-rb.velocity.normalized * rollingResistance + -rb.velocity.normalized * _airResistance);
        }


        GameObject frontLeftWheel = steeringWheels[0];
        GameObject frontRightWheel = steeringWheels[1];

        WheelCollider frontLeftCollider = frontLeftWheel.GetComponent<WheelCollider>();
        WheelCollider frontRightCollider = frontRightWheel.GetComponent<WheelCollider>();

        if (Mathf.Abs(im.steering) > 0.01f)
        {
            float steerAngle = im.steering * maxSteerAngle;
            float turnRadius = wheelBase / Mathf.Tan(Mathf.Abs(steerAngle) * Mathf.Deg2Rad);

            float innerAngle = Mathf.Atan(wheelBase / (turnRadius - (trackWidth / 2))) * Mathf.Rad2Deg;
            float outerAngle = Mathf.Atan(wheelBase / (turnRadius + (trackWidth / 2))) * Mathf.Rad2Deg;

            if (im.steering > 0) // Turning right
            {
                frontLeftCollider.steerAngle = innerAngle;
                frontRightCollider.steerAngle = outerAngle;
            }
            if (im.steering < 0) // Turning left 
            {
                frontLeftCollider.steerAngle = -outerAngle; // Negate angles to switch the rotation dirrection
                frontRightCollider.steerAngle = -innerAngle; // Negate angles to switch the rotation dirrection
            }

            // Update visual rotation
            frontLeftWheel.transform.localEulerAngles = new Vector3(0f, frontLeftCollider.steerAngle, 0f);
            frontRightWheel.transform.localEulerAngles = new Vector3(0f, frontRightCollider.steerAngle, 0f);
        }
        else
        {
            frontLeftCollider.steerAngle = 0f;
            frontRightCollider.steerAngle = 0f;
        }

        foreach (WheelCollider wheel in frontWheelColliders){
            float wheelRPM = wheel.rpm;
            float rotationPerSecond = wheelRPM * Time.deltaTime; 
            foreach (GameObject mesh in frontWheelMeshes){
                mesh.transform.Rotate(rotationPerSecond,0f, 0f);
            }
        }
        foreach (WheelCollider wheel in throttleWheels){
            float wheelRPM = wheel.rpm;
            float rotationPerSecond = wheelRPM * Time.deltaTime; 
            foreach (GameObject mesh in rearWheelMeshes){
                mesh.transform.Rotate(rotationPerSecond,0f, 0f);
            }
        }
    }

    void CalculateEngineRPM(float throttleInput, bool reverse){
        float targetRPM = Mathf.Lerp(minRPM, maxRPM, throttleInput);
        if (!reverse)
        {
            float wheelRPM = speed / (2 * Mathf.PI * _RearWheelRadius) * gearRatios[_currentGearIndex] * finalDriveRatio * 60f;
            engineRPM = Mathf.Lerp(wheelRPM, targetRPM, Time.deltaTime * ((maxRPM - engineRPM) / maxRPM));
        }
        else {
            float wheelRPM = speed / (2 * Mathf.PI * _RearWheelRadius) * finalDriveRatio * 60f;
            engineRPM = Mathf.Lerp(wheelRPM, targetRPM, Time.deltaTime * ((maxRPM - engineRPM) / maxRPM));
        }
        engineRPM = Mathf.Clamp(engineRPM, minRPM, maxRPM);
    }

    void manualShifting(){
        if (im.shiftUp) ShiftUp();
        else if (im.shiftDown) ShiftDown();
    }

    void autoShifting(){
        if (forwardSpeed <= 0 && im.brake > 0 && im.brake > im.throttle){
            reverse = true;
            if (im.throttle == 0) Reverse(1);
            else Reverse(im.brake - im.throttle);
        }
        else reverse = false;

        float currentTime = Time.time;

        if (!reverse && currentTime - lastShiftTime > shiftCooldown)
        {
            // Upshift
            if (engineRPM >= 6800f) {
                if (_currentGearIndex < gearRatios.Length - 1) {
                    ShiftUp();
                    lastShiftTime = currentTime;
                }
            }
            // Downshift
            else if (engineRPM <= 4000f) {
                if (_currentGearIndex > 0) {
                    ShiftDown();
                    lastShiftTime = currentTime;
                }
            }
        }
    }

    void ShiftUp()
    {
        if (_currentGearIndex < gearRatios.Length - 1) _currentGearIndex++;
    }

    void ShiftDown()
    {
        if (_currentGearIndex > 0) _currentGearIndex--; // |Decrease RPM|;
    }

    void accelerate(float throttleInput)
    {
        if (_velocity < maxSpeed[_currentGearIndex]){
            foreach (WheelCollider wheel in throttleWheels){
            float wheelTorque = engineTorque * finalDriveRatio * 2f * gearRatios[_currentGearIndex];
            wheel.motorTorque = wheelTorque * throttleInput / throttleWheels.Count;
            wheel.brakeTorque = 0f;
            }
        }
        else{
            foreach (WheelCollider wheel in throttleWheels){
                wheel.brakeTorque = 0f;
                wheel.motorTorque = 0f;
            }
        }
        foreach (WheelCollider wheel in frontWheelColliders){
            wheel.brakeTorque = 0f;
        }
    }
    void Reverse(float throttleInput){
        reverse = true;
        if (speed <= 10f){
            foreach (WheelCollider wheel in throttleWheels){
            float wheelTorque = engineTorque * finalDriveRatio * -2f;
            wheel.motorTorque = wheelTorque * throttleInput / throttleWheels.Count;
            wheel.brakeTorque = 0f;
            }
        }
        else{
            foreach (WheelCollider wheel in throttleWheels){
                wheel.brakeTorque = 0f;
                wheel.motorTorque = 0f;
            }
        }
        foreach (WheelCollider wheel in frontWheelColliders){
            wheel.brakeTorque = 0f;
    }
    }

    void brake(float brakeInput)
    {
        float frontBias = 0.7f; 
        float rearBias = 1.0f - frontBias;

        foreach (WheelCollider wheel in throttleWheels)
        {
            wheel.motorTorque = 0f;
            float slip;
            bool isGrounded = false;
            WheelHit hit;

            if (wheel.GetGroundHit(out hit))
            {
                isGrounded = true;
                slip = Mathf.Abs(hit.forwardSlip); // Forward slip value checks by how much the wheel colliders are slipping 
            }
            else
            {
                slip = 0f;
            }

            // ABS
            float absBrakeForce;
            if (isGrounded && slip > 0.4f)
            {
                absBrakeForce = brakeForce * 0.3f;
            }
            else
            {
                absBrakeForce = brakeForce;
            }

            wheel.brakeTorque = absBrakeForce * brakeInput * rearBias / throttleWheels.Count;
        }

        foreach (WheelCollider wheel in frontWheelColliders)
        {
            float slip;
            bool isGrounded = false;
            WheelHit hit;

            if (wheel.GetGroundHit(out hit))
            {
                isGrounded = true;
                slip = Mathf.Abs(hit.forwardSlip);
            }
            else
            {
                slip = 0f;
            }

            float absBrakeForce;
            if (isGrounded && slip > 0.4f)
            {
                absBrakeForce = brakeForce * 0.2f;
            }
            else
            {
                absBrakeForce = brakeForce;
            }

            wheel.brakeTorque = absBrakeForce * brakeInput * frontBias / frontWheelColliders.Count;
        }
    }

    
    void engineBreak()
    {
        foreach (WheelCollider wheel in throttleWheels){
            wheel.brakeTorque = 0f;
            wheel.motorTorque = 0f;  // Default to 0

            if (_velocity >= 2){
                float brakeTorque = -engineBrakingStrength * engineRPM / maxRPM / throttleWheels.Count;

                // Only apply if it's actually a resistive force (not pushing)
                if (brakeTorque < 0){
                    wheel.motorTorque = brakeTorque;
                }
            }
        }
        foreach (WheelCollider wheel in frontWheelColliders){
            wheel.brakeTorque = 0f;
        }
    }
}