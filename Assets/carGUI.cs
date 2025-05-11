using UnityEngine;
using UnityEngine.UI;

public class carGUI : MonoBehaviour
{
    public carScript car;
    public Text Velocity;
    public Text Gear;
    public Text RPM;

    void Update()
    {
        float speed = car.rigid.linearVelocity.magnitude * 3.6f;
        Velocity.text = "Predkosc: " + speed.ToString("F1") + " km/h";
        Gear.text = "Bieg: " + car.currentGear.ToString();
        RPM.text = "RPM: " + car.engineRPM.ToString("F0");
    }
}
