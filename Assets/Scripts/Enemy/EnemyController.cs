using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [Header("Chase")]
    private NavMeshAgent agent;
    private EnemyVision vision;
    private EnemyHealth health;
    private GameObject player;
    // Public to allow Audio Controller access
    public EnemyHearing hearing;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        vision = GetComponent<EnemyVision>();
        hearing = GetComponent<EnemyHearing>();
        health = GetComponent<EnemyHealth>();
        player = GameObject.FindWithTag("Player");
        GameController.Instance.AddEnemy(this);
    }

    void Start()
    {
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var hit, 2f, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }
    }

    void Update()
    {
        if (!player)
            return;

        if (vision.PlayerVisible())
        {
            agent.SetDestination(player.transform.position);
        }
        if (!health.isAlive)
        {
            GameController.Instance.RemoveEnemy(this);
        }
    }
}
