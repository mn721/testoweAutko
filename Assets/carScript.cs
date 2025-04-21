using UnityEngine;

public class carScript : MonoBehaviour
{
    public Rigidbody rigid;
    public WheelCollider FrontLeftWheel, FrontRightWheel, RearLeftWheel, RearRightWheel;
    public float drivespeed = 24000f;
    public float steerspeed = 20f;
    public float brakeForce = 60000f;
    public float handbrakeForce = 150000f;
    public float reverseSpeedThreshold = 2f;
    public float currentSpeed = 0f;
    public float[] gearSpeedLimits = { 0f, 25f, 50f, 100f, 145f, 190f };
    public int currentGear = 0;

    float horizontalInput, verticalInput;
    bool isHandBraking;
    bool isBraking;

    void Start()
    {
        SetWheelFriction(FrontLeftWheel);
        SetWheelFriction(FrontRightWheel);
        SetWheelFriction(RearLeftWheel);
        SetWheelFriction(RearRightWheel);
        GetComponent<Rigidbody>().centerOfMass = new Vector3(0, -0.2f, -0.1f);
    }

    void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        isHandBraking = Input.GetKey(KeyCode.Space);
        isBraking = Input.GetKey(KeyCode.S);

        if (Input.GetKeyDown(KeyCode.E) && currentGear < gearSpeedLimits.Length - 1)
            currentGear++;

        if (Input.GetKeyDown(KeyCode.Q) && currentGear > 0)
            currentGear--;
    }

    void FixedUpdate()
    {
        float speedKmh = rigid.linearVelocity.magnitude * 3.6f;
        float motor = 0f;

        if (currentGear > 0)
        {
            if (speedKmh < gearSpeedLimits[currentGear] || verticalInput < 0)
                motor = verticalInput * drivespeed;
        }

        currentSpeed = speedKmh;

        if (isBraking && drivespeed < 100)
        {
            motor = -drivespeed;
        }

        RearLeftWheel.motorTorque = motor;
        RearRightWheel.motorTorque = motor;
        //frontLeft.motorTorque = motor;
        //frontRight.motorTorque = motor;

        FrontLeftWheel.steerAngle = steerspeed * horizontalInput;
        FrontRightWheel.steerAngle = steerspeed * horizontalInput;

        FrontLeftWheel.brakeTorque = 0;
        FrontRightWheel.brakeTorque = 0;
        RearLeftWheel.brakeTorque = 0;
        RearRightWheel.brakeTorque = 0;

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
        euler.z = 0f; // reset przechy³u
        transform.eulerAngles = euler;

        AntiRoll(FrontLeftWheel, FrontRightWheel);
        AntiRoll(RearLeftWheel, RearRightWheel);
    }

    void OnGUI()
    {
        float speed = rigid.linearVelocity.magnitude * 3.6f;
        GUI.Label(new Rect(10, 10, 200, 20), "Predkosc: " + speed.ToString("F1") + " km/h");

        GUI.Label(new Rect(10, 20, 200, 20), "Bieg: " + (currentGear).ToString());
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

        float antiRollForce = (travelLeft - travelRight) * 10000f; // dostosuj si³ê

        if (groundedLeft)
            GetComponent<Rigidbody>().AddForceAtPosition(leftWheel.transform.up * -antiRollForce, leftWheel.transform.position);
        if (groundedRight)
            GetComponent<Rigidbody>().AddForceAtPosition(rightWheel.transform.up * antiRollForce, rightWheel.transform.position);
    }
}