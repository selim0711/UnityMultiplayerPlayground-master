using UnityEngine;
using TMPro;
using Unity.Netcode;

public class ScoreUI : MonoBehaviour
{
    /*
    public PlayerWithRaycastControl playerScript; // Assign this via the inspector or find it dynamically
    private TextMeshProUGUI textMesh;

    void Start()
    {
        if (playerScript == null)
        {
            // Attempt to find the player script automatically if not set
            playerScript = FindObjectOfType<PlayerWithRaycastControl>(); // This is a simplification and may need refinement for multiple players
        }

        textMesh = GetComponent<TextMeshProUGUI>();
        if (!textMesh)
        {
            Debug.LogError("TextMeshPro component not found!");
            return;
        }

        // Register callback for when the score changes
        if (playerScript)
        {
            playerScript.networkScore.OnValueChanged += HandleScoreChanged;
        }
    }

    private void HandleScoreChanged(int oldScore, int newScore)
    {
        // Update the UI
        if (textMesh)
        {
            textMesh.text = $"Score: {newScore}";
        }
    }

    void OnDestroy()
    {
        // Clean up delegate to prevent memory leaks
        if (playerScript)
        {
            playerScript.networkScore.OnValueChanged -= HandleScoreChanged;
        }
    } */
}
