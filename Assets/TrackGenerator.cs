using System.Collections.Generic;
using UnityEngine;

public class RoadGenerator : MonoBehaviour
{
    public GameObject roadSegmentPrefab;
    public Transform carTransform;
    public int initialSegments = 3;
    public float spawnDistance = 50f;
    public float despawnDistance = 30f;
    public float maxTurnAngle = 5f;
    [Range(0, 1)] public float turnChance = 0.3f;

    private Queue<GameObject> segmentsQueue = new Queue<GameObject>();
    private float segmentLength = 35;
    private Vector3 currentPosition;
    private Quaternion currentRotation;

    void Start()
    {
        currentPosition = Vector3.zero;
        currentRotation = Quaternion.identity;

        // Pocz¹tkowe segmenty proste
        float originalTurnChance = turnChance;
        turnChance = 0;

        for (int i = 0; i < initialSegments; i++)
        {
            SpawnSegment();
        }

        turnChance = originalTurnChance;

        if (carTransform != null)
        {
            carTransform.position = currentPosition + Vector3.up * 2 - currentRotation * Vector3.forward * segmentLength * initialSegments;
            carTransform.rotation = currentRotation;
        }
    }

    void Update()
    {
        if (Vector3.Distance(carTransform.position, currentPosition) < spawnDistance)
        {
            SpawnSegment();
        }

        if (segmentsQueue.Count > 0 &&
            Vector3.Distance(carTransform.position, segmentsQueue.Peek().transform.position) > despawnDistance)
        {
            Destroy(segmentsQueue.Dequeue());
        }
    }

    void SpawnSegment()
    {
        GameObject newSegment = Instantiate(roadSegmentPrefab, currentPosition, currentRotation);
        segmentsQueue.Enqueue(newSegment);

        // Losowy skrêt
        float turnAngle = 0f;
        if (Random.value < turnChance)
        {
            turnAngle = Random.Range(-maxTurnAngle, maxTurnAngle);
        }

        // Aktualizacja pozycji i rotacji
        currentRotation *= Quaternion.Euler(0, turnAngle, 0);
        currentPosition += currentRotation * Vector3.forward * segmentLength;
    }
}