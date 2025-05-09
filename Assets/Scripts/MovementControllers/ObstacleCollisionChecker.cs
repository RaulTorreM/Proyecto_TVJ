using UnityEngine;

public class ObstacleCollisionChecker : MonoBehaviour
{
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float checkRadius = 0.05f;

    public bool CanMoveTo(Vector2 targetPosition)
    {
        return !Physics2D.OverlapCircle(targetPosition, checkRadius, obstacleLayer);
    }
}
