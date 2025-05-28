using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System;
using System.Collections;
using System.Reflection;

public class CarScriptTests
{
    private GameObject carObject;
    private carScript car;

    [SetUp]
    public void Setup()
    {
        carObject = new GameObject("TestCar");
        car = carObject.AddComponent<carScript>();

        car.FrontLeftWheel = carObject.AddComponent<WheelCollider>();
        car.FrontRightWheel = carObject.AddComponent<WheelCollider>();
        car.RearLeftWheel = carObject.AddComponent<WheelCollider>();
        car.RearRightWheel = carObject.AddComponent<WheelCollider>();
        car.rigid = carObject.AddComponent<Rigidbody>();

        car.gearRatios = new float[] { -3.2f, 0f, 3.5f, 2.2f, 1.5f, 1.1f, 0.9f, 0.7f };
        car.minEngineRPM = 1000f;
        car.maxEngineRPM = 7000f;
        car.brakeForce = 60000f;
        car.maxTorqueRPM = 4000f;
        car.powerBandWidth = 2000f;
        car.lowEndTorqueMultiplier = 0.6f;

        car.currentGear = carScript.Gear.N;
    }

    /*[UnityTest]
    public IEnumerator BrakeInput_AppliesBrakeTorque()
    {
        SetPrivateField("isBraking", true);
        SetPrivateField("currentSpeed", 50f);

        car.FixedUpdate();
        yield return null;

        Assert.That(car.FrontLeftWheel.brakeTorque, Is.EqualTo(car.brakeForce).Within(1f));
        Assert.That(car.RearLeftWheel.brakeTorque, Is.EqualTo(car.brakeForce).Within(1f));
    }*/

    [Test]
    public void GearUp_IncreasesGear()
    {
        car.currentGear = carScript.Gear.First;
        car.currentGear++;

        Assert.That(car.currentGear, Is.EqualTo(carScript.Gear.First));
    }

    [Test]
    public void GearDown_DecreasesGear()
    {
        car.currentGear = carScript.Gear.Second;
        car.currentGear--;

        Assert.That(car.currentGear, Is.EqualTo(carScript.Gear.N));
    }

    [Test]
    public void GearDoesNotExceedLimits()
    {
        car.currentGear = carScript.Gear.Sixth;
        car.currentGear++;
        Assert.That(car.currentGear, Is.EqualTo(carScript.Gear.Sixth));

        car.currentGear = carScript.Gear.R;
        car.currentGear--;
        Assert.That(car.currentGear, Is.EqualTo(carScript.Gear.R));
    }

   /* [Test]
    public void GetTorqueFactorFromRPM_PeakTorqueRPM_ReturnsOne()
    {
        SetPrivateField("engineRPM", car.maxTorqueRPM);
        float result = car.GetTorqueFactorFromRPM();
        Assert.That(result, Is.EqualTo(1f).Within(0.01f));
    }

    [Test]
    public void GetTorqueFactorFromRPM_BelowPeak_ReturnsInterpolated()
    {
        SetPrivateField("engineRPM", 2500f);
        float result = car.GetTorqueFactorFromRPM();
        Assert.That(result, Is.GreaterThan(car.lowEndTorqueMultiplier).And.LessThan(1f));
    }

    [Test]
    public void GetTorqueFactorFromRPM_AbovePeak_ReturnsInterpolated()
    {
        SetPrivateField("engineRPM", 6000f);
        float result = car.GetTorqueFactorFromRPM();
        Assert.That(result, Is.LessThan(1f).And.GreaterThan(0.5f));
    }*/

    [Test]
    public void UpdateEngineRPM_Neutral_Throttle_RPMGoesUp()
    {
        car.currentGear = carScript.Gear.N;

        SetPrivateField("verticalInput", 1f);
        SetPrivateField("engineRPM", 1000f);
        SetPrivateField("targetEngineRPM", 1000f);

        for (int i = 0; i < 10; i++)
            car.Invoke("UpdateEngineRPM", 0f);

        float engineRPM = (float)GetPrivateField("engineRPM");
        Assert.That(engineRPM, Is.GreaterThan(1000f));
    }

    private void SetPrivateField(string fieldName, object value)
    {
        typeof(carScript).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(car, value);
    }

    private object GetPrivateField(string fieldName)
    {
        return typeof(carScript).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(car);
    }
}
