using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public Transform defaultView;
    public Transform leftView;
    public Transform rightView;
    public Transform frontView;

    private Transform currentTarget;
    private bool overrideView = false;

    void Start()
    {
        currentTarget = defaultView;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            currentTarget = leftView;
            overrideView = true;
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            currentTarget = rightView;
            overrideView = true;
        }
        else if (Input.GetKeyDown(KeyCode.K))
        {
            currentTarget = frontView;
            overrideView = true;
        }

        if (!Input.GetKey(KeyCode.J) && !Input.GetKey(KeyCode.K) && !Input.GetKey(KeyCode.L))
        {
            currentTarget = defaultView;
            overrideView = false;
        }

        transform.position = currentTarget.position;
        transform.rotation = currentTarget.rotation;
    }
}