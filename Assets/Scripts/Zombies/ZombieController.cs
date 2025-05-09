using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MyGame.Utilities;

public class ZombieController : MonoBehaviour, IDamager
{
    private enum ZombieState { Patrol, Alerted, BreakingDoor, Hunting, Killing, Disoriented }
    private ZombieState currentState = ZombieState.Patrol;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private float waypointThreshold = 0.1f;
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private float pathUpdateInterval = 0.5f;
    [SerializeField] private Vector2 gridSize = Vector2.one;
    [SerializeField] private LayerMask obstacleLayer; // capa "Obstacle"

    [Header("Behavior Settings")]
    [SerializeField] [Range(0,1)] private float alertProbability = 0.3f;
    [SerializeField] private float searchCooldown = 5f;
    [SerializeField] private float doorInteractionCooldown = 1f;

    [Header("Components")]
    [SerializeField] private DirectionSpriteController directionController;

    private Rigidbody2D rb;
    private Vector2 colliderCheckSize;
    private Stack<Vector2> currentPath = new Stack<Vector2>();
    private Vector2? currentStep;
    private float pathUpdateTimer;
    private float stateTimer;
    private Transform currentTarget;
    private DoorController targetDoor;

    public float GetDamage() => 10f;
    public void OnDamageDealed() {}

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        directionController ??= GetComponent<DirectionSpriteController>();
        var box = GetComponent<BoxCollider2D>();
        colliderCheckSize = box ? box.size : gridSize * 0.9f;
    }

    private void Update()
    {
        HandleStateMachine();
        HandlePathUpdates();
    }

    private void HandleStateMachine()
    {
        switch(currentState)
        {
            case ZombieState.Patrol: PatrolBehavior(); break;
            case ZombieState.Alerted: AlertedBehavior(); break;
            case ZombieState.BreakingDoor: BreakingDoorBehavior(); break;
            case ZombieState.Hunting: HuntingBehavior(); break;
            case ZombieState.Killing: KillingBehavior(); break;
            case ZombieState.Disoriented: DisorientedBehavior(); break;
        }
    }

    private void PatrolBehavior()
    {
        if(Random.value < alertProbability * Time.deltaTime)
            TransitionTo(ZombieState.Alerted);
    }

    private void AlertedBehavior()
    {
        currentTarget = FindNearestSurvivor();
        TransitionTo(currentTarget != null ? 
            (IsTargetBehindDoor(currentTarget, out targetDoor) ? ZombieState.BreakingDoor : ZombieState.Hunting) 
            : ZombieState.Disoriented);
    }

    private void BreakingDoorBehavior()
    {
        if(targetDoor==null||targetDoor.IsBroken) { TransitionTo(ZombieState.Hunting); return; }
        if(Vector2.Distance(transform.position, targetDoor.transform.position)>attackRange)
            SetPathTo(targetDoor.transform.position);
        else return;
    }

    private void HuntingBehavior()
    {
        if(currentTarget==null) { TransitionTo(ZombieState.Disoriented); return; }
        if(Vector2.Distance(transform.position, currentTarget.position)<=attackRange)
            TransitionTo(ZombieState.Killing);
        else SetPathTo(currentTarget.position);
    }

    private void KillingBehavior()
    {
        var surv=currentTarget?.GetComponent<Survivor>();
        if(surv!=null&&!surv.HasWeapon) Destroy(currentTarget.gameObject);
        SearchForNearbySurvivors();
        TransitionTo(ZombieState.Disoriented);
    }

    private void DisorientedBehavior()
    {
        stateTimer+=Time.deltaTime;
        if(stateTimer>=searchCooldown)
            TransitionTo(Random.value<alertProbability?ZombieState.Alerted:ZombieState.Patrol);
    }

    private void TransitionTo(ZombieState ns)
    {
        currentState=ns;
        stateTimer=0;
        currentPath.Clear();
        currentStep=null;
        if(ns==ZombieState.Alerted) currentTarget=FindNearestSurvivor();
        if(ns==ZombieState.Hunting && currentTarget!=null) SetPathTo(currentTarget.position);
        if(ns==ZombieState.Disoriented) currentTarget=null;
    }

    private Transform FindNearestSurvivor()
    {
        float md=float.MaxValue; Transform best=null;
        foreach(var o in GameObject.FindGameObjectsWithTag("Survivor")){
            float d=Vector2.Distance(transform.position,o.transform.position);
            if(d<md){md=d;best=o.transform;}
        }
        return best;
    }

    private void SearchForNearbySurvivors()
    {
        foreach(var h in Physics2D.OverlapCircleAll(transform.position,detectionRadius)){
            if(h.CompareTag("Survivor")){currentTarget=h.transform;TransitionTo(ZombieState.Hunting);return;}
        }
    }

    private bool IsTargetBehindDoor(Transform t,out DoorController d){
        var h=Physics2D.Linecast(transform.position,t.position,LayerMask.GetMask("Door"));
        d=h.collider?.GetComponent<DoorController>();return d!=null;
    }

    private void HandlePathUpdates()
    {
        if(currentState!=ZombieState.Hunting && currentState!=ZombieState.BreakingDoor) return;
        pathUpdateTimer+=Time.deltaTime;
        if(pathUpdateTimer>=pathUpdateInterval){ pathUpdateTimer=0; if(currentTarget!=null) SetPathTo(currentTarget.position);}        
        if(!currentStep.HasValue && currentPath.Count>0) currentStep=currentPath.Pop();
        if(currentStep.HasValue) MoveToStep();
    }

    private void SetPathTo(Vector2 worldTarget)
    {
        // Determina celda destino y ajusta si está bloqueada
        Vector2Int targetCell=WorldToGrid(worldTarget);
        if(!CanMoveToGridCell(targetCell)){
            // buscar vecino libre más cercano
            var neighbors=new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
            float bestDist=float.MaxValue; Vector2Int bestCell=targetCell;
            foreach(var dir in neighbors){
                var nc=targetCell+dir;
                if(CanMoveToGridCell(nc)){
                    var wp=GridToWorld(nc);
                    float d=Vector2.Distance(worldTarget,wp);
                    if(d<bestDist){bestDist=d; bestCell=nc;}
                }
            }
            if(bestDist==float.MaxValue){ currentPath.Clear(); currentStep=null; return; }
            targetCell=bestCell;
        }
        // BFS considerando obstáculos
        Vector2Int start=WorldToGrid(transform.position);
        var frontier=new Queue<Vector2Int>();
        var came=new Dictionary<Vector2Int,Vector2Int>();
        var vis=new HashSet<Vector2Int>();
        frontier.Enqueue(start);vis.Add(start);came[start]=start;
        bool found=false;
        while(frontier.Count>0){
            var cur=frontier.Dequeue();
            if(cur==targetCell){ found=true; break; }
            foreach(var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right }){
                var nxt=cur+d;
                if(vis.Contains(nxt)||!CanMoveToGridCell(nxt)) continue;
                frontier.Enqueue(nxt); vis.Add(nxt); came[nxt]=cur;
            }
        }
        // reconstruir ruta
        var pathCells=new List<Vector2Int>();
        if(found){
            var step=targetCell;
            while(step!=start){
                pathCells.Add(step);
                step=came[step];
            }
            pathCells.Reverse();
        }
        // llenar stack
        currentPath.Clear();
        for(int i=pathCells.Count-1;i>=0;i--) currentPath.Push(GridToWorld(pathCells[i]));
        currentStep=currentPath.Count>0?currentPath.Pop():(Vector2?)null;
    }

    private bool CanMoveToGridCell(Vector2Int c)
    {
        var wp=GridToWorld(c);
        return Physics2D.OverlapBox(wp,colliderCheckSize,0f,obstacleLayer)==null;
    }

    private void MoveToStep()
    {
        var sp=currentStep.Value;
        var raw=sp-rb.position;
        var md=raw.normalized;
        var sd=Mathf.Abs(raw.x)>Mathf.Abs(raw.y)?new Vector2(Mathf.Sign(raw.x),0):new Vector2(0,Mathf.Sign(raw.y));
        directionController?.UpdateDirection(sd);
        var mv=md*moveSpeed*Time.deltaTime;
        if(mv.sqrMagnitude>=raw.sqrMagnitude){rb.MovePosition(sp);currentStep=null;}else rb.MovePosition(rb.position+mv);
    }

    private Vector2Int WorldToGrid(Vector2 wp)=>new(Mathf.RoundToInt(wp.x/gridSize.x),Mathf.RoundToInt(wp.y/gridSize.y));
    private Vector2 GridToWorld(Vector2Int c)=>new(c.x*gridSize.x,c.y*gridSize.y);
}
