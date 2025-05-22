using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class carScript : MonoBehaviour
{
    //Podzespoły
    public Rigidbody rigid;
    public WheelCollider FrontLeftWheel, FrontRightWheel, RearLeftWheel, RearRightWheel;

    //Sterowanie
    public float drivespeed = 50000f;
    public float steerspeed = 50f;
    public float brakeForce = 60000f;
    public float handbrakeForce = 150000f;
    public float idleBrakeForce = 4000f; // <-- Nowy parametr hamowania na luzie

    //Silnik i sprzęgło
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

    //Biegi
    public enum Gear { R = -1, N = 0, First = 1, Second, Third, Fourth, Fifth, Sixth }
    public Gear currentGear = Gear.N;
    public float[] gearRatios = { -3.2f, 0f, 4.0f, 2.6f, 1.8f, 1.3f, 1.0f, 0.8f };
    private float lastGearChangeTime = 0f;

    //Napęd
    public enum DriveType { RWD, FWD, AWD }
    public DriveType currentDrive = DriveType.RWD;

    //Drift
    public TMP_Text driftPointsText;
    private float driftPoints = 0f;

    //GUI
    public Text _velocity;
    public Text _gear;
    public Text _rpm;
    public TMP_Text _driftAngle;
    public TMP_Text _input; 

    //Inne
    public float currentSpeed = 0f;
    private float horizontalInput, verticalInput;
    private bool isHandBraking;
    private bool isBraking;

    void Start()
    {
        SetWheelFriction(FrontLeftWheel);
        SetWheelFriction(FrontRightWheel);
        SetWheelFriction(RearLeftWheel);
        SetWheelFriction(RearRightWheel);

        rigid.centerOfMass = new Vector3(0, -0.5f, -0.3f);

        rigid.linearDamping = 0.05f;
        rigid.angularDamping = 0.1f;

        SetupSuspension(FrontLeftWheel, true);
        SetupSuspension(FrontRightWheel, true);
        SetupSuspension(RearLeftWheel, false);
        SetupSuspension(RearRightWheel, false);

        // Zastosuj hamulce na początku, żeby auto nie jechało
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

        updateGUI();
    }

    void FixedUpdate()
    {
        currentSpeed = rigid.linearVelocity.magnitude * 3.6f;
        wheelRPM = (rigid.linearVelocity.magnitude / (2 * Mathf.PI * 0.34f)) * 60f;

        UpdateEngineRPM();

        // Zmniejszone siły grawitacji lub siła odpowiednia dla pojazdu
        rigid.AddForce(Vector3.down * 9.81f, ForceMode.Acceleration);

        float motor = 0f;

        // Resetuj napęd kół przy braku wejścia
        if (Mathf.Abs(verticalInput) < 0.05f)
        {
            ResetWheelTorques();
            ApplyIdleBrake();
            return;
        }

        if (currentGear != Gear.N)
        {
            float gearRatio = gearRatios[(int)currentGear + 1];
            bool canMove = CanMoveFromCurrentGear(currentSpeed);

            if (canMove && engineRPM < maxEngineRPM && Mathf.Abs(verticalInput) > 0.1f)
            {
                float rpmFactor = engineRPM < 0.85f * maxEngineRPM ? 1f :
                    Mathf.Clamp01(1f - (engineRPM - 0.85f * maxEngineRPM) / (0.15f * maxEngineRPM));

                int gearIndex = Mathf.Max(1, Mathf.Abs((int)currentGear));
                float speedRatio = Mathf.Clamp01(currentSpeed / (30f * gearIndex));
                float gearRatioSpeedFactor = Mathf.Lerp(1f, 0.4f, 1f - speedRatio);

                float torqueFactor = GetTorqueFactorFromRPM();
                motor = verticalInput * drivespeed * Mathf.Sign(gearRatio) * clutchValue * rpmFactor * gearRatioSpeedFactor * torqueFactor;
            }
        }

        ApplyDriveTorque(motor);
        AdjustRearTractionForPowerDrift();

        FrontLeftWheel.steerAngle = steerspeed * horizontalInput;
        FrontRightWheel.steerAngle = steerspeed * horizontalInput;

        ApplyBrakes();

        if (verticalInput > 0.6f && currentSpeed > 30f)
        {
            float torqueAmount = horizontalInput * 1000f;
            rigid.AddTorque(transform.up * torqueAmount);
        }

        FrontLeftWheel.steerAngle = steerspeed * horizontalInput;
        FrontRightWheel.steerAngle = steerspeed * horizontalInput;

        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0f);

        // Zmniejszona siła anti-roll
        AntiRoll(FrontLeftWheel, FrontRightWheel, 4000f);
        AntiRoll(RearLeftWheel, RearRightWheel, 4000f);

        CalculateDriftPoints();

        SetupSuspension(FrontLeftWheel, true);
        SetupSuspension(FrontRightWheel, true);
        SetupSuspension(RearLeftWheel, false);
        SetupSuspension(RearRightWheel, false);

        float downforce = currentSpeed * 30f;
        rigid.AddForce(-transform.up * downforce);
    }

    // Nowa metoda resetowania napędu kół
    void ResetWheelTorques()
    {
        FrontLeftWheel.motorTorque = 0f;
        FrontRightWheel.motorTorque = 0f;
        RearLeftWheel.motorTorque = 0f;
        RearRightWheel.motorTorque = 0f;
    }

    // Nowa metoda hamowania na luzie
    void ApplyIdleBrake()
    {
        // Hamowanie na luzie - zatrzyma auto, gdy nie ma inputu
        if (currentSpeed < 0.5f)
        {
            // Mocniejsze hamowanie dla zatrzymania pojazdu
            FrontLeftWheel.brakeTorque = idleBrakeForce * 2;
            FrontRightWheel.brakeTorque = idleBrakeForce * 2;
            RearLeftWheel.brakeTorque = idleBrakeForce * 2;
            RearRightWheel.brakeTorque = idleBrakeForce * 2;
        }
        else
        {
            // Standardowe hamowanie przy jeździe bez gazu
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

            // Nie przyśpieszaj automatycznie bez wciśniętego gazu
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
        float rpmChangeSpeed = isClutchEngaged ? 2.0f : 5.0f; // wolniej rośnie gdy sprzęgło wciśnięte
        engineRPM = Mathf.Lerp(engineRPM, targetEngineRPM * clutchValue * limiterEffect, Time.deltaTime * rpmChangeSpeed);

        engineRPM = Mathf.Clamp(engineRPM, minEngineRPM, maxEngineRPM + 1000f);

        if (currentGear == Gear.N && Mathf.Abs(verticalInput) < 0.05f)
            targetEngineRPM = minEngineRPM + Mathf.Sin(Time.time * 5f) * 50f;
    }

    float GetTorqueFactorFromRPM()
    {
        float rpmNormalized = Mathf.InverseLerp(minEngineRPM, maxEngineRPM, engineRPM);
        return Mathf.Clamp01(Mathf.Sin(rpmNormalized * Mathf.PI)); // krzywa sinusoidalna
    }

    void ApplyDriveTorque(float motor)
    {
        // Nie stosuj napędu przy braku inputu
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

        // Automatyczne hamowanie przy braku gazu
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
            // Zwolnij hamulce tylko gdy wciśnięty jest gaz
            FrontLeftWheel.brakeTorque = 0f;
            FrontRightWheel.brakeTorque = 0f;
            RearLeftWheel.brakeTorque = 0f;
            RearRightWheel.brakeTorque = 0f;
        }
        else
        {
            // Zastosuj hamulce przy braku inputu
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
        if (currentSpeed > 120f)
            return;

        bool isAggressiveThrottle = verticalInput > 0.9f;
        bool isSharpTurning = Mathf.Abs(horizontalInput) > 0.5f;
        bool shouldReduceGrip = isAggressiveThrottle && isSharpTurning && currentSpeed > 40f;

        WheelFrictionCurve frictionL = RearLeftWheel.sidewaysFriction;
        WheelFrictionCurve frictionR = RearRightWheel.sidewaysFriction;

        float targetStiffness = shouldReduceGrip ? 1.35f : 1.5f;
        float lerpSpeed = 1.5f;

        frictionL.stiffness = Mathf.Max(1.25f, Mathf.Lerp(frictionL.stiffness, targetStiffness, Time.deltaTime * lerpSpeed));
        frictionR.stiffness = Mathf.Max(1.25f, Mathf.Lerp(frictionR.stiffness, targetStiffness, Time.deltaTime * lerpSpeed));

        RearLeftWheel.sidewaysFriction = frictionL;
        RearRightWheel.sidewaysFriction = frictionR;
    }

    void SetWheelFriction(WheelCollider wheel)
    {
        WheelFrictionCurve forward = wheel.forwardFriction;
        WheelFrictionCurve sideways = wheel.sidewaysFriction;

        bool isRear = (wheel == RearLeftWheel || wheel == RearRightWheel);

        forward.stiffness = isRear ? 1.2f : 1.5f;
        sideways.stiffness = isRear ? 1.0f : 2.5f;

        wheel.forwardFriction = forward;
        wheel.sidewaysFriction = sideways;
    }

    void SetupSuspension(WheelCollider wheel, bool isFront)
    {
        JointSpring spring = wheel.suspensionSpring;
        spring.spring = isFront ? 80000f : 40000f;
        spring.damper = isFront ? 10000f : 6000f;
        spring.targetPosition = 0.5f;

        wheel.suspensionSpring = spring;
        wheel.suspensionDistance = 0.3f;

        WheelFrictionCurve forward = wheel.forwardFriction;
        WheelFrictionCurve sideways = wheel.sidewaysFriction;

        forward.stiffness = isFront ? 1.5f : 1.2f;
        sideways.stiffness = isFront ? 2.5f : 1.0f;

        wheel.forwardFriction = forward;
        wheel.sidewaysFriction = sideways;
    }

    // Zmodyfikowana funkcja AntiRoll
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

        // Nie dodawaj sił, gdy pojazd stoi w miejscu
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
        //_driftAngle.text = "Drift Angle: " + Vector3.Angle(transform.forward, rigid.linearVelocity).ToString("F1");
        //_input.text = "Input: " + verticalInput.ToString("F2");

        Debug.Log($"Gear: {currentGear}, Engine RPM: {engineRPM}, Wheel RPM: {wheelRPM}, Speed: {currentSpeed}, MotorTorque RL: {RearLeftWheel.motorTorque}, Vertical Input: {verticalInput}");
    }
}
