using UnityEngine;

public class speedometer : MonoBehaviour
{
    private Transform needleTransform;

    private void Awake()
    {
        needleTransform = transform.Find("needle");
    }

    private void Update()
    {
        
    }

    private void GetSpeedRotation()
    {

    }
}
