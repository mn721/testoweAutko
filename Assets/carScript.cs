using UnityEngine;

public class carScript : MonoBehaviour
{
    public Rigidbody rigid;
    public WheelCollider frontLeft, frontRight, rearLeft, rearRight;
    public float drivespeed = 2400f;
    public float steerspeed = 20f;
    public float brakeForce = 60000f;
    public float handbrakeForce = 150000f;
    public float reverseSpeedThreshold = 2f;

    float horizontalInput, verticalInput;
    bool isHandBraking;
    bool isBraking;

    public TrailRenderer skidLeft;
    public TrailRenderer skidRight;
    public ParticleSystem smokeLeft;
    public ParticleSystem smokeRight;

    void Start()
    {
        SetWheelFriction(frontLeft);
        SetWheelFriction(frontRight);
        SetWheelFriction(rearLeft);
        SetWheelFriction(rearRight);

        if (smokeLeft != null) smokeLeft.Stop();
        if (smokeRight != null) smokeRight.Stop();
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

        if (isBraking && rigid.linearVelocity.magnitude < reverseSpeedThreshold)
        {
            motor = -drivespeed * 0.5f;
        }

        rearLeft.motorTorque = motor;
        rearRight.motorTorque = motor;

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

            DisableSkidEffects();
        }
        else if (isHandBraking)
        {
            rearLeft.brakeTorque = handbrakeForce;
            rearRight.brakeTorque = handbrakeForce;

            EnableSkidEffects();
        }
        else
        {
            DisableSkidEffects();
        }
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

    void EnableSkidEffects()
    {
        if (skidLeft != null) skidLeft.emitting = true;
        if (skidRight != null) skidRight.emitting = true;
        if (smokeLeft != null && !smokeLeft.isPlaying) smokeLeft.Play();
        if (smokeRight != null && !smokeRight.isPlaying) smokeRight.Play();
    }

    void DisableSkidEffects()
    {
        if (skidLeft != null) skidLeft.emitting = false;
        if (skidRight != null) skidRight.emitting = false;
        if (smokeLeft != null && smokeLeft.isPlaying) smokeLeft.Stop();
        if (smokeRight != null && smokeRight.isPlaying) smokeRight.Stop();
    }
}