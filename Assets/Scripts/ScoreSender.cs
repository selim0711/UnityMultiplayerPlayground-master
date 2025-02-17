using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class ScoreSender : MonoBehaviour
{
    /*
    string updateScoreUrl = "http://192.168.8.157/api/updateScore.php";

    public void SendScore(int clientId, int score)
    {
        StartCoroutine(UpdatePlayerScore(clientId, score));
    }

    IEnumerator UpdatePlayerScore(int playerId, int score)
    {
        WWWForm form = new WWWForm();
        form.AddField("id", playerId);
        form.AddField("score", score);

        using (UnityWebRequest www = UnityWebRequest.Post(updateScoreUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Fehler beim Senden des Scores: " + www.error);
            }
            else
            {
                Debug.Log("Score erfolgreich gesendet! Antwort: " + www.downloadHandler.text);
            }
        }
    } */
}
