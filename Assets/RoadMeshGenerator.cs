using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RoadMeshGenerator : MonoBehaviour
{
    public float roadTotalWidth = 60f; 
    public float barrierToRoadRatio = 0.7f; 
    public int resolution = 10;
    public Material roadMaterial;
    public GameObject barrierPrefab;
    public float barrierSpacing = 2f;
    public float barrierHeightOffset = -0.5f;

    private Mesh mesh;
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector3> rightBarrierPoints = new List<Vector3>();
    private List<Vector3> leftBarrierPoints = new List<Vector3>();
    private List<GameObject> barriers = new List<GameObject>();

    void Awake()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    public void UpdateRoad(List<Vector3> controlPoints)
    {
        GenerateMesh(controlPoints);
        UpdateMeshCollider();
        GenerateBarriers();
        GetComponent<MeshRenderer>().material = roadMaterial;
    }

    void GenerateMesh(List<Vector3> controlPoints)
    {
        vertices.Clear();
        triangles.Clear();
        rightBarrierPoints.Clear();
        leftBarrierPoints.Clear();

        if (controlPoints.Count < 4) return;

        float roadWidth = roadTotalWidth * barrierToRoadRatio;
        float barrierOffset = (roadTotalWidth - roadWidth) * 0.5f;

        for (int segment = 0; segment < controlPoints.Count - 3; segment++)
        {
            for (int i = 0; i <= resolution; i++)
            {
                float t = i / (float)resolution;
                Vector3 point = CalculateCatmullRomPosition(t, controlPoints[segment], controlPoints[segment + 1], controlPoints[segment + 2], controlPoints[segment + 3]);
                Vector3 tangent = CalculateCatmullRomTangent(t, controlPoints[segment], controlPoints[segment + 1], controlPoints[segment + 2], controlPoints[segment + 3]).normalized;

                Vector3 normal = Vector3.Cross(tangent, Vector3.up).normalized;

                Vector3 rightBarrierPoint = point + normal * (roadWidth * 0.5f - barrierOffset);
                Vector3 leftBarrierPoint = point - normal * (roadWidth * 0.5f - barrierOffset);

                rightBarrierPoints.Add(rightBarrierPoint);
                leftBarrierPoints.Add(leftBarrierPoint);

                Vector3 rightPoint = point + normal * roadWidth * 0.5f;
                Vector3 leftPoint = point - normal * roadWidth * 0.5f;

                vertices.Add(rightPoint);
                vertices.Add(leftPoint);
            }
        }

        GenerateTriangles();
        UpdateMesh();
    }

    void GenerateTriangles()
    {
        for (int i = 0; i < vertices.Count - 2; i += 2)
        {
            triangles.Add(i);
            triangles.Add(i + 2);
            triangles.Add(i + 1);

            triangles.Add(i + 1);
            triangles.Add(i + 2);
            triangles.Add(i + 3);
        }
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
    }

    void UpdateMeshCollider()
    {
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null) meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }

    void GenerateBarriers()
    {
        ClearBarriers();
        if (barrierPrefab == null) return;

        GenerateBarriersAlongEdge(rightBarrierPoints, true);
        GenerateBarriersAlongEdge(leftBarrierPoints, false);
    }

    void ClearBarriers()
    {
        foreach (var barrier in barriers)
        {
            if (barrier != null)
            {
                Destroy(barrier);
            }
        }
        barriers.Clear();
    }

    void GenerateBarriersAlongEdge(List<Vector3> barrierPoints, bool isRightEdge)
    {
        if (barrierPoints.Count < 2) return;

        float distance = 0f;
        for (int i = 0; i < barrierPoints.Count - 1; i++)
        {
            Vector3 start = barrierPoints[i];
            Vector3 end = barrierPoints[i + 1];
            float segmentLength = Vector3.Distance(start, end);
            Vector3 direction = (end - start).normalized;

            while (distance < segmentLength)
            {
                float t = distance / segmentLength;
                Vector3 position = Vector3.Lerp(start, end, t);

                position.y += barrierHeightOffset;

                Vector3 normal = Vector3.Cross(direction, Vector3.up).normalized;
                Quaternion rotation = Quaternion.LookRotation(isRightEdge ? -normal : normal, Vector3.up);

                GameObject barrier = Instantiate(barrierPrefab, position, rotation, transform);
                barriers.Add(barrier);
                distance += barrierSpacing;
            }
            distance -= segmentLength;
        }
    }

    Vector3 CalculateCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return 0.5f * (
            (-p0 + 3f * p1 - 3f * p2 + p3) * (t * t * t) +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * (t * t) +
            (-p0 + p2) * t +
            2f * p1
        );
    }

    Vector3 CalculateCatmullRomTangent(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return 0.5f * (
            (-p0 + 3f * p1 - 3f * p2 + p3) * (3f * t * t) +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * (2f * t) +
            (-p0 + p2)
        ).normalized;
    }
}