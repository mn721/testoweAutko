using UnityEngine;

public class Wheel : MonoBehaviour
{
    public WheelCollider wheelCollider;
    public Transform wheelMesh;
    public bool wheelTurn; // Czy ko³o skrêtne

    private float currentSteerAngle = 0f;
    private float wheelRotation = 0f;

    void Start()
    {
        SetupWheel(wheelCollider);
    }

    void Update()
    {
        UpdateWheelVisuals();
    }

    void SetupWheel(WheelCollider wheelCollider)
    {
        wheelCollider.mass = 40f;
        wheelCollider.radius = 0.35f;
        wheelCollider.suspensionDistance = 0.3f;

        JointSpring spring = wheelCollider.suspensionSpring;
        spring.spring = 70000f;
        spring.damper = 9000f;
        spring.targetPosition = 0.5f;
        wheelCollider.suspensionSpring = spring;

        WheelFrictionCurve forwardFriction = wheelCollider.forwardFriction;
        forwardFriction.stiffness = 1.5f;
        wheelCollider.forwardFriction = forwardFriction;

        WheelFrictionCurve sidewaysFriction = wheelCollider.sidewaysFriction;
        sidewaysFriction.stiffness = 2.5f;
        wheelCollider.sidewaysFriction = sidewaysFriction;
    }

    void UpdateWheelVisuals()
    {
        // Pozycja z WheelCollidera
        wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion _);
        wheelMesh.position = pos;

        // P³ynny skrêt
        float targetAngle = wheelTurn ? wheelCollider.steerAngle : 0f;
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetAngle, Time.deltaTime * 10f);

        // Oblicz obrót tocz¹cy siê
        float rotationThisFrame = wheelCollider.rpm / 60f * 360f * Time.deltaTime;
        wheelRotation += rotationThisFrame;
        wheelRotation %= 360f;

        // Ustaw rotacjê: skrêt na Y, toczenie na X
        wheelMesh.localRotation = Quaternion.Euler(wheelRotation, currentSteerAngle, 0f);
    }
}