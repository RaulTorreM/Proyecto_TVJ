using UnityEngine;
using System.Collections;

public class NPCRandomMovement : MonoBehaviour
{
    public float moveSpeed = 0.75f;
    public float gridSize = 0.1f;
    public float minWaitTime = 2f;
    public float maxWaitTime = 5f;
    public Sprite[] directionSprites;

    private SpriteRenderer spriteRenderer;
    private ObstacleCollisionChecker collisionChecker;
    private bool isMoving = false;
    private float currentWaitTime;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        collisionChecker = GetComponent<ObstacleCollisionChecker>();

        if (collisionChecker == null)
        {
            Debug.LogError("Falta el componente ObstacleCollisionChecker en: " + gameObject.name);
        }

        currentWaitTime = Random.Range(minWaitTime, maxWaitTime);
        StartCoroutine(RandomMovementRoutine());
    }

    IEnumerator RandomMovementRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(currentWaitTime);

            if (!isMoving)
            {
                Vector2 randomDirection = GetRandomDirection();
                UpdateNPCSprite(randomDirection);

                Vector2 movement = randomDirection * gridSize;
                Vector2 targetPos = (Vector2)transform.position + movement;

                if (collisionChecker == null || collisionChecker.CanMoveTo(targetPos))
                {
                    yield return StartCoroutine(MoveToPosition(targetPos));
                }
            }

            currentWaitTime = Random.Range(minWaitTime, maxWaitTime);
        }
    }

    IEnumerator MoveToPosition(Vector2 target)
    {
        isMoving = true;

        while (Vector2.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector2.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = target;
        isMoving = false;
    }

    Vector2 GetRandomDirection()
    {
        int direction = Random.Range(0, 4);
        switch (direction)
        {
            case 0: return Vector2.up;
            case 1: return Vector2.down;
            case 2: return Vector2.left;
            case 3: return Vector2.right;
            default: return Vector2.zero;
        }
    }

    void UpdateNPCSprite(Vector2 direction)
    {
        if (direction == Vector2.up) spriteRenderer.sprite = directionSprites[2];
        else if (direction == Vector2.down) spriteRenderer.sprite = directionSprites[0];
        else if (direction == Vector2.left) spriteRenderer.sprite = directionSprites[1];
        else if (direction == Vector2.right) spriteRenderer.sprite = directionSprites[3];
    }
}
