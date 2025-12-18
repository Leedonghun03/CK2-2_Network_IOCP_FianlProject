using UnityEngine;

public class SpawnerVisualizer : MonoBehaviour
{
    public Vector3[] spawnerPositions = new Vector3[]
    {
        new Vector3(10, 0.5f, 10),
        new Vector3(-10, 0.5f, 10),
        new Vector3(10, 0.5f, -10),
        new Vector3(-10, 0.5f, -10),
        new Vector3(0, 0.5f, 0)
    };

    public Color[] spawnerColors = new Color[]
    {
        Color.green,   // Slime
        Color.green,   // Slime
        Color.yellow,  // Goblin
        Color.yellow,  // Goblin
        Color.red      // Wolf
    };

    void OnDrawGizmos()
    {
        for (int i = 0; i < spawnerPositions.Length; i++)
        {
            Gizmos.color = spawnerColors[i];
            Gizmos.DrawWireSphere(spawnerPositions[i], 1f);
            Gizmos.DrawLine(spawnerPositions[i], spawnerPositions[i] + Vector3.up * 3f);
        }
    }
}