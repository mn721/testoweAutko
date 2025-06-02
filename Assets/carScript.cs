using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class carScript : MonoBehaviour
{
    public Rigidbody rigid;
    public WheelCollider FrontLeftWheel, FrontRightWheel, RearLeftWheel, RearRightWheel;

    public float drivespeed = 25000f;
    public float steerspeed = 50f;
    public float brakeForce = 60000f;
    public float handbrakeForce = 150000f;
    public float idleBrakeForce = 4000f;

    public float maxEngineRPM = 7000f;
    public float minEngineRPM = 1000f;
    private float engineRPM, targetEngineRPM, wheelRPM;
    private float clutchValue = 1f;
    private float clutchEngageSpeed = 2.5f;
    private float clutchDisengageTime = 0.25f;
    private float clutchTimer = 0f;
    private bool isClutchEngaged = true;
    private bool isRevLimiting = false;
    private float revLimiterCooldown = 0.2f;
    private float revLimiterTimer = 0f;
    public float maxTorqueRPM = 4000f;
    public float powerBandWidth = 2000f;
    public float lowEndTorqueMultiplier = 0.6f;

    public enum Gear { R = -1, N = 0, First = 1, Second, Third, Fourth, Fifth, Sixth }
    public Gear currentGear = Gear.N;
    public float[] gearRatios = { -3.2f, 0f, 3.5f, 2.2f, 1.5f, 1.1f, 0.9f, 0.7f };
    private float lastGearChangeTime = 0f;

    public enum DriveType { RWD, FWD, AWD }
    public DriveType currentDrive = DriveType.RWD;

    public TMP_Text driftPointsText;
    private float driftPoints = 0f;

    public Text _velocity;
    public Text _gear;
    public Text _rpm;
    public RectTransform rpmNeedle;
    public RectTransform speedNeedle;
    public float maxNeedleAngle = -220f;
    public float minNeedleAngle = 0f;

    public Transform referencePoint;
    public float distanceToReference;
    public float totalDistance;
    private Vector3 oldPosition;
    private float startTime;

    public float currentSpeed = 0f;
    public float averageSpeed = 0f;
    private float horizontalInput, verticalInput;
    private bool isHandBraking;
    private bool isBraking;

    void Start()
    {
        SetWheelFriction(FrontLeftWheel);
        SetWheelFriction(FrontRightWheel);
        SetWheelFriction(RearLeftWheel);
        SetWheelFriction(RearRightWheel);

        rigid.centerOfMass = new Vector3(0, -0.7f, -0.1f);
        rigid.linearDamping = 0.02f;
        rigid.angularDamping = 0.15f;

        SetupSuspension(FrontLeftWheel, true);
        SetupSuspension(FrontRightWheel, true);
        SetupSuspension(RearLeftWheel, false);
        SetupSuspension(RearRightWheel, false);

        oldPosition = transform.position;
        startTime = Time.time;
        ApplyIdleBrake();
    }

    void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        isHandBraking = Input.GetKey(KeyCode.Space);
        isBraking = Input.GetKey(KeyCode.S);

        if (Input.GetKeyDown(KeyCode.E) && currentGear < Gear.Sixth)
        {
            currentGear++;
            lastGearChangeTime = Time.time;
            isClutchEngaged = false;
            clutchTimer = clutchDisengageTime;
        }

        if (Input.GetKeyDown(KeyCode.Q) && currentGear > Gear.R)
        {
            currentGear--;
            lastGearChangeTime = Time.time;
            isClutchEngaged = false;
            clutchTimer = clutchDisengageTime;
        }

        if (Input.GetKeyDown(KeyCode.T))
            currentDrive = (DriveType)(((int)currentDrive + 1) % 3);

        if (!isClutchEngaged)
        {
            clutchTimer -= Time.deltaTime;
            if (clutchTimer <= 0f)
                isClutchEngaged = true;
        }

        totalDistance += Vector3.Distance(transform.position, oldPosition);
        oldPosition = transform.position;

        if (referencePoint != null)
            distanceToReference = Vector3.Distance(transform.position, referencePoint.position);

        float timePassed = Time.time - startTime;
        if (timePassed > 0.1f)
            averageSpeed = (totalDistance / timePassed) * 3.6f;
        else
            averageSpeed = 0f;

        updateGUI();
        UpdateNeedles();
    }

    void FixedUpdate()
    {
        currentSpeed = rigid.linearVelocity.magnitude * 3.6f;
        wheelRPM = (rigid.linearVelocity.magnitude / (2 * Mathf.PI * 0.34f)) * 60f;

        UpdateEngineRPM();
        rigid.AddForce(Vector3.down * 9.81f, ForceMode.Acceleration);
        ApplySpeedCompensatedSteering();

        float motor = 0f;

        if (Mathf.Abs(verticalInput) < 0.05f)
        {
            ResetWheelTorques();
            ApplyIdleBrake();
            return;
        }
        else
        {
            if (currentGear != Gear.N)
            {
                float gearRatio = gearRatios[(int)currentGear + 1];
                bool canMove = CanMoveFromCurrentGear(currentSpeed);

                if (canMove && engineRPM < maxEngineRPM && Mathf.Abs(verticalInput) > 0.1f)
                {
                    float rpmFactor = engineRPM < 0.8f * maxEngineRPM ? 1f :
                        Mathf.Clamp01(1f - (engineRPM - 0.8f * maxEngineRPM) / (0.2f * maxEngineRPM));

                    int gearIndex = Mathf.Max(1, Mathf.Abs((int)currentGear));
                    float maxSpeedForGear = 40f * gearIndex;
                    float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeedForGear);
                    float gearEfficiency = Mathf.Lerp(1f, 0.3f, speedRatio);

                    float throttleResponse = Mathf.Pow(Mathf.Abs(verticalInput), 1.5f);
                    float torqueFactor = GetTorqueFactorFromRPM();

                    motor = throttleResponse * drivespeed * Mathf.Sign(gearRatio) * clutchValue *
                            rpmFactor * gearEfficiency * torqueFactor * 0.8f;
                }
            }

            ApplyDriveTorque(motor);
        }

        AdjustRearTractionForPowerDrift();
        FrontLeftWheel.steerAngle = steerspeed * horizontalInput;
        FrontRightWheel.steerAngle = steerspeed * horizontalInput;
        ApplyBrakes();

        if (verticalInput > 0.6f && currentSpeed > 30f)
        {
            float torqueAmount = horizontalInput * 1000f;
            rigid.AddTorque(transform.up * torqueAmount);
        }

        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0f);

        AntiRoll(FrontLeftWheel, FrontRightWheel, 4000f);
        AntiRoll(RearLeftWheel, RearRightWheel, 4000f);

        CalculateDriftPoints();
        SetupSuspension(FrontLeftWheel, true);
        SetupSuspension(FrontRightWheel, true);
        SetupSuspension(RearLeftWheel, false);
        SetupSuspension(RearRightWheel, false);

        ApplyDragForces();
        ApplyDownforceAndStability();
    }

    void ResetWheelTorques()
    {
        FrontLeftWheel.motorTorque = 0f;
        FrontRightWheel.motorTorque = 0f;
        RearLeftWheel.motorTorque = 0f;
        RearRightWheel.motorTorque = 0f;
    }

    void ApplyIdleBrake()
    {
        if (currentSpeed < 0.5f)
        {
            FrontLeftWheel.brakeTorque = idleBrakeForce * 2;
            FrontRightWheel.brakeTorque = idleBrakeForce * 2;
            RearLeftWheel.brakeTorque = idleBrakeForce * 2;
            RearRightWheel.brakeTorque = idleBrakeForce * 2;
        }
        else
        {
            FrontLeftWheel.brakeTorque = idleBrakeForce;
            FrontRightWheel.brakeTorque = idleBrakeForce;
            RearLeftWheel.brakeTorque = idleBrakeForce;
            RearRightWheel.brakeTorque = idleBrakeForce;
        }
    }

    void UpdateEngineRPM()
    {
        if (currentGear == Gear.N)
        {
            if (Mathf.Abs(verticalInput) > 0.05f)
                targetEngineRPM = Mathf.Lerp(targetEngineRPM, minEngineRPM + verticalInput * (maxEngineRPM - minEngineRPM), Time.deltaTime * 5f);
            else
                targetEngineRPM = Mathf.Lerp(targetEngineRPM, minEngineRPM, Time.deltaTime * 2f);
        }
        else
        {
            float gearRatio = gearRatios[(int)currentGear + 1];
            float wheelBasedRPM = Mathf.Abs(wheelRPM * gearRatio);

            if (Mathf.Abs(verticalInput) > 0.05f)
                targetEngineRPM = Mathf.Lerp(wheelBasedRPM, maxEngineRPM, Mathf.Clamp01(verticalInput));
            else
                targetEngineRPM = wheelBasedRPM;
        }

        if (engineRPM >= maxEngineRPM && verticalInput > 0.1f)
        {
            isRevLimiting = true;
            revLimiterTimer = revLimiterCooldown;
        }

        if (isRevLimiting)
        {
            revLimiterTimer -= Time.deltaTime;
            if (revLimiterTimer <= 0f)
                isRevLimiting = false;
        }

        float limiterEffect = isRevLimiting ? 0f : 1f;
        clutchValue = Mathf.MoveTowards(clutchValue, isClutchEngaged ? 1f : 0f, Time.deltaTime * clutchEngageSpeed);
        float rpmChangeSpeed = isClutchEngaged ? 2.0f : 5.0f;
        engineRPM = Mathf.Lerp(engineRPM, targetEngineRPM * clutchValue * limiterEffect, Time.deltaTime * rpmChangeSpeed);
        engineRPM = Mathf.Clamp(engineRPM, minEngineRPM, maxEngineRPM + 1000f);

        if (currentGear == Gear.N && Mathf.Abs(verticalInput) < 0.05f)
            targetEngineRPM = minEngineRPM + Mathf.Sin(Time.time * 5f) * 50f;
    }

    float GetTorqueFactorFromRPM()
    {
        float rpmNormalized = Mathf.InverseLerp(minEngineRPM, maxEngineRPM, engineRPM);
        float peakRPMNormalized = maxTorqueRPM / maxEngineRPM;
        float distanceFromPeak = Mathf.Abs(rpmNormalized - peakRPMNormalized);
        float bandWidthNormalized = (powerBandWidth / 2f) / maxEngineRPM;

        float torqueFactor;
        if (distanceFromPeak <= bandWidthNormalized)
        {
            torqueFactor = 1f;
        }
        else if (rpmNormalized < peakRPMNormalized)
        {
            float lowEndFactor = rpmNormalized / peakRPMNormalized;
            torqueFactor = Mathf.Lerp(lowEndTorqueMultiplier, 1f, lowEndFactor);
        }
        else
        {
            float highEndFactor = (1f - rpmNormalized) / (1f - peakRPMNormalized);
            torqueFactor = Mathf.Lerp(0.5f, 1f, highEndFactor);
        }

        return Mathf.Clamp01(torqueFactor);
    }

    void ApplyDriveTorque(float motor)
    {
        if (Mathf.Abs(verticalInput) < 0.05f)
        {
            ResetWheelTorques();
            return;
        }

        if (currentDrive == DriveType.RWD || currentDrive == DriveType.AWD)
        {
            RearLeftWheel.motorTorque = motor;
            RearRightWheel.motorTorque = motor;
        }

        if (currentDrive == DriveType.FWD || currentDrive == DriveType.AWD)
        {
            FrontLeftWheel.motorTorque = motor;
            FrontRightWheel.motorTorque = motor;
        }
    }

    void ApplyBrakes()
    {
        bool isCoasting = Mathf.Abs(verticalInput) < 0.1f && currentSpeed > 1f;

        if (isCoasting)
        {
            ApplyIdleBrake();
            return;
        }

        if (isBraking)
        {
            float appliedBrake = brakeForce;
            FrontLeftWheel.brakeTorque = appliedBrake;
            FrontRightWheel.brakeTorque = appliedBrake;
            RearLeftWheel.brakeTorque = appliedBrake;
            RearRightWheel.brakeTorque = appliedBrake;
        }
        else if (Mathf.Abs(verticalInput) > 0.05f)
        {
            FrontLeftWheel.brakeTorque = 0f;
            FrontRightWheel.brakeTorque = 0f;
            RearLeftWheel.brakeTorque = 0f;
            RearRightWheel.brakeTorque = 0f;
        }
        else
        {
            ApplyIdleBrake();
        }

        if (isHandBraking)
        {
            SetWheelDriftMode(RearLeftWheel, true);
            SetWheelDriftMode(RearRightWheel, true);
        }
        else
        {
            SetWheelDriftMode(RearLeftWheel, false);
            SetWheelDriftMode(RearRightWheel, false);
        }
    }

    void SetWheelDriftMode(WheelCollider wheel, bool drifting)
    {
        WheelFrictionCurve sideways = wheel.sidewaysFriction;
        sideways.stiffness = drifting ? 0.3f : 1.0f;
        wheel.sidewaysFriction = sideways;
    }

    void AdjustRearTractionForPowerDrift()
    {
        WheelFrictionCurve frictionL = RearLeftWheel.sidewaysFriction;
        WheelFrictionCurve frictionR = RearRightWheel.sidewaysFriction;

        bool isAggressiveThrottle = verticalInput > 0.9f;
        bool isSharpTurning = Mathf.Abs(horizontalInput) > 0.7f;
        bool shouldReduceGrip = isAggressiveThrottle && isSharpTurning && currentSpeed > 30f && currentSpeed < 100f;

        if (currentSpeed > 100f)
        {
            frictionL.stiffness = Mathf.Lerp(frictionL.stiffness, 4.0f, Time.deltaTime * 3f);
            frictionR.stiffness = Mathf.Lerp(frictionR.stiffness, 4.0f, Time.deltaTime * 3f);
        }
        else if (currentSpeed > 60f)
        {
            frictionL.stiffness = Mathf.Lerp(frictionL.stiffness, 3.5f, Time.deltaTime * 2f);
            frictionR.stiffness = Mathf.Lerp(frictionR.stiffness, 3.5f, Time.deltaTime * 2f);
        }
        else
        {
            float targetStiffness = shouldReduceGrip ? 1.8f : 2.5f;
            frictionL.stiffness = Mathf.Lerp(frictionL.stiffness, targetStiffness, Time.deltaTime * 2f);
            frictionR.stiffness = Mathf.Lerp(frictionR.stiffness, targetStiffness, Time.deltaTime * 2f);
        }

        RearLeftWheel.sidewaysFriction = frictionL;
        RearRightWheel.sidewaysFriction = frictionR;
    }

    void SetWheelFriction(WheelCollider wheel)
    {
        WheelFrictionCurve forward = wheel.forwardFriction;
        WheelFrictionCurve sideways = wheel.sidewaysFriction;

        bool isRear = (wheel == RearLeftWheel || wheel == RearRightWheel);

        forward.stiffness = isRear ? 2.0f : 2.5f;
        sideways.stiffness = isRear ? 2.0f : 3.0f;

        forward.extremumSlip = 0.4f;
        forward.extremumValue = 1.0f;
        forward.asymptoteSlip = 0.8f;
        forward.asymptoteValue = 0.5f;

        sideways.extremumSlip = 0.3f;
        sideways.extremumValue = 1.0f;
        sideways.asymptoteSlip = 0.6f;
        sideways.asymptoteValue = 0.75f;

        wheel.forwardFriction = forward;
        wheel.sidewaysFriction = sideways;
    }

    void ApplyDownforceAndStability()
    {
        float speedFactor = Mathf.Clamp01(currentSpeed / 150f);
        float baseDownforce = currentSpeed * 20f;
        float additionalDownforce = speedFactor * currentSpeed * 40f;

        rigid.AddForce(-transform.up * (baseDownforce + additionalDownforce));

        if (currentSpeed > 80f)
        {
            float speedOverLimit = currentSpeed - 80f;
            float maxSpeedForCalculation = 200f;
            float reductionFactor = Mathf.Clamp01(speedOverLimit / (maxSpeedForCalculation - 80f));
            float steerReduction = Mathf.Lerp(1f, 0.4f, reductionFactor);

            if (!isBraking && !isHandBraking)
            {
                FrontLeftWheel.steerAngle *= steerReduction;
                FrontRightWheel.steerAngle *= steerReduction;
            }

            rigid.angularDamping = Mathf.Lerp(0.15f, 0.4f, speedFactor);
        }
        else
        {
            rigid.angularDamping = 0.15f;
        }
    }

    void ApplySpeedCompensatedSteering()
    {
        float baseSteering = steerspeed * horizontalInput;

        if (currentSpeed > 60f)
        {
            float precisionFactor = Mathf.Clamp(1f + (currentSpeed - 60f) / 200f, 1f, 2f);
            baseSteering *= precisionFactor;
            baseSteering = Mathf.Clamp(baseSteering, -40f, 40f);
        }

        FrontLeftWheel.steerAngle = baseSteering;
        FrontRightWheel.steerAngle = baseSteering;
    }

    void ApplyDragForces()
    {
        float airDragCoeff = 0.8f;
        float airDrag = airDragCoeff * currentSpeed * currentSpeed * 0.01f;
        rigid.AddForce(-rigid.linearVelocity.normalized * airDrag);

        float rollingResistance = 800f;
        if (currentSpeed > 1f)
            rigid.AddForce(-rigid.linearVelocity.normalized * rollingResistance);
    }

    void SetupSuspension(WheelCollider wheel, bool isFront)
    {
        JointSpring spring = wheel.suspensionSpring;
        spring.spring = isFront ? 100000f : 60000f;
        spring.damper = isFront ? 15000f : 10000f;
        spring.targetPosition = 0.5f;

        wheel.suspensionSpring = spring;
        wheel.suspensionDistance = 0.25f;

        WheelFrictionCurve forward = wheel.forwardFriction;
        WheelFrictionCurve sideways = wheel.sidewaysFriction;

        forward.stiffness = isFront ? 2.5f : 2.0f;
        sideways.stiffness = isFront ? 3.0f : 2.0f;

        wheel.forwardFriction = forward;
        wheel.sidewaysFriction = sideways;
    }

    void AntiRoll(WheelCollider leftWheel, WheelCollider rightWheel, float antiRollForceMultiplier)
    {
        WheelHit hit;
        float travelLeft = 1f;
        float travelRight = 1f;

        bool groundedLeft = leftWheel.GetGroundHit(out hit);
        if (groundedLeft)
            travelLeft = (-leftWheel.transform.InverseTransformPoint(hit.point).y - leftWheel.radius) / leftWheel.suspensionDistance;

        bool groundedRight = rightWheel.GetGroundHit(out hit);
        if (groundedRight)
            travelRight = (-rightWheel.transform.InverseTransformPoint(hit.point).y - rightWheel.radius) / rightWheel.suspensionDistance;

        float antiRollForce = (travelLeft - travelRight) * antiRollForceMultiplier;

        if (currentSpeed < 0.5f)
            return;

        if (groundedLeft)
            rigid.AddForceAtPosition(leftWheel.transform.up * -antiRollForce, leftWheel.transform.position);

        if (groundedRight)
            rigid.AddForceAtPosition(rightWheel.transform.up * antiRollForce, rightWheel.transform.position);
    }

    void CalculateDriftPoints()
    {
        float angle = Vector3.Angle(transform.forward, rigid.linearVelocity);
        if (angle > 10f && rigid.linearVelocity.magnitude > 5f)
        {
            driftPoints += angle * Time.deltaTime;
            if (driftPointsText) driftPointsText.text = "Drift Points: " + ((int)driftPoints).ToString();
        }
    }

    bool CanMoveFromCurrentGear(float speedKmh)
    {
        if (currentGear == Gear.R || currentGear == Gear.First)
            return true;

        return speedKmh >= 10f || Mathf.Abs(verticalInput) > 0.1f;
    }

    void updateGUI()
    {
        float speed = rigid.linearVelocity.magnitude * 3.6f;
        _velocity.text = "Predkosc: " + speed.ToString("F1") + " km/h";
        _gear.text = "Bieg: " + currentGear.ToString();
        _rpm.text = "RPM: " + engineRPM.ToString("F0");
    }

    void UpdateNeedles()
    {
        float rpmNormalized = Mathf.InverseLerp(minEngineRPM, maxEngineRPM, engineRPM);
        float rpmAngle = Mathf.Lerp(minNeedleAngle, maxNeedleAngle, rpmNormalized);
        rpmNeedle.localRotation = Quaternion.Euler(0, 0, rpmAngle);

        float speedNormalized = Mathf.InverseLerp(0, 240, currentSpeed);
        float speedAngle = Mathf.Lerp(minNeedleAngle, maxNeedleAngle, speedNormalized);
        speedNeedle.localRotation = Quaternion.Euler(0, 0, speedAngle);
    }
}