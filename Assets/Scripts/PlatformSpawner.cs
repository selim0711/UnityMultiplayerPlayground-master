using UnityEngine;

//[CreateAssetMenu(fileName = "PlatformSpawner", menuName = "Scriptable Objects/PlatformSpawner")]
public class PlatformSpawner : MonoBehaviour
{
    public GameObject platformPrefab;
    public int numberOfPlatforms = 10;
    public GameObject areaGameObject; // Reference to the empty GameObject with a Box Collider
    public Transform platformsContainer; // Optional: A parent object to keep the scene tidy

    public void SpawnPlatforms()
    {
        ClearPlatforms(); // Clear existing platforms before spawning new ones

        if (areaGameObject == null)
        {
            Debug.LogError("Area GameObject is not assigned!");
            return;
        }

        BoxCollider areaCollider = areaGameObject.GetComponent<BoxCollider>();
        if (areaCollider == null)
        {
            Debug.LogError("No Box Collider component found on area GameObject!");
            return;
        }

        Bounds bounds = areaCollider.bounds;

        for (int i = 0; i < numberOfPlatforms; i++)
        {
            Vector3 spawnPosition = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z)
            );

            GameObject newPlatform = Instantiate(platformPrefab, spawnPosition, Quaternion.identity, platformsContainer);
            newPlatform.name = "Moving Platform " + i;

            // Ensure the movement is only vertical
            Vector3 direction = Vector3.up;  // Set direction strictly upwards
            float distance = Random.Range(7f, 20f);
            float speed = Random.Range(0.2f, 0.5f);

            MovingPlatform movingPlatform = newPlatform.GetComponent<MovingPlatform>();
            if (movingPlatform != null)
            {
                movingPlatform.settings.direction = direction;
                movingPlatform.settings.distance = distance;
                movingPlatform.settings.speed = speed;
            }
        }
    }

    // Method to clear all spawned platforms
    public void ClearPlatforms()
    {
        if (platformsContainer != null)
        {
            foreach (Transform child in platformsContainer)
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }
}
