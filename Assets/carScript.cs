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
    }

    void FixedUpdate()
    {
        float motor = verticalInput * drivespeed;

        currentSpeed = motor;

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