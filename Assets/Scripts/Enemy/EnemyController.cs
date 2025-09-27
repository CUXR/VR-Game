using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityHFSM;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [Header("References")]
    private NavMeshAgent agent;
    private EnemyVision vision;
    private EnemyHealth health;
    private GameObject player;
    private float investigateTime = 15f;
    private float chaseTime = 10f;
    StateMachine fsm;

    // Public to allow Audio Controller access
    [HideInInspector] public EnemyHearing hearing;

    [Header("Patrol Settings")]
    public List<Transform> patrolPoints;
    public float patrolSpeed = 4f;
    private int currentPatrolIndex = 0;

    [Header("Investigation Settings")]
    public Stack<Vector3> investigatePositions = new Stack<Vector3>(3);
    public float investigateSpeed = 6f;
    private float startInvestigateTime;
    private float reachedPositionTime;
    private bool reachedPosition;

    [Header("Chase Settings")]
    public float chaseSpeed = 8f;
    private float startChaseTime;


    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        vision = GetComponent<EnemyVision>();
        hearing = GetComponent<EnemyHearing>();
        health = GetComponent<EnemyHealth>();
        player = GameObject.FindGameObjectWithTag("Player");        
    }

    void Start()
    {
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var hit, 2f, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }
        GameController.Instance.AddEnemy(this);

        // Copy start point and set as first patrol point
        GameObject startPatrolPoint = new GameObject(gameObject.name + ": Waypoint 0");
        startPatrolPoint.transform.position = transform.position;
        startPatrolPoint.transform.rotation = transform.rotation;

        if (patrolPoints.Count > 0) {
            startPatrolPoint.transform.SetParent(patrolPoints[0].parent);
        }
        
        patrolPoints.Insert(0, startPatrolPoint.transform);

        fsm = new StateMachine();

        // Add FSM States
        fsm.AddState("Patrol", onEnter: state => agent.speed = patrolSpeed, onLogic: state => Patrol());
        fsm.AddState("Investigate", onEnter: state => agent.speed = investigateSpeed, onLogic: state => Investigate());
        // For when scientists are killed and become more aggressive: fsm.AddState("Search", onLogic: state => Search());
        fsm.AddState("Chase", onEnter: state => agent.speed = chaseSpeed, onLogic: state => Chase());
        // Depends on how fleshed out head-to-head combat will be: fsm.AddState("Evade", onLogic: state => Evade());
        fsm.AddState("Dead", onEnter: state => Dead());

        // Patrol -> Investigate
        fsm.AddTransition("Patrol", "Investigate", t => hearing.heardSound || vision.InvestigatePlayer());

        // Patrol -> Chase
        fsm.AddTransition("Patrol", "Chase", t => vision.PlayerVisible());

        // Investigate -> Chase
        fsm.AddTransition("Investigate", "Chase", t => vision.PlayerVisible());

        // Investigate -> Patrol
        fsm.AddTransition("Investigate", "Patrol", t => Time.time - startInvestigateTime >= investigateTime);

        // Chase -> Investigate
        fsm.AddTransition("Chase", "Investigate", t => Time.time - startChaseTime >= chaseTime);

        // [Movement] -> Dead
        fsm.AddTransition("Patrol", "Dead", t => !health.isAlive);
        fsm.AddTransition("Investigate", "Dead", t => !health.isAlive);
        fsm.AddTransition("Chase", "Dead", t => !health.isAlive);

        // Initialize FSM
        fsm.SetStartState("Patrol");
        fsm.Init();
    }

    void Update()
    {
        fsm.OnLogic();
    }

    void Patrol()
    {
        print("Patrolling");
        investigatePositions.Clear();

        if (patrolPoints.Count == 0) return;

        if (agent.remainingDistance < 0.5f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }

    void Investigate()
    {
        print("Investigating Position: " + investigatePositions.Peek());
        
        startInvestigateTime = Time.time;
        if (agent.remainingDistance < 0.5f) {
            reachedPositionTime = Time.time;
            reachedPosition = true;
        }
        if (investigatePositions.Count > 0) {
            if (Time.time - reachedPositionTime > 3f && reachedPosition) {
                investigatePositions.Pop();
                startInvestigateTime = Time.time;
            }
            agent.SetDestination(investigatePositions.Peek());
        }
    }

    // void Search()
    // {
    //     // TODO: Implement search logic
    // }

    void Chase()
    {
        print("Chasing Player");
        if (vision.PlayerVisible()) startChaseTime = Time.time;
        agent.SetDestination(player.transform.position);
        agent.SetDestination(player.transform.position);
    }
    
    // void Evade()
    // {
    //     // TODO: Implement evade logic
    // }

    void Dead()
    {
        print("Enemy Dead");
        agent.isStopped = true;

                GameController.Instance.RemoveEnemy(this);
        Destroy(gameObject, 5f);
    }

    protected void FindNearestPatrolPoint()
    {
        Transform closestPatrolPos = null;
        float closestDistance = float.MaxValue;

        foreach (Transform patrolPos in patrolPoints)
        {
            float distance = Vector3.Distance(patrolPos.position, transform.position);

            if (closestPatrolPos != null)
            {

                // If closer than previous closest distance
                if (distance < closestDistance)
                {

                    // Set as temporary closest patrol point
                    closestPatrolPos = patrolPos;
                    closestDistance = distance;
                }
            }

            else
            {
                closestPatrolPos = patrolPos;
                closestDistance = distance;
            }
        }

        // Go to closest patrol point
        currentPatrolIndex = patrolPoints.IndexOf(closestPatrolPos);
    }

    public float GetHearingRange()
    {
        return hearing.GetRange();
    }
}
