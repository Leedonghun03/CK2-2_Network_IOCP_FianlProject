using UnityEngine;

public class SpawnZoneVisualizer : MonoBehaviour
{
    public Vector3 Size = new Vector3(10f, 0f, 10f);
    public Color GizmoColor = Color.green;

    void OnDrawGizmos()
    {
        Gizmos.color = GizmoColor;
        Gizmos.DrawWireCube(transform.position, Size);
    }
}