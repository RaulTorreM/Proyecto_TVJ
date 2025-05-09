using UnityEngine;

[System.Serializable]
public class DirectionSprites {
    public Sprite up;
    public Sprite down;
    public Sprite left;
    public Sprite right;
}

public class DirectionSpriteController : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private DirectionSprites directionSprites;
    [SerializeField] private SpriteRenderer entitySpriteRenderer;

    [Header("Debug")]
    [SerializeField] private Vector2 currentDirection;

    private void Awake() {
        if(entitySpriteRenderer == null) {
            entitySpriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    public void UpdateDirection(Vector2 movementDirection) {
        currentDirection = movementDirection.normalized;
        UpdateSprite();
    }

    private void UpdateSprite() {
        if(currentDirection == Vector2.zero) return;

        // Determinar dirección dominante
        if(Mathf.Abs(currentDirection.x) > Mathf.Abs(currentDirection.y)) {
            // Horizontal
            entitySpriteRenderer.sprite = currentDirection.x > 0 
                ? directionSprites.right 
                : directionSprites.left;
        }
        else {
            // Vertical
            entitySpriteRenderer.sprite = currentDirection.y > 0 
                ? directionSprites.up 
                : directionSprites.down;
        }
    }
}