using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [Header("Chase")]
    private NavMeshAgent agent;
    private EnemyVision vision;
    private GameObject player;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        vision = GetComponent<EnemyVision>();
        player = GameObject.FindWithTag("Player");
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

        if (vision.playerVisible)
        {
            agent.SetDestination(player.transform.position);
        }
    }
}
