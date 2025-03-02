using UnityEngine;

public class PlatformSpawner : MonoBehaviour
{
    public GameObject platformPrefab;
    public int numberOfPlatforms = 10;
    public GameObject areaGameObject;
    public Transform platformsContainer;

    public void SpawnPlatforms()
    {
        ClearPlatforms(); 

        if (areaGameObject == null)
        {
            Debug.LogError("Area GameObject nicht zugewiesen");
            return;
        }

        BoxCollider areaCollider = areaGameObject.GetComponent<BoxCollider>();
        if (areaCollider == null)
        {
            Debug.LogError("Kein Collider gefunden");
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
            newPlatform.name = "Speed Item" + i;


        }
    }

   
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
