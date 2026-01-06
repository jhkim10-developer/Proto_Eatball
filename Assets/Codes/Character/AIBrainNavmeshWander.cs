using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AIBrainNavmeshWander : MonoBehaviour
{
    [Header("Wander")]
    [SerializeField] private float roamRadius = 12f;
    [SerializeField] private float repathInterval = 1.5f;
    [SerializeField] private float arriveDistance = 1.0f;

    private NavMeshAgent _agent;
    private float _nextRepathTime;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.autoBraking = true;
    }

    private void OnEnable()
    {
        PickNewDestination();
    }

    private void Update()
    {
        if (Time.time < _nextRepathTime) return;
        _nextRepathTime = Time.time + repathInterval;

        if (!_agent.hasPath || _agent.remainingDistance <= Mathf.Max(arriveDistance, _agent.stoppingDistance))
        {
            PickNewDestination();
        }
    }

    private void PickNewDestination()
    {
        Vector3 random = Random.insideUnitSphere * roamRadius;
        random.y = 0f;

        Vector3 target = transform.position + random;

        if (NavMesh.SamplePosition(target, out var hit, roamRadius, NavMesh.AllAreas))
        {
            _agent.SetDestination(hit.position);
        }
    }

    // 디버그용: 씬에서 roamRadius를 볼 수 있게
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, roamRadius);
    }
}
