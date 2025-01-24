using UnityEngine;

public class ActivationManager : MonoBehaviour
{
    public GameObject[] gameObjectsToActivate;

    [System.Obsolete]
    void Start()
    {
        DBConnection dbConnection = FindObjectOfType<DBConnection>();
        if (dbConnection != null)
        {
            dbConnection.AddLoginListener(ActivateGameObjects);
        }
    }

    [System.Obsolete]
    void OnDestroy()
    {
        DBConnection dbConnection = FindObjectOfType<DBConnection>();
        if (dbConnection != null)
        {
            dbConnection.RemoveLoginListener(ActivateGameObjects);
        }
    }

    void ActivateGameObjects()
    {
        foreach (GameObject obj in gameObjectsToActivate)
        {
            obj.SetActive(true); 
        }
    }
}
