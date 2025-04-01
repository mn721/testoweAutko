using UnityEngine;

public class carScript : MonoBehaviour
{
    public Rigidbody rigid;
    public WheelCollider frontLeft, frontRight, rearLeft, rearRight;
    public float drivespeed = 800f;
    public float steerspeed = 30f;
    public float brakeForce = 3000f;
    public float handbrakeForce = 15000f;
    public float reverseSpeedThreshold = 2f;
    float horizontalInput, verticalInput;
    bool isHandBraking;
    bool isBraking;

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

        if (isBraking && rigid.linearVelocity.magnitude < reverseSpeedThreshold)
        {
            motor = -drivespeed * 0.5f;
        }

        rearLeft.motorTorque = motor;
        rearRight.motorTorque = motor;

        frontLeft.steerAngle = steerspeed * horizontalInput;
        frontRight.steerAngle = steerspeed * horizontalInput;

        if (isBraking && rigid.linearVelocity.magnitude >= reverseSpeedThreshold)
        {
            rearLeft.brakeTorque = brakeForce;
            rearRight.brakeTorque = brakeForce;
        }
        else
        {
            rearLeft.brakeTorque = 0;
            rearRight.brakeTorque = 0;
        }

        if (isHandBraking)
        {
            rearLeft.brakeTorque = handbrakeForce;
            rearRight.brakeTorque = handbrakeForce;
        }
    }
}