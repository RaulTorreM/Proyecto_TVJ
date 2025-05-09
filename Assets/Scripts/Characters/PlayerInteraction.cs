using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    
    private Survivor survivor;
    private Camera mainCamera;

    private void Awake()
    {
        survivor = GetComponent<Survivor>();
        mainCamera = Camera.main;
    }

    private void Update()
    {
        HandleMovementInput();
        HandleInteractionInput();
    }

    private void HandleMovementInput()
    {
        Vector2 input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );
        
        survivor.SetMovementInput(input);
    }

    private void HandleInteractionInput()
    {
        if(Input.GetKeyDown(interactKey))
        {
            survivor.TryInteractWithDoor();
        }
    }
}