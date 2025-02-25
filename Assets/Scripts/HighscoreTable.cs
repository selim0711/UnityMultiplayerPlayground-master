using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class HighscoreTable : MonoBehaviour
{
    public TMP_Text[] highscoreTexts;
    private string highscoreURL = "http://localhost/api/get_highscores.php";

    private void Start()
    {
        StartCoroutine(GetHighscores());
    }

    IEnumerator GetHighscores()
    {
        UnityWebRequest request = UnityWebRequest.Get(highscoreURL);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonResponse = request.downloadHandler.text;
            HighscoreData data = JsonUtility.FromJson<HighscoreData>(jsonResponse);

            if (data.success)
            {
                DisplayHighscores(data.highscores);
            }
            else
            {
                Debug.LogError("Fehler: " + data.message);
            }
        }
        else
        {
            Debug.LogError("Fehler beim Laden der Highscores: " + request.error);
        }
    }

    void DisplayHighscores(HighscoreEntry[] highscores)
    {
        for (int i = 0; i < highscoreTexts.Length; i++)
        {
            if (i < highscores.Length)
            {
                highscoreTexts[i].text = $"{i + 1}. {highscores[i].name} - {highscores[i].score} Punkte";
            }
            else
            {
                highscoreTexts[i].text = $"{i + 1}. ---";
            }
        }
    }
}

// Hilfsklassen für JSON-Daten
[System.Serializable]
public class HighscoreData
{
    public bool success;
    public string message;
    public HighscoreEntry[] highscores;
}

[System.Serializable]
public class HighscoreEntry
{
    public string name;
    public int score;
}
