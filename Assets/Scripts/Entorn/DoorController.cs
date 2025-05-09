using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(Collider2D)), RequireComponent(typeof(SpriteRenderer))]
public class DoorController : MonoBehaviour
{
    // Estados de la puerta
    public enum DoorState { Closed, Open, Broken }
    
    [Header("Configuración Básica")]
    [SerializeField] private string doorTag = "Door";
    [SerializeField] private DoorState initialState = DoorState.Closed;
    
    [Header("Visuales")]
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;
    [SerializeField] private Sprite brokenSprite;
    [SerializeField] private float shakeIntensity = 0.1f;
    [SerializeField] private float shakeDuration = 0.5f;
    
    [Header("Mecánicas")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float damagePerAttack = 10f;
    [SerializeField] private float attackCooldown = 1f;
    
    
    // Componentes
    private Collider2D solidCollider;
    private Collider2D triggerCollider;
    private SpriteRenderer spriteRenderer;
    private Vector2 originalPosition;
    
    // Estado interno
    private DoorState currentState;
    private float currentHealth;
    private float lastAttackTime;
    private List<Survivor> blockers = new List<Survivor>();
    
    public bool IsBroken => currentState == DoorState.Broken;
    public bool IsOpen => currentState == DoorState.Open;

    
    // Eventos para expandir funcionalidad
    public event System.Action<DoorState> OnStateChanged;
    public event System.Action<float> OnHealthChanged;

    private void Awake()
    {
        // Configurar componentes
        solidCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalPosition = transform.position;
        
        // Buscar trigger collider si existe
        var colliders = GetComponents<Collider2D>();
        triggerCollider = colliders.Length > 1 ? colliders[1] : null;
        
        // Configuración inicial
        gameObject.tag = doorTag;
        InitializeState(initialState);
    }

    private void InitializeState(DoorState state)
    {
        currentState = state;
        currentHealth = maxHealth;
        
        UpdateVisuals();
        UpdateColliders();
        
        OnStateChanged?.Invoke(currentState);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    private void UpdateVisuals()
    {
        switch(currentState)
        {
            case DoorState.Closed:
                spriteRenderer.sprite = closedSprite;
                break;
            case DoorState.Open:
                spriteRenderer.sprite = openSprite;
                break;
            case DoorState.Broken:
                spriteRenderer.sprite = brokenSprite;
                break;
        }
    }

    private void UpdateColliders()
    {
        if(solidCollider != null)
        {
            solidCollider.enabled = currentState == DoorState.Closed;
            solidCollider.isTrigger = false;
        }
        
        if(triggerCollider != null)
        {
            triggerCollider.enabled = currentState != DoorState.Closed;
            triggerCollider.isTrigger = true;
        }
    }

    public void AddBlocker(Survivor survivor)
    {
        if(!blockers.Contains(survivor))
        {
            blockers.Add(survivor);
            UpdateDoorSecurity();
        }
    }

    public void RemoveBlocker(Survivor survivor)
    {
        if(blockers.Remove(survivor))
        {
            UpdateDoorSecurity();
        }
    }

    private void UpdateDoorSecurity()
    {
        // Lógica de seguridad basada en supervivientes
        bool isSecure = CheckSecurity();
        
        if(isSecure && currentState == DoorState.Broken)
        {
            RepairDoor();
        }
    }

    private bool CheckSecurity()
    {
        int normalSurvivors = blockers.FindAll(s => !s.IsStrong).Count;
        bool hasStrong = blockers.Exists(s => s.IsStrong);
        
        return hasStrong || normalSurvivors >= 2;
    }

    public void TakeDamage(float damage)
    {
        if(currentState == DoorState.Broken || CheckSecurity()) return;
        
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
        
        // Feedback visual
        ShakeDoor();
        
        if(currentHealth <= 0)
        {
            BreakDoor();
        }
    }

    private void ShakeDoor()
    {
        // Sistema de vibración alternativo sin LeanTween
        StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        Vector3 originalPos = transform.position;
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            transform.position = originalPos + (Vector3)Random.insideUnitCircle * shakeIntensity;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = originalPos;
    }

    private void BreakDoor()
    {
        currentState = DoorState.Broken;
        UpdateVisuals();
        UpdateColliders();
        OnStateChanged?.Invoke(currentState);
    }

    private void RepairDoor()
    {
        currentState = DoorState.Closed;
        currentHealth = maxHealth;
        UpdateVisuals();
        UpdateColliders();
        OnStateChanged?.Invoke(currentState);
        OnHealthChanged?.Invoke(1f);
    }

    // Métodos públicos para interacción
    public void Interact(IDamager damager)
    {
        if(Time.time > lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            TakeDamage(damagePerAttack);
            damager.OnDamageDealed();
        }
    }

    public void ToggleState()
    {
        if(currentState == DoorState.Closed)
        {
            currentState = DoorState.Open;
        }
        else if(currentState == DoorState.Open)
        {
            currentState = DoorState.Closed;
        }
        
        UpdateVisuals();
        UpdateColliders();
        OnStateChanged?.Invoke(currentState);
    }

    // Propiedades de solo lectura para otros componentes
    public DoorState CurrentState => currentState;
    public bool IsPassable => currentState != DoorState.Closed;
    public float HealthPercentage => currentHealth / maxHealth;
}