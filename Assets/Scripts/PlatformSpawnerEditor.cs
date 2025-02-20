using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlatformSpawner))]
public class PlatformSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draws the default inspector

        PlatformSpawner spawner = (PlatformSpawner)target;

        if (GUILayout.Button("Spawn Platforms"))
        {
            spawner.SpawnPlatforms();
        }
        if (GUILayout.Button("Clear Platforms"))
        {
            spawner.ClearPlatforms();
        }
    }
}
