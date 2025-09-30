using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityHFSM;

public class ScientistController : EnemyController
{
    [Header("Patrol Settings")]
    public List<Transform> patrolPoints;
    public float patrolSpeed = 4f;
    public float minPatrolPauseTime = 2f;
    public float maxPatrolPauseTime = 5f;
    private int currentPatrolIndex;
    private bool isPatrolWaiting;
    private float patrolWaitUntil;

    [Header("Investigation Settings")]
    public float investigateSpeed = 6f;
    public float investigateTime = 10f;
    public float investigateWaitTime = 3f;
    private float startInvestigateTime;
    private float reachedPositionTime;
    private bool reachedPosition;

    [Header("Chase Settings")]
    public float chaseSpeed = 8f;
    public float chaseTime = 15f;
    private float startChaseTime;

    protected override void Start()
    {
        base.Start();
        SetInitialPatrolPosition();

        InitStates();
        InitTransitions();
        fsm.Init();

        ToggleRagdoll(false);
    }

    protected override void Update()
    {
        base.Update();

        print(fsm.ActiveStateName);
    }

    private void InitStates()
    {
        // lerp speed for smoother transitions between speeds
        fsm.AddState("Patrol",
            onEnter: state => { SetSpeed(patrolSpeed);  FindNearestPatrolPoint(); hasInvestigatePosition = false; isPatrolWaiting = false; },
            onLogic: state => Patrol()
        );

        fsm.AddState("Investigate",
            onEnter: state => { SetSpeed(investigateSpeed); reachedPosition = false; startInvestigateTime = Time.time; },
            onLogic: state => Investigate(),
            onExit: state => { hasInvestigatePosition = false; reachedPosition = false; }
        );

        fsm.AddState("Chase",
            onEnter: state => { SetSpeed(chaseSpeed); hasInvestigatePosition = false; startChaseTime = Time.time; },
            onLogic: state => Chase()
        );

        fsm.AddState("Dead", onEnter: state => Dead());

        // For when scientists are killed and become more aggressive: fsm.AddState("Search", onLogic: state => Search());
        // Depends on how fleshed out head-to-head combat will be: fsm.AddState("Evade", onLogic: state => Evade());

        fsm.SetStartState("Patrol");
    }

    private void InitTransitions()
    {
        // Patrol -> Investigate
        fsm.AddTransition("Patrol", "Investigate", t => vision.PlayerInvestigate() || hearing.HeardSound());

        // Patrol -> Chase
        fsm.AddTransition("Patrol", "Chase", t => vision.PlayerVisible());

        // Investigate -> Chase
        fsm.AddTransition("Investigate", "Chase", t => vision.PlayerVisible());

        // Investigate -> Patrol
        fsm.AddTransition("Investigate", "Patrol", t => reachedPosition && Time.time - reachedPositionTime >= investigateTime);

        // Chase -> Investigate
        fsm.AddTransition("Chase", "Investigate", t => Time.time - startChaseTime >= chaseTime);

        // [ANY STATE] -> Dead
        fsm.AddTransitionFromAny("Dead", t => !health.isAlive);
    }

    protected override void Patrol()
    {
        if (patrolPoints.Count == 0) return;

        // If currently waiting at a patrol point, check timer
        if (isPatrolWaiting)
        {
            SetSpeed(0);

            if (Time.time >= patrolWaitUntil)
            {
                // Move to the next patrol point
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;

                if (agent != null)
                {
                    SetSpeed(patrolSpeed);
                    agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                }

                isPatrolWaiting = false;
            }
            return;
        }

        // If reached current destination, start waiting
        if (agent.remainingDistance < agent.stoppingDistance)
        {
            isPatrolWaiting = true;
            patrolWaitUntil = Time.time + Random.Range(minPatrolPauseTime, maxPatrolPauseTime);
        }
    }

    protected override void Investigate()
    {
        if (vision.PlayerInvestigate() || hearing.HeardSound())
        {
            startInvestigateTime = Time.time;
            reachedPosition = false;
            SetSpeed(investigateSpeed);
        }

        agent.SetDestination(investigatePosition);
        print("Investigating: " + investigatePosition);
        
        if (hasInvestigatePosition && agent.remainingDistance < agent.stoppingDistance && !reachedPosition)
        {
            reachedPositionTime = Time.time;
            reachedPosition = true;
        }

        if (reachedPosition)
        {
            if (Time.time - reachedPositionTime >= investigateWaitTime)
            {
                hasInvestigatePosition = false;
                reachedPosition = false;
            }
            else
            {
                SetSpeed(0);
            }
        }
    }

    // protected override void Search()
    // {
    //     // TODO: Implement search logic
    // }

    protected override void Chase()
    {
        hasInvestigatePosition = false;
        if (vision.PlayerVisible()) startChaseTime = Time.time;
        agent.SetDestination(player.transform.position);
    }

    // protected override void Evade()
    // {
    //     // TODO: Implement evade logic
    // }

    protected override void Dead()
    {
        agent.isStopped = true;

        if (GameController.Instance != null)
        {
            GameController.Instance.RemoveEnemy(this);
        }

        ToggleRagdoll(true);
    }

    protected void SetInitialPatrolPosition()
    {
        GameObject startPatrolPoint = new GameObject(gameObject.name + ": Waypoint 0");
        startPatrolPoint.transform.position = transform.position;

        if (patrolPoints.Count > 0)
        {
            startPatrolPoint.transform.SetParent(patrolPoints[0].parent);
        }

        patrolPoints.Insert(0, startPatrolPoint.transform);
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

    private void ToggleRagdoll(bool isRagdoll)
    {
        if (isRagdoll)
        {
            animator.enabled = false;
            agent.enabled = false;
            foreach (var rb in GetComponentsInChildren<Rigidbody>())
            {
                rb.isKinematic = false;
            }
        }

        else
        {
            animator.enabled = true;
            agent.enabled = true;
            foreach (var rb in GetComponentsInChildren<Rigidbody>())
            {
                rb.isKinematic = true;
            }
        }
    }
}
