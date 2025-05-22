using System.Collections.Generic;
using UnityEngine;

public class InfiniteTrackGenerator : MonoBehaviour
{
    [Header("Base Settings")]
    public GameObject trackSegmentPrefab;
    public float segmentLength = 1.5f; // D³ugoœæ pojedynczego segmentu
    public float trackWidth = 8f;
    public float maxCurvature = 45f;
    public Transform vehicle; // Referencja do pojazdu

    [Header("Generation Settings")]
    public int segmentsAhead = 40; // Liczba segmentów do generowania przed pojazdem
    public int segmentsBehind = 20; // Liczba segmentów do utrzymania za pojazdem
    public float cleanupCheckInterval = 0.5f; // Co ile sekund sprawdzaæ do usuniêcia

    [Header("Noise Settings")]
    public float noiseFrequency = 0.1f;
    private Vector2 noiseOffset;

    private Queue<GameObject> segmentsQueue = new Queue<GameObject>();
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private float nextCleanupTime;
    private int lastCleanSegmentIndex = 0;

    void Start()
    {
        if (vehicle == null)
        {
            Debug.LogError("Vehicle reference not set in TrackGenerator!");
            enabled = false;
            return;
        }

        noiseOffset = new Vector2(
            Random.Range(0f, 1000f),
            Random.Range(0f, 1000f)
        );

        // Inicjalizacja pozycji startowej
        lastPosition = vehicle.position - vehicle.forward * segmentsBehind * segmentLength;
        lastRotation = vehicle.rotation;

        // Generowanie pocz¹tkowej drogi
        GenerateInitialTrack();
    }

    void Update()
    {
        // Generuj nowe segmenty jeœli potrzeba
        if (segmentsQueue.Count == 0 ||
            Vector3.Distance(vehicle.position, lastPosition) < segmentsAhead * segmentLength)
        {
            GenerateSegment();
        }

        // Okresowe czyszczenie starych segmentów
        if (Time.time >= nextCleanupTime)
        {
            CleanupOldSegments();
            nextCleanupTime = Time.time + cleanupCheckInterval;
        }
    }

    void GenerateInitialTrack()
    {
        for (int i = 0; i < segmentsAhead + segmentsBehind; i++)
        {
            GenerateSegment();
        }
    }

    void GenerateSegment()
    {
        // Oblicz now¹ rotacjê na podstawie szumu Perlina
        float noiseValue = Mathf.PerlinNoise(
            segmentsQueue.Count * noiseFrequency + noiseOffset.x,
            noiseOffset.y
        );

        float targetAngle = Mathf.Lerp(-maxCurvature, maxCurvature, noiseValue);
        lastRotation *= Quaternion.Euler(0, targetAngle * Time.deltaTime, 0);

        // Oblicz now¹ pozycjê (skrócon¹ o 1/10 d³ugoœci segmentu)
        float adjustedSegmentLength = segmentLength * 0.9f;
        lastPosition += lastRotation * Vector3.forward * adjustedSegmentLength;

        // Utwórz segment (zachowaj oryginaln¹ skalê)
        GameObject segment = Instantiate(trackSegmentPrefab, lastPosition, lastRotation);
        segment.transform.localScale = new Vector3(trackWidth, 1f, segmentLength); // Zachowaj oryginaln¹ d³ugoœæ w skali
        segment.transform.SetParent(transform);

        segmentsQueue.Enqueue(segment);
    }

    void CleanupOldSegments()
    {
        while (segmentsQueue.Count > segmentsAhead + segmentsBehind)
        {
            Destroy(segmentsQueue.Dequeue());
        }

        // Dodatkowe czyszczenie zbyt odleg³ych segmentów
        var segmentsArray = segmentsQueue.ToArray();
        for (int i = lastCleanSegmentIndex; i < segmentsArray.Length; i++)
        {
            if (Vector3.Distance(vehicle.position, segmentsArray[i].transform.position) >
                (segmentsBehind + 10) * segmentLength)
            {
                Destroy(segmentsArray[i]);
                lastCleanSegmentIndex = i + 1;
            }
            else
            {
                break;
            }
        }
    }

    public void ResetTrack()
    {
        // Usuñ wszystkie istniej¹ce segmenty
        while (segmentsQueue.Count > 0)
        {
            Destroy(segmentsQueue.Dequeue());
        }

        // Zresetuj pozycjê generowania
        lastPosition = vehicle.position - vehicle.forward * segmentsBehind * segmentLength;
        lastRotation = vehicle.rotation;

        // Wygeneruj nowy odcinek pocz¹tkowy
        GenerateInitialTrack();

        // Nowy offset szumu dla œwie¿ej trasy
        noiseOffset = new Vector2(
            Random.Range(0f, 1000f),
            Random.Range(0f, 1000f)
        );
    }
}