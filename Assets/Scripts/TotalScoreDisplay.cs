using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class TotalScoreDisplay : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    private string fetchScoreUrl = "/api/fetchTotalScore.php";
    private int lastScore = -1;

    void Start()
    {
        scoreText.text = "Please log in to view your score.";
        DBConnection.OnLoggedIn += StartFetchingScore;
    }

    void StartFetchingScore()
    {
        int userId = PlayerPrefs.GetInt("userID", 0);
        if (userId != 0)
        {
            StartCoroutine(FetchPlayerScore(userId));
            StartCoroutine(UpdateScoreLoop(userId));
        }
    }

    IEnumerator FetchPlayerScore(int userId)
    {
        string url = DBConnection.db_ip + fetchScoreUrl + "?id=" + userId;
        //Debug.Log($"📡 Sending GET request to {url}");

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                //Debug.LogError("❌ Error while fetching the score: " + www.error);
                scoreText.text = "Failed to load score.";
            }
            else
            {
                ScoreResponse response = JsonUtility.FromJson<ScoreResponse>(www.downloadHandler.text);
                if (response.Status)
                {
                    UpdateScoreUI(response.Score);
                }
                else
                {
                    scoreText.text = "⚠ Error: " + response.Message;
                }
            }
        }
    }

    IEnumerator UpdateScoreLoop(int userId)
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            StartCoroutine(FetchPlayerScore(userId));
        }
    }

    void UpdateScoreUI(int newScore)
    {
        if (newScore != lastScore)
        {
            lastScore = newScore;
            scoreText.text = "Total Score: " + newScore;
            Debug.Log($"✅ Score updated: {newScore}");
        }
    }

    [System.Serializable]
    public class ScoreResponse
    {
        public int Score;
        public bool Status;
        public string Message;
    }
}
