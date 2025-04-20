using UnityEngine;

public class SimpleGearbox : MonoBehaviour
{
    public float[] gearSpeedLimits = { 20f, 40f, 60f, 90f, 130f };
    public int currentGear = 0;
    public float acceleration = 10f;
    public float deceleration = 15f;
    public float currentSpeed = 0f;

    void Update()
    {
        HandleGearChange();
        HandleAcceleration();
    }

    void HandleGearChange()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentGear < gearSpeedLimits.Length - 1)
                currentGear++;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (currentGear > 0)
                currentGear--;
        }
    }

    void HandleAcceleration()
    {
        if (Input.GetKey(KeyCode.W))
        {
            currentSpeed += acceleration * Time.deltaTime;

            if (currentSpeed > gearSpeedLimits[currentGear])
                currentSpeed = gearSpeedLimits[currentGear];
        }
        else
        {
            currentSpeed -= deceleration * Time.deltaTime;
            if (currentSpeed < 0)
                currentSpeed = 0;
        }
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
    }
}