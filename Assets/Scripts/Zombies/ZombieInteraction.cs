using UnityEngine;

public class ZombieInteraction : MonoBehaviour
{
    [SerializeField] private float interactionRange = 0.5f;
    [SerializeField] private float damagePerSecond = 20f;
    
    private DoorController targetDoor;
    
    void Update()
    {
        if(targetDoor == null) return;

        if(Vector2.Distance(transform.position, targetDoor.transform.position) <= interactionRange)
        {
            if(!targetDoor.IsBroken)
            {
                targetDoor.TakeDamage(damagePerSecond);
            }
            else
            {
                // La puerta está rota, el zombie puede pasar
            }
        }
    }

    public void SetTargetDoor(DoorController door)
    {
        targetDoor = door;
    }


    
}