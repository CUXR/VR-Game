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

        InitStates(initialState: "Patrol");
        InitTransitions();
        fsm.Init();

        ToggleRagdoll(false);
    }

    protected override void Update()
    {
        base.Update();

        print(fsm.ActiveStateName);
    }

    private void InitStates(string initialState)
    {
        // lerp speed for smoother transitions between speeds
        fsm.AddState("Patrol", onEnter: state => SetSpeed(patrolSpeed), onLogic: state => Patrol());
        fsm.AddState("Investigate", onEnter: state => SetSpeed(investigateSpeed), onLogic: state => Investigate(), onExit: state => investigatePositions.Clear());
        // For when scientists are killed and become more aggressive: fsm.AddState("Search", onLogic: state => Search());
        fsm.AddState("Chase", onEnter: state => SetSpeed(chaseSpeed), onLogic: state => Chase());
        // Depends on how fleshed out head-to-head combat will be: fsm.AddState("Evade", onLogic: state => Evade());
        fsm.AddState("Dead", onEnter: state => Dead());

        fsm.SetStartState(initialState);
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
        fsm.AddTransition("Investigate", "Patrol", t => Time.time - startInvestigateTime >= investigateTime);

        // Chase -> Investigate
        fsm.AddTransition("Chase", "Investigate", t => Time.time - startChaseTime >= chaseTime);

        // [Movement] -> Dead
        fsm.AddTransition("Patrol", "Dead", t => !health.isAlive);
        fsm.AddTransition("Investigate", "Dead", t => !health.isAlive);
        fsm.AddTransition("Chase", "Dead", t => !health.isAlive);
    }

    protected override void Patrol()
    {
        investigatePositions.Clear();
        if (patrolPoints.Count == 0) return;

        if (agent.remainingDistance < 0.5f && !isPatrolWaiting)
        {
            StartCoroutine(WaitAtPatrolPoint(Random.Range(minPatrolPauseTime, maxPatrolPauseTime)));
        }
    }

    private IEnumerator WaitAtPatrolPoint(float waitTime)
    {
        isPatrolWaiting = true;
        SetSpeed(0);

        yield return new WaitForSeconds(waitTime);
        
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;

        if (agent != null)
        {
            SetSpeed(patrolSpeed);
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }

        isPatrolWaiting = false;
    }

    protected override void Investigate()
    {
        StopAllCoroutines();

        if (vision.PlayerInvestigate() || hearing.HeardSound()) startInvestigateTime = Time.time;

        if (!reachedPosition)
        {
            Vector3 targetPos = investigatePositions.Peek();
            agent.SetDestination(targetPos);

            if (agent.remainingDistance < 0.5f)
            {
                reachedPosition = true;
                reachedPositionTime = Time.time;
                investigatePositions.Pop();
            }
        }
        else
        {
            if (Time.time - reachedPositionTime >= investigateWaitTime)
            {
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
        StopAllCoroutines();
        investigatePositions.Clear();
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

    protected IEnumerator UpdateSpeed(float targetSpeed, float duration)
    {
        float initialSpeed = agent.speed;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            agent.speed = Mathf.Lerp(initialSpeed, targetSpeed, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        agent.speed = targetSpeed;
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
