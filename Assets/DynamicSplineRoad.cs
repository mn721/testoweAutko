using System.Collections.Generic;
using UnityEngine;

public class DynamicSplineRoad : MonoBehaviour
{
    public Transform car;
    public float spawnDistance = 100f;
    public float pointSpacing = 10f;
    public int maxPoints = 50;
    public float maxDeviationAngle = 15f;

    private List<Vector3> controlPoints = new List<Vector3>();
    private Vector3 currentDirection;

    void Start()
    {
        currentDirection = car.forward;
        InitializeStartingPoints();
    }

    void Update()
    {
        if (Vector3.Distance(car.position, GetLastPoint()) < spawnDistance)
        {
            AddNewPoint();
            RemoveOldPoints();
            GetComponent<RoadMeshGenerator>().UpdateRoad(controlPoints);
        }
    }

    void InitializeStartingPoints()
    {
        for (int i = 0; i < 4; i++)
        {
            AddNewPoint();
        }
    }

    void AddNewPoint()
    {
        Vector3 newPos = controlPoints.Count == 0 ?
            car.position :
            GetLastPoint() + currentDirection * pointSpacing;

        currentDirection = Quaternion.Euler(
            0,
            Random.Range(-maxDeviationAngle, maxDeviationAngle),
            0
        ) * currentDirection;

        controlPoints.Add(newPos);
    }

    void RemoveOldPoints()
    {
        if (controlPoints.Count > maxPoints)
        {
            controlPoints.RemoveAt(0);
        }
    }

    Vector3 GetLastPoint()
    {
        return controlPoints[controlPoints.Count - 1];
    }

    public List<Vector3> GetPoints()
    {
        return controlPoints;
    }
}