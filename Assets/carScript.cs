using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class carScript : MonoBehaviour
{
    public Rigidbody rigid;
    public WheelCollider FrontLeftWheel, FrontRightWheel, RearLeftWheel, RearRightWheel;
    public float drivespeed = 24000f;
    public float steerspeed = 20f;
    public float brakeForce = 60000f;
    public float handbrakeForce = 150000f;
    public float maxEngineRPM = 7000f;
    public float minEngineRPM = 800f;
    public float currentSpeed = 0f;

    public enum Gear { R = -1, N = 0, First = 1, Second, Third, Fourth, Fifth, Sixth }
    public Gear currentGear = Gear.N;
    public float[] gearRatios = { -3.5f, 0f, 3.2f, 2.1f, 1.5f, 1.2f, 1.0f, 0.85f }; // R, N, 1,2,3,4,5,6

    public enum DriveType { RWD, FWD, AWD }
    public DriveType currentDrive = DriveType.RWD;

    public Slider driftSlider;
    public TMP_Text driftPointsText;
    private float driftPoints = 0f;

    public float wheelRPM;
    public float engineRPM;

    float horizontalInput, verticalInput;
    bool isHandBraking;
    bool isBraking;

    void Start()
    {
        SetWheelFriction(FrontLeftWheel);
        SetWheelFriction(FrontRightWheel);
        SetWheelFriction(RearLeftWheel);
        SetWheelFriction(RearRightWheel);
        rigid.centerOfMass = new Vector3(0, -0.2f, 0.0f);

        SetupSuspension(FrontLeftWheel, true);
        SetupSuspension(FrontRightWheel, true);
        SetupSuspension(RearLeftWheel, false);
        SetupSuspension(RearRightWheel, false);
    }

    void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        isHandBraking = Input.GetKey(KeyCode.Space);
        isBraking = Input.GetKey(KeyCode.S);

        if (Input.GetKeyDown(KeyCode.E) && currentGear < Gear.Sixth) currentGear++;
        if (Input.GetKeyDown(KeyCode.Q) && currentGear > Gear.R) currentGear--;

        if (Input.GetKeyDown(KeyCode.T))
        {
            currentDrive = (DriveType)(((int)currentDrive + 1) % 3);
        }

        if (driftSlider)
        {
            float driftValue = driftSlider.value;
            AdjustDriftFriction(driftValue);
        }
    }

    void FixedUpdate()
    {
        float speedKmh = rigid.linearVelocity.magnitude * 3.6f;
        currentSpeed = speedKmh;

        wheelRPM = (rigid.linearVelocity.magnitude / (2 * Mathf.PI * 0.34f)) * 60f;
        UpdateEngineRPM();

        rigid.AddForce(Vector3.down * 7f, ForceMode.Acceleration);

        float motor = 0f;
        if (currentGear != Gear.N)
        {
            float gearRatio = gearRatios[(int)currentGear + 1];
            bool canMove = CanMoveFromCurrentGear(speedKmh);
            if (canMove && engineRPM < maxEngineRPM)
            {
                float tractionControl = Mathf.Clamp01(1 - (currentSpeed / 150f)); // mniej mocy przy wi�kszej pr�dko�ci
                motor = verticalInput * drivespeed * Mathf.Sign(gearRatio) * tractionControl;
            }
        }

        ApplyDriveTorque(motor);

        FrontLeftWheel.steerAngle = steerspeed * horizontalInput;
        FrontRightWheel.steerAngle = steerspeed * horizontalInput;

        ApplyBrakes();

        Vector3 euler = transform.eulerAngles;
        euler.z = 0f;
        transform.eulerAngles = euler;

        AntiRoll(FrontLeftWheel, FrontRightWheel);
        AntiRoll(RearLeftWheel, RearRightWheel);

        CalculateDriftPoints();

        SetupSuspension(FrontLeftWheel, true);
        SetupSuspension(FrontRightWheel, true);
        SetupSuspension(RearLeftWheel, false);
        SetupSuspension(RearRightWheel, false);

    }

    void ApplyDriveTorque(float motor)
    {
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
        if (verticalInput == 0f)
        {
            RearLeftWheel.brakeTorque = 1000f;
            RearRightWheel.brakeTorque = 1000f;
        }
        else
        {
            RearLeftWheel.brakeTorque = 0;
            RearRightWheel.brakeTorque = 0;
        }

        if (isBraking)
        {
            FrontLeftWheel.brakeTorque = brakeForce;
            FrontRightWheel.brakeTorque = brakeForce;
            RearLeftWheel.brakeTorque = brakeForce;
            RearRightWheel.brakeTorque = brakeForce;
        }
        else if (isHandBraking)
        {
            RearLeftWheel.brakeTorque = handbrakeForce;
            RearRightWheel.brakeTorque = handbrakeForce;
        }
        else
        {
            FrontLeftWheel.brakeTorque = 0;
            FrontRightWheel.brakeTorque = 0;
        }
    }

    void UpdateEngineRPM()
    {
        if (currentGear == Gear.N)
        {
            engineRPM = Mathf.Lerp(engineRPM, minEngineRPM, Time.deltaTime * 2f);
        }
        else
        {
            float gearRatio = gearRatios[(int)currentGear + 1];
            engineRPM = Mathf.Abs(wheelRPM * gearRatio);
            engineRPM = Mathf.Clamp(engineRPM, minEngineRPM, maxEngineRPM + 500f);
        }
    }

    bool CanMoveFromCurrentGear(float speedKmh)
    {
        if (currentGear == Gear.R || currentGear == Gear.First) return true;
        if (speedKmh < 10f) return false;
        return true;
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.white;

        float speed = rigid.linearVelocity.magnitude * 3.6f;

        GUI.Label(new Rect(20, 20, 300, 30), "Predkosc: " + speed.ToString("F1") + " km/h", style);
        GUI.Label(new Rect(20, 50, 300, 30), "Bieg: " + currentGear.ToString(), style);
        GUI.Label(new Rect(20, 80, 300, 30), "RPM: " + engineRPM.ToString("F0"), style);
    }

    void SetWheelFriction(WheelCollider wheel)
    {
        WheelFrictionCurve forwardFriction = wheel.forwardFriction;
        WheelFrictionCurve sidewaysFriction = wheel.sidewaysFriction;

        forwardFriction.stiffness = 1.5f;
        sidewaysFriction.stiffness = 2.0f;

        wheel.forwardFriction = forwardFriction;
        wheel.sidewaysFriction = sidewaysFriction;
    }

    void AdjustDriftFriction(float driftFactor)
    {
        AdjustWheelFriction(FrontLeftWheel, driftFactor);
        AdjustWheelFriction(FrontRightWheel, driftFactor);
        AdjustWheelFriction(RearLeftWheel, driftFactor);
        AdjustWheelFriction(RearRightWheel, driftFactor);
    }

    void AdjustWheelFriction(WheelCollider wheel, float driftFactor)
    {
        WheelFrictionCurve sidewaysFriction = wheel.sidewaysFriction;
        sidewaysFriction.stiffness = Mathf.Lerp(0.5f, 3.0f, driftFactor);
        wheel.sidewaysFriction = sidewaysFriction;
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

    void AntiRoll(WheelCollider leftWheel, WheelCollider rightWheel)
    {
        WheelHit hit;
        float travelLeft = 1.0f;
        float travelRight = 1.0f;

        bool groundedLeft = leftWheel.GetGroundHit(out hit);
        if (groundedLeft)
            travelLeft = (-leftWheel.transform.InverseTransformPoint(hit.point).y - leftWheel.radius) / leftWheel.suspensionDistance;

        bool groundedRight = rightWheel.GetGroundHit(out hit);
        if (groundedRight)
            travelRight = (-rightWheel.transform.InverseTransformPoint(hit.point).y - rightWheel.radius) / rightWheel.suspensionDistance;

        float antiRollForce = (travelLeft - travelRight) * 5000f;

        if (groundedLeft)
            rigid.AddForceAtPosition(leftWheel.transform.up * -antiRollForce, leftWheel.transform.position);
        if (groundedRight)
            rigid.AddForceAtPosition(rightWheel.transform.up * antiRollForce, rightWheel.transform.position);
    }

    void SetupSuspension(WheelCollider wheel, bool isFront)
    {
        JointSpring spring = wheel.suspensionSpring;
        spring.spring = isFront ? 80000f : 60000f;
        spring.damper = isFront ? 10000f : 8000f;
        spring.targetPosition = 0.5f;
        wheel.suspensionSpring = spring;

        wheel.suspensionDistance = 0.3f;

        // Dynamiczne ustawienie przyczepno�ci
        float driftAmount = driftSlider != null ? driftSlider.value : 1.5f;
        float speedFactor = Mathf.Clamp01(currentSpeed / 120f); // wi�ksza pr�dko�� = wi�cej przyczepno�ci
        float baseStiffness = isFront ? 2.0f : 1.2f;
        float adjustedStiffness = Mathf.Lerp(baseStiffness, driftAmount, 1f - speedFactor);

        WheelFrictionCurve forwardFriction = wheel.forwardFriction;
        forwardFriction.stiffness = isFront ? 1.5f : adjustedStiffness * 0.9f;
        wheel.forwardFriction = forwardFriction;

        WheelFrictionCurve sidewaysFriction = wheel.sidewaysFriction;
        sidewaysFriction.stiffness = adjustedStiffness;
        wheel.sidewaysFriction = sidewaysFriction;
    }
}