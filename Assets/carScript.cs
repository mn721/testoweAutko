using UnityEngine;

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

    public enum Gear
    {
        R = -1,
        N = 0,
        First = 1,
        Second,
        Third,
        Fourth,
        Fifth,
        Sixth
    }

    public Gear currentGear = Gear.N;
    public float[] gearRatios = { -3.5f, 0f, 3.2f, 2.1f, 1.5f, 1.2f, 1.0f, 0.85f }; // R, N, 1,2,3,4,5,6

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
        rigid.centerOfMass = new Vector3(0, -0.2f, -0.1f);
    }

    void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        isHandBraking = Input.GetKey(KeyCode.Space);
        isBraking = Input.GetKey(KeyCode.S);

        if (Input.GetKeyDown(KeyCode.E) && currentGear < Gear.Sixth)
            currentGear++;
        

        if (Input.GetKeyDown(KeyCode.Q) && currentGear > Gear.R)
            currentGear--;
            
        
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
                motor = verticalInput * drivespeed * Mathf.Sign(gearRatio);
            }
            else
            {
                motor = 0f;
            }
        }

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

        RearLeftWheel.motorTorque = motor;
        RearRightWheel.motorTorque = motor;

        FrontLeftWheel.steerAngle = steerspeed * horizontalInput;
        FrontRightWheel.steerAngle = steerspeed * horizontalInput;

        FrontLeftWheel.brakeTorque = 0;
        FrontRightWheel.brakeTorque = 0;

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

        Vector3 euler = transform.eulerAngles;
        euler.z = 0f;
        transform.eulerAngles = euler;

        AntiRoll(FrontLeftWheel, FrontRightWheel);
        AntiRoll(RearLeftWheel, RearRightWheel);
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
        if (currentGear == Gear.R)
            return true;

        if (currentGear == Gear.First)
            return true;

        if (speedKmh < 10f)
            return false;

        return true;
    }

    void OnGUI()
    {
        float speed = rigid.linearVelocity.magnitude * 3.6f;
        GUI.Label(new Rect(250, 50, 200, 20), "Predkosc: " + speed.ToString("F1") + " km/h");
        GUI.Label(new Rect(250, 90, 200, 20), "Bieg: " + currentGear.ToString());
        GUI.Label(new Rect(250, 130, 200, 20), "RPM: " + engineRPM.ToString("F0"));
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

        float antiRollForce = (travelLeft - travelRight) * 10000f;

        if (groundedLeft)
            rigid.AddForceAtPosition(leftWheel.transform.up * -antiRollForce, leftWheel.transform.position);
        if (groundedRight)
            rigid.AddForceAtPosition(rightWheel.transform.up * antiRollForce, rightWheel.transform.position);
    }
}
