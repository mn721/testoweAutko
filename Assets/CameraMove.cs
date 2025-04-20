using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public Transform[] cameraPositions;
    public Camera cam;
    private int currentIndex = 0;

    void Update()
    {
        SetCameraPosition(3);

        if (Input.GetKey(KeyCode.J)) SetCameraPosition(0);
        if (Input.GetKey(KeyCode.K)) SetCameraPosition(1);
        if (Input.GetKey(KeyCode.L)) SetCameraPosition(2);
    }

    void SetCameraPosition(int index)
    {
        if (index < cameraPositions.Length)
        {
            currentIndex = index;
            cam.transform.position = cameraPositions[index].position;
            cam.transform.rotation = cameraPositions[index].rotation;
        }
    }
}