using UnityEngine;

public class CliffTopTrigger : MonoBehaviour
{
    [Tooltip("Point this object's blue Z-axis arrow INTO the mountain wall (the direction the player should face while climbing down).")]
    public bool useObjectForwardAsWallDirection = true;

    public Vector3 GetFaceMountainDirection()
    {
        Vector3 dir = transform.forward;
        dir.y = 0f;
        return dir.sqrMagnitude > 0.001f ? dir.normalized : Vector3.forward;
    }

    // Draws a blue arrow in the Scene view showing which way the player will face when pressing E
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 start = transform.position;
        Vector3 dir = GetFaceMountainDirection();
        Gizmos.DrawRay(start, dir * 1.5f);
        Gizmos.DrawSphere(start + dir * 1.5f, 0.12f);
    }
}