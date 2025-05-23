using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class dataSender : MonoBehaviour
{
    public string javaServerUrl = "http://localhost:8080/api/data";
    public float sendInterval = 1.0f;
    private float timer = 0f;

    void Start()
    {
        Debug.Log("Dziala");
    }
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= sendInterval)
        {
            timer = 0f;
            StartCoroutine(SendData());
        }
    }

    IEnumerator SendData()
    {
        var gameData = new GameData
        {
            CurrentSpeed = GetComponent<carScript>().currentSpeed,
            AverageSpeed = GetComponent<carScript>().averageSpeed,
            //DriftPoints = GetComponent<carScript>().DriftPoints, //do dorobienia
            DistanceToTarget = GetComponent<carScript>().distanceToReference,
            DistanceTraveled = GetComponent<carScript>().totalDistance
        };

        string json = JsonUtility.ToJson(gameData);
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);

        using (UnityWebRequest www = new UnityWebRequest(javaServerUrl, "POST"))
        {
            www.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("B≥πd: " + www.error);
            }
            else
            {
                Debug.Log("Odpowiedü: " + www.downloadHandler.text);
            }
        }
    }

    [System.Serializable]
    private class GameData
    {
        public float CurrentSpeed;
        public float AverageSpeed;
        public int DriftPoints;
        public float DistanceTraveled;
        public float DistanceToTarget;
    }
}