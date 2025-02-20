using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class TotalScoreDisplay : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    private string fetchScoreUrl = "http://192.168.0.222/api/fetchTotalScore.php";
    private int lastScore = -1; // ✅ Store last fetched score to detect changes

    void Start()
    {
        scoreText.text = "Please log in to view your score.";
        DBConnection.OnLoggedIn += StartFetchingScore; // ✅ Start fetching only after login
    }

    void StartFetchingScore()
    {
        int userId = PlayerPrefs.GetInt("userID", 0);
        if (userId != 0)
        {
            StartCoroutine(FetchPlayerScore(userId));
            StartCoroutine(UpdateScoreLoop(userId)); // ✅ Start checking for score updates
        }
    }

    IEnumerator FetchPlayerScore(int userId)
    {
        string url = fetchScoreUrl + "?id=" + userId;
        Debug.Log($"📡 Sending GET request to {url}");

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("❌ Error while fetching the score: " + www.error);
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
        while (true) // ✅ Keep checking the score every 3 seconds
        {
            yield return new WaitForSeconds(1f);
            StartCoroutine(FetchPlayerScore(userId));
        }
    }

    void UpdateScoreUI(int newScore)
    {
        if (newScore != lastScore) // ✅ Only update UI if the score has changed
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
