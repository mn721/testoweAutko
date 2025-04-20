using UnityEngine;

public class carScript : MonoBehaviour
{
    public Rigidbody rigid;
    public WheelCollider frontLeft, frontRight, rearLeft, rearRight;
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
        SetWheelFriction(frontLeft);
        SetWheelFriction(frontRight);
        SetWheelFriction(rearLeft);
        SetWheelFriction(rearRight);
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

        rearLeft.motorTorque = motor;
        rearRight.motorTorque = motor;
        //frontLeft.motorTorque = motor;
        //frontRight.motorTorque = motor;

        frontLeft.steerAngle = steerspeed * horizontalInput;
        frontRight.steerAngle = steerspeed * horizontalInput;

        frontLeft.brakeTorque = 0;
        frontRight.brakeTorque = 0;
        rearLeft.brakeTorque = 0;
        rearRight.brakeTorque = 0;

        if (isBraking)
        {
            frontLeft.brakeTorque = brakeForce;
            frontRight.brakeTorque = brakeForce;
            rearLeft.brakeTorque = brakeForce;
            rearRight.brakeTorque = brakeForce;
        }
        else if (isHandBraking)
        {
            rearLeft.brakeTorque = handbrakeForce;
            rearRight.brakeTorque = handbrakeForce;
        }
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
}