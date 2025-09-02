using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyNav : MonoBehaviour
{
    [Header("Chase")]
    public Transform target;              // Drag your PLAYER here in the Inspector
    public float repathInterval = 0.1f;   // How often to refresh the path (seconds)

    private NavMeshAgent agent;
    // private float timer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var hit, 2f, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }

        //auto-find a target tagged "Player" if none assigned
        if (!target)
        {
            var p = GameObject.FindWithTag("Player");
            if (p) target = p.transform;
        }
    }

    void Update()
    {
        // if (!target) return;

        // timer += Time.deltaTime;
        // if (timer < repathInterval) return;
        // timer = 0f;

        agent.SetDestination(target.position);
    }
}
