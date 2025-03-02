using UnityEngine;

[System.Serializable]
public struct MovementSettings
{
    public Vector3 direction;
    public float distance;  
    public float speed; 
}

public class MovingPlatform : MonoBehaviour
{
    public MovementSettings settings;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float movementPhase = Mathf.Sin(Time.timeSinceLevelLoad * settings.speed * Mathf.PI);
        transform.position = startPos + settings.direction.normalized * settings.distance * movementPhase;
    }
}
