using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityHFSM;

[RequireComponent(typeof(NavMeshAgent))]
public abstract class EnemyController : MonoBehaviour
{
    [Header("References")]
    protected NavMeshAgent agent;
    protected EnemyVision vision;
    protected EnemyHealth health;
    protected GameObject player;
    protected StateMachine fsm;
    [HideInInspector] public EnemyHearing hearing;
    public Stack<Vector3> investigatePositions = new Stack<Vector3>(3);

    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        vision = GetComponent<EnemyVision>();
        hearing = GetComponent<EnemyHearing>();
        health = GetComponent<EnemyHealth>();
        player = GameObject.FindGameObjectWithTag("Player");

        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var hit, 2f, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }

        if (GameController.Instance != null)
        {
            GameController.Instance.AddEnemy(this);
        }
        else
        {
            Debug.LogError("GameController instance not found! Make sure it exists in the scene.");
        }
        fsm = new StateMachine();
    }

    protected virtual void Update()
    {
        fsm.OnLogic();
    }
    
    protected virtual void Patrol() { }
    protected virtual void Investigate() { }
    protected virtual void Chase() { }
    // protected virtual void Search() { }
    // protected virtual void Evade() { }

    protected virtual void Dead()
    {
        print("Enemy Dead");
        agent.isStopped = true;

        if (GameController.Instance != null)
        {
            GameController.Instance.RemoveEnemy(this);
        }
        Destroy(gameObject, 5f);
    }

    public float GetHearingRange()
    {
        return hearing.GetRange();
    }
}
