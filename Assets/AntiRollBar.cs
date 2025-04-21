using UnityEngine;

public class AntiRollBar : MonoBehaviour
{
    public WheelCollider wheelLeft;
    public WheelCollider wheelRight;
    public float antiRollForce = 5000f;

    void FixedUpdate()
    {
        ApplyAntiRoll(wheelLeft, wheelRight);
    }

    void ApplyAntiRoll(WheelCollider wL, WheelCollider wR)
    {
        WheelHit hitL, hitR;
        bool groundedL = wL.GetGroundHit(out hitL);
        bool groundedR = wR.GetGroundHit(out hitR);

        float travelL = groundedL ? (-wL.transform.InverseTransformPoint(hitL.point).y - wL.radius) / wL.suspensionDistance : 1f;
        float travelR = groundedR ? (-wR.transform.InverseTransformPoint(hitR.point).y - wR.radius) / wR.suspensionDistance : 1f;

        float antiRoll = (travelL - travelR) * antiRollForce;

        if (groundedL)
            wL.attachedRigidbody.AddForceAtPosition(wL.transform.up * -antiRoll, hitL.point);
        if (groundedR)
            wR.attachedRigidbody.AddForceAtPosition(wR.transform.up * antiRoll, hitR.point);
    }
}