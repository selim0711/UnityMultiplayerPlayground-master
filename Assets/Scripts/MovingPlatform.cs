using UnityEngine;

[System.Serializable] // This makes the struct visible in the Inspector.
public struct MovementSettings
{
    public Vector3 direction; // Direction of movement
    public float distance;    // Distance of the movement
    public float speed;       // Speed of the oscillation
}

public class MovingPlatform : MonoBehaviour
{
    public MovementSettings settings; // Instance of the MovementSettings struct.

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position; // Record the initial position to use as a base for movement.
    }

    void Update()
    {
        // Calculate the new position using a sine wave for smooth oscillation.
        float movementPhase = Mathf.Sin(Time.timeSinceLevelLoad * settings.speed * Mathf.PI);
        transform.position = startPos + settings.direction.normalized * settings.distance * movementPhase;
    }
}
