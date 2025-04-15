using UnityEngine;

public class wheel : MonoBehaviour
{
    public WheelCollider wheelCollider;
    public Transform wheelMesh;
    public bool wheelTurn;

    float currentSteerAngle;

    void Update()
    {
        UpdateVisualRotation();
    }

    void UpdateVisualRotation()
    {
        wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        wheelMesh.position = pos;

        if (wheelTurn)
        {
            float targetAngle = wheelCollider.steerAngle;
            currentSteerAngle = Mathf.LerpAngle(currentSteerAngle, targetAngle, Time.deltaTime * 10f);

            Quaternion steerRot = Quaternion.Euler(currentSteerAngle, 0f, 0f);
            wheelMesh.rotation = rot; //* steerRot;
        }
        else
        {
            wheelMesh.rotation = rot;
        }

        float rotationThisFrame = wheelCollider.rpm / 60f * 360f * Time.deltaTime;
        wheelMesh.Rotate(rotationThisFrame, 0f, 0f);
    }

    //void UpdateVisualRotation()
    //{
    //    wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
    //    wheelMesh.position = pos;
    //    wheelMesh.rotation = rot;

    //    if (wheelTurn)
    //    {
    //        float targetAngle = wheelCollider.steerAngle;
    //        currentSteerAngle = Mathf.LerpAngle(currentSteerAngle, targetAngle, Time.deltaTime * 10f); //Dostosowanie jak szybko ma ruszaæ siê ko³o: zmieniamy 10f
    //        Vector3 localEuler = wheelMesh.localEulerAngles;
    //        wheelMesh.localEulerAngles = new Vector3(localEuler.x, currentSteerAngle, localEuler.z);
    //    }

    //    float rotationThisFrame = wheelCollider.rpm / 60f * 360f * Time.deltaTime;
    //    wheelMesh.Rotate(rotationThisFrame, 0f, 0f);
    //}
}