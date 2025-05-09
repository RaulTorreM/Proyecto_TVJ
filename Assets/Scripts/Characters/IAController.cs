using UnityEngine;
using System.Collections;

public class AISurvivorController : Survivor 
{
    [SerializeField] private float decisionInterval = 2f;
    
    private void Start()
    {
        StartCoroutine(AIDecisionRoutine());
    }

    private IEnumerator AIDecisionRoutine()
    {
        while(true)
        {
            yield return new WaitForSeconds(decisionInterval);
            
            if(Random.value > 0.7f)
            {
                TryInteractWithDoor();
            }
        }
    }
}