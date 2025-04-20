using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    // The RPMs
    public int RPM;
    public int MaxRPM;
    public int ChangeRPM;

    // This is the actual GearBox
    public float[] Gearbox;
    public int CurrentGear;
    public bool CanShiftGear = false;

    // The vehicle Speed
    public float Speed;
    public Rigidbody RB;
    public float Torque;

    private void Update()
    {
        // It checks if the RPMs are the desired ones and if you can shift
        // UpShift
        if (RPM >= ChangeRPM && CanShiftGear && CurrentGear != Gearbox.Length - 1)
        { CurrentGear++; CanShiftGear = false; }
        //DownShift

        if (RPM <= 2000f && CanShiftGear && CurrentGear != 0)
        { CurrentGear--; CanShiftGear = false; }
        // Takes Car Of the boolean

        if (RPM < ChangeRPM)
        { CanShiftGear = true; }
        // Once you have reached the last gear ypu can not shift

        if (CurrentGear == Gearbox.Length - 1)
        { CanShiftGear = false; }
        // Speed

        Speed = RB.linearVelocity.magnitude;
        GearBox();
    }

    void GearBox()
    {
        if (Speed >= Gearbox[CurrentGear])
        { Torque = 0f; }
        else
        { Torque = 1f; }
    }
}