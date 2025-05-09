using UnityEngine;
using System.Collections;

[RequireComponent(typeof(DirectionSpriteController)), RequireComponent(typeof(ObstacleCollisionChecker))]
public class Survivor : MonoBehaviour
{
    [Header("Survivor Settings")]
    [SerializeField] private bool isStrong = false;
    [SerializeField] private float interactionRadius = 1.5f;
    [SerializeField] private float movementSpeed = 3f;
    [SerializeField] private float blockCooldown = 1f;

    [Header("Weapon Settings")]
    [SerializeField] private bool hasWeapon = false;
    [SerializeField] private GameObject weaponObject;

    private DoorController currentDoor;
    private Vector2 movementInput;
    private float lastBlockTime;
    private bool isBlocking = false;

    // Componentes
    private DirectionSpriteController directionController;
    private Rigidbody2D rb;
    private Animator animator;

    // Propiedades públicas para acceso externo
    public bool IsStrong => isStrong;
    public bool HasWeapon => hasWeapon;
    public bool IsBlocking => isBlocking;

    private void Awake()
    {
        directionController = GetComponent<DirectionSpriteController>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        
        if(weaponObject != null) 
            weaponObject.SetActive(hasWeapon);
    }

    private void Update()
    {
        HandleAnimation();
        UpdateDirection();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    public void SetMovementInput(Vector2 input)
    {
        movementInput = input.normalized;
    }

    public void TryInteractWithDoor()
    {
        if(Time.time - lastBlockTime < blockCooldown) return;

        DoorController door = FindNearestDoor();
        
        if(door != null)
        {
            if(isBlocking)
            {
                StopBlocking();
            }
            else
            {
                StartBlocking(door);
            }
            
            lastBlockTime = Time.time;
        }
    }

    private void StartBlocking(DoorController door)
    {
        if(Vector2.Distance(transform.position, door.transform.position) > interactionRadius) return;

        currentDoor = door;
        isBlocking = true;
        currentDoor.AddBlocker(this);
    }

    private void StopBlocking()
    {
        if(currentDoor != null)
        {
            currentDoor.RemoveBlocker(this);
            currentDoor = null;
        }
        isBlocking = false;
    }

    private DoorController FindNearestDoor()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRadius);
        foreach(var hit in hits)
        {
            DoorController door = hit.GetComponent<DoorController>();
            if(door != null && !door.IsBroken)
            {
                return door;
            }
        }
        return null;
    }

    private void HandleMovement()
    {
        if(isBlocking) return;

        Vector2 targetPosition = rb.position + movementInput * movementSpeed * Time.fixedDeltaTime;
        
        if(CanMoveTo(targetPosition))
        {
            rb.MovePosition(targetPosition);
        }
    }

    private bool CanMoveTo(Vector2 position)
    {
        return GetComponent<ObstacleCollisionChecker>().CanMoveTo(position);
    }

    private void UpdateDirection()
    {
        if(movementInput.magnitude > 0.1f && !isBlocking)
        {
            directionController.UpdateDirection(movementInput);
        }
    }

    private void HandleAnimation()
    {
        if(animator != null)
        {
            animator.SetBool("IsMoving", movementInput.magnitude > 0.1f);
            animator.SetBool("IsBlocking", isBlocking);
        }
    }

    public void EquipWeapon(GameObject weaponPrefab)
    {
        if(weaponObject != null)
        {
            Destroy(weaponObject);
        }
        
        weaponObject = Instantiate(weaponPrefab, transform);
        hasWeapon = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }

    private void OnDestroy()
    {
        StopBlocking();
    }
}