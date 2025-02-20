using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ScoreDisplay : MonoBehaviour
{
    public TextMeshProUGUI scoreText;

    void OnEnable()
    {
        PlayerWithRaycastControl.OnPlayerReady += SetupScoreDisplay;
    }

    void OnDisable()
    {
        PlayerWithRaycastControl.OnPlayerReady -= SetupScoreDisplay;
    }

    private void SetupScoreDisplay(PlayerWithRaycastControl player)
    {
        if (player.IsOwner)
        {
            player.networkScore.OnValueChanged += UpdateScoreUI;
            UpdateScoreUI(player.networkScore.Value, player.networkScore.Value);
        }
    }

    private void UpdateScoreUI(int oldValue, int newValue)
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + newValue;
        }
    }
}
