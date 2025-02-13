using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class HighscoreTable : MonoBehaviour
{
    public TMP_Text[] scoreTexts; // Array of TextMesh Pro text objects

    void Start()
    {
        StartCoroutine(FetchHighScores());
    }

    IEnumerator FetchHighScores()
    {
        using (UnityWebRequest www = UnityWebRequest.Get("http://192.168.8.157/api/fetchHighScores.php"))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to fetch high scores: {www.error}");
            }
            else
            {
                UpdateHighscoreDisplay(www.downloadHandler.text);
            }
        }
    }

    void UpdateHighscoreDisplay(string jsonString)
    {
        try
        {
            HighScores scores = JsonUtility.FromJson<HighScores>(jsonString);

            if (scores.success && scores.highscores != null) // Check success and if highscores is not null
            {
                int index = 0;
                foreach (var entry in scores.highscores)
                {
                    if (index < scoreTexts.Length)
                    {
                        scoreTexts[index].text = $"{index + 1}. {entry.name} - {entry.score}";
                        index++;
                    }
                }

                // Clear any remaining text slots
                for (; index < scoreTexts.Length; index++)
                {
                    scoreTexts[index].text = "";
                }
            }
            else
            {
                Debug.LogError("No highscores found or success is false.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing JSON: {e.Message}\nJSON: {jsonString}");
        }
    }

    [System.Serializable]
    public class HighScores
    {
        public bool success;
        public ScoreEntry[] highscores; // This should match the key in your JSON
    }

    [System.Serializable]
    public class ScoreEntry
    {
        public string name;
        public int score; // Ensure this is an int if it's always a whole number
    }
}
